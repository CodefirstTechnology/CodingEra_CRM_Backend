using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Accounting;
using ERP.Domain.Procurement;
using ERP.Domain.Production;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Sales;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend_ERP.Tests
{
    public class SalesOrderNexusHardenedTests
    {
        private ERPDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ERPDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new ERPDbContext(options);
        }

        [Fact]
        public async Task Quote_To_SO_Conversion_Maintains_Frozen_Pricing_And_Sets_ConvertedSalesOrderId()
        {
            using var db = CreateInMemoryDbContext();
            var allocationService = new SalesOrderAllocationService(db);
            var soService = new SalesOrderService(db, allocationService);

            var quote = new Quotation
            {
                Id = 1,
                QuotationNumber = "QT-2026-0001",
                CustomerId = "10",
                CustomerName = "Apex Industrial Systems",
                Status = QuotationStatuses.Approved,
                Subtotal = 10000m,
                DiscountTotal = 1000m,
                TaxTotal = 1620m,
                GrandTotal = 10620m,
                Items = new List<QuotationItem>
                {
                    new()
                    {
                        Id = 1,
                        ItemName = "High-Torque Servomotor",
                        ItemCode = "FG-MOTOR-100",
                        Quantity = 10m,
                        UnitPrice = 1000m,
                        DiscountPercent = 10m,
                        TaxPercent = 18m,
                        LineTotal = 10620m,
                        ResolvedPriceListId = 5,
                        PriceListNumberSnapshot = "PL-EXP-2026",
                        OriginalSellingRate = 1000m,
                        FloorPriceSnapshot = 850m,
                        MaxDiscountPctSnapshot = 15m,
                        TaxSlabPercentSnapshot = 18m
                    }
                }
            };
            db.Quotations.Add(quote);
            await db.SaveChangesAsync();

            // Act: Convert Quote to Sales Order
            var result = await soService.ConvertQuotationAsync(1, "test-user");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(SalesOrderStatuses.Submitted, result.Status);
            Assert.Equal(10620m, result.GrandTotal);

            var updatedQuote = await db.Quotations.FindAsync(1);
            Assert.NotNull(updatedQuote);
            Assert.Equal(QuotationStatuses.ConvertedToSO, updatedQuote.Status);
            Assert.Equal(result.Id, updatedQuote.ConvertedSalesOrderId);

            var createdSo = await db.SalesOrders.Include(s => s.Items).FirstOrDefaultAsync(s => s.Id == result.Id);
            Assert.NotNull(createdSo);
            var line = createdSo.Items.First();
            Assert.Equal("PL-EXP-2026", line.PriceListNumberSnapshot);
            Assert.Equal(1000m, line.OriginalSellingRate);
            Assert.Equal(850m, line.FloorPriceSnapshot);
            Assert.Equal(18m, line.TaxSlabPercentSnapshot);
        }

        [Fact]
        public async Task Duplicate_Quote_Conversion_Throws_InvalidOperationException()
        {
            using var db = CreateInMemoryDbContext();
            var allocationService = new SalesOrderAllocationService(db);
            var soService = new SalesOrderService(db, allocationService);

            var quote = new Quotation
            {
                Id = 2,
                QuotationNumber = "QT-2026-0002",
                CustomerId = "12",
                CustomerName = "Beta Dynamics",
                Status = QuotationStatuses.ConvertedToSO,
                ConvertedSalesOrderId = 99,
                ConvertedSalesOrderNumber = "SO-2026-0099"
            };
            db.Quotations.Add(quote);
            await db.SaveChangesAsync();

            // Act & Assert: Attempting to convert already converted quote must fail
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                soService.ConvertQuotationAsync(2, "test-user"));
        }

        [Fact]
        public async Task Soft_Allocation_Increments_ReservedQuantity_And_Generates_SalesOrderAllocation()
        {
            using var db = CreateInMemoryDbContext();
            var allocationService = new SalesOrderAllocationService(db);

            var fg = new FinishedGood
            {
                Id = 50,
                ProductCode = "FG-FLANGE-50",
                ProductName = "Precision Flange",
                WarehouseId = 1,
                WarehouseName = "Central Warehouse",
                FinishedQuantity = 100m,
                ReservedQuantity = 10m
            };
            fg.RecalculateAvailable(); // Available = 90
            db.FinishedGoods.Add(fg);

            var order = new SalesOrder
            {
                Id = 10,
                SalesOrderNumber = "SO-2026-0010",
                CustomerId = 15,
                CustomerName = "Precision Machining Corp",
                Status = SalesOrderStatuses.Confirmed,
                Items = new List<SalesOrderItem>
                {
                    new()
                    {
                        Id = 100,
                        ItemId = 50,
                        ItemCode = "FG-FLANGE-50",
                        ItemName = "Precision Flange",
                        Quantity = 40m,
                        AllocatedQuantity = 0m
                    }
                }
            };
            db.SalesOrders.Add(order);
            await db.SaveChangesAsync();

            // Act: Allocate stock
            var allocResult = await allocationService.AllocateOrderInventoryAsync(10, "planner-1");

            // Assert
            Assert.True(allocResult.Success);
            var updatedFg = await db.FinishedGoods.FindAsync(50);
            Assert.NotNull(updatedFg);
            Assert.Equal(50m, updatedFg.ReservedQuantity); // 10 + 40 = 50
            Assert.Equal(50m, updatedFg.AvailableQuantity); // 100 - 50 = 50

            var allocations = await db.SalesOrderAllocations.Where(a => a.SalesOrderId == 10).ToListAsync();
            Assert.Single(allocations);
            Assert.Equal(40m, allocations[0].AllocatedQuantity);
            Assert.Equal(AllocationStatus.Reserved, allocations[0].AllocationStatus);
        }

        [Fact]
        public async Task Order_Cancellation_Releases_Allocations_And_Decrements_ReservedQuantity()
        {
            using var db = CreateInMemoryDbContext();
            var allocationService = new SalesOrderAllocationService(db);
            var soService = new SalesOrderService(db, allocationService);

            var fg = new FinishedGood
            {
                Id = 60,
                ProductCode = "FG-VALVE-60",
                ProductName = "Hydraulic Valve",
                WarehouseId = 2,
                FinishedQuantity = 50m,
                ReservedQuantity = 20m
            };
            fg.RecalculateAvailable(); // 30 available
            db.FinishedGoods.Add(fg);

            var order = new SalesOrder
            {
                Id = 20,
                SalesOrderNumber = "SO-2026-0020",
                CustomerId = 22,
                CustomerName = "Valves Global",
                Status = SalesOrderStatuses.Confirmed,
                Items = new List<SalesOrderItem>
                {
                    new()
                    {
                        Id = 200,
                        ItemId = 60,
                        ItemCode = "FG-VALVE-60",
                        ItemName = "Hydraulic Valve",
                        Quantity = 20m,
                        AllocatedQuantity = 20m
                    }
                },
                Allocations = new List<SalesOrderAllocation>
                {
                    new()
                    {
                        Id = 1,
                        SalesOrderId = 20,
                        SalesOrderItemId = 200,
                        FinishedGoodId = 60,
                        WarehouseId = 2,
                        AllocatedQuantity = 20m,
                        AllocationStatus = AllocationStatus.Reserved
                    }
                }
            };
            db.SalesOrders.Add(order);
            await db.SaveChangesAsync();

            // Act: Cancel order
            var cancelResult = await soService.CancelAsync(20, new SalesOrderCancelRequestDto { Remarks = "Customer cancelled contract" }, "admin");

            // Assert
            Assert.NotNull(cancelResult);
            Assert.Equal(SalesOrderStatuses.Cancelled, cancelResult.Status);

            var updatedFg = await db.FinishedGoods.FindAsync(60);
            Assert.NotNull(updatedFg);
            Assert.Equal(0m, updatedFg.ReservedQuantity); // 20 - 20 = 0
            Assert.Equal(50m, updatedFg.AvailableQuantity); // 50 - 0 = 50

            var alloc = await db.SalesOrderAllocations.FindAsync(1);
            Assert.NotNull(alloc);
            Assert.Equal(AllocationStatus.Released, alloc.AllocationStatus);
        }

        [Fact]
        public async Task Customer_Exposure_Over_Limit_Triggers_Credit_Hold()
        {
            using var db = CreateInMemoryDbContext();
            var creditService = new CustomerCreditAssessmentService(db);

            // Existing Open AR = 4,00,000; Limit = 5,00,000
            db.CustomerLedgerEntries.Add(new CustomerLedgerEntry
            {
                CustomerId = 30,
                Outstanding = 400000m,
                DueDate = DateTime.UtcNow.AddDays(10), // Not overdue
                EntryType = LedgerEntryType.Invoice
            });

            await db.SaveChangesAsync();

            // Act: New order of ₹1,50,000 -> Total Exposure = 4,00,000 + 1,50,000 = 5,50,000 > 5,00,000
            var assessment = await creditService.AssessCustomerExposureAsync(30, 150000m);

            // Assert
            Assert.True(assessment.IsOnHold);
            Assert.False(assessment.IsPassed);
            Assert.Contains("exceeds approved credit limit", assessment.Reason);
        }

        [Fact]
        public async Task Overdue_Invoices_Trigger_Immediate_Credit_Hold()
        {
            using var db = CreateInMemoryDbContext();
            var creditService = new CustomerCreditAssessmentService(db);

            // Overdue AR of ₹50,000
            db.CustomerLedgerEntries.Add(new CustomerLedgerEntry
            {
                CustomerId = 35,
                Outstanding = 50000m,
                DueDate = DateTime.UtcNow.AddDays(-5), // Overdue by 5 days
                EntryType = LedgerEntryType.Invoice
            });

            await db.SaveChangesAsync();

            // Act: Even small order of ₹5,000 should be held due to overdue debt
            var assessment = await creditService.AssessCustomerExposureAsync(35, 5000m);

            // Assert
            Assert.True(assessment.IsOnHold);
            Assert.Contains("overdue", assessment.Reason);
        }

        [Fact]
        public async Task Two_Tier_Advance_Gate_Blocks_Production_And_Dispatch_Properly()
        {
            using var db = CreateInMemoryDbContext();
            var advanceGate = new SalesOrderAdvanceGateService(db);

            var order = new SalesOrder
            {
                Id = 40,
                CustomerCategory = "Standard",
                PaymentTerms = "30% Advance, Balance on Dispatch",
                GrandTotal = 100000m,
                AdvanceAllocatedAmount = 20000m // Only 20% allocated (less than 30%)
            };
            db.SalesOrders.Add(order);
            await db.SaveChangesAsync();

            // Act: Check Tier 1 Production Release Gate (requires 30,000)
            var tier1Check = await advanceGate.CanReleaseToProductionAsync(40);
            Assert.True(tier1Check.IsLocked);
            Assert.Contains("Tier 1 Production Lock", tier1Check.LockReason);

            // Now allocate 35,000 (35% > 30%)
            order.AdvanceAllocatedAmount = 35000m;
            await db.SaveChangesAsync();

            var tier1Passed = await advanceGate.CanReleaseToProductionAsync(40);
            Assert.False(tier1Passed.IsLocked);

            // Act: Check Tier 2 Dispatch Release Gate (requires 100,000)
            var tier2Check = await advanceGate.CanReleaseToDispatchAsync(40);
            Assert.True(tier2Check.IsLocked);
            Assert.Contains("Tier 2 Dispatch Lock", tier2Check.LockReason);

            // Pay 100%
            order.AdvanceAllocatedAmount = 100000m;
            await db.SaveChangesAsync();

            var tier2Passed = await advanceGate.CanReleaseToDispatchAsync(40);
            Assert.False(tier2Passed.IsLocked);
        }

        [Fact]
        public async Task MTO_Net_Deficit_Calculation_Factors_Open_Work_Orders()
        {
            using var db = CreateInMemoryDbContext();
            var mtoService = new MtoProductionTriggerService(db);

            var fg = new FinishedGood
            {
                Id = 70,
                ProductCode = "FG-GEAR-70",
                ProductName = "Heavy Spur Gear",
                WarehouseId = 1,
                FinishedQuantity = 10m,
                ReservedQuantity = 10m
            };
            fg.RecalculateAvailable(); // Available = 0
            db.FinishedGoods.Add(fg);

            // Case A: 100 units ordered, 0 stock, but open work order for 100 units already exists
            db.WorkOrders.Add(new WorkOrder
            {
                Id = 1,
                ProductId = 70,
                PlannedQuantity = 100m,
                PendingQuantity = 100m,
                Status = WorkOrderStatus.Released
            });

            var order = new SalesOrder
            {
                Id = 50,
                SalesOrderNumber = "SO-2026-0050",
                OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Items = new List<SalesOrderItem>
                {
                    new()
                    {
                        Id = 500,
                        ItemId = 70,
                        ItemCode = "FG-GEAR-70",
                        ItemName = "Heavy Spur Gear",
                        Quantity = 100m,
                        AllocatedQuantity = 0m
                    }
                }
            };
            db.SalesOrders.Add(order);
            await db.SaveChangesAsync();

            // Act: Evaluate MTO
            var result = await mtoService.EvaluateAndSpawnProductionAsync(50, "planner");

            // Assert: No new MTO plan should be spawned because existing open work order covers demand
            Assert.False(result.IsMtoRequired);
            Assert.Empty(result.SpawnedProductionPlanIds);
        }
    }
}
