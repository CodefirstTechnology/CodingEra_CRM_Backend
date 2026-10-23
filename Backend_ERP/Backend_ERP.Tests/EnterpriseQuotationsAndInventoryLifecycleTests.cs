using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Approvals;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Procurement;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Sales;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend_ERP.Tests
{
    public class EnterpriseQuotationsAndInventoryLifecycleTests
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
        public async Task ATP_Check_Calculates_Physical_Minus_Reserved_Accurately()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var allocationService = new InventoryAllocationService(db);

            var fg = new FinishedGood
            {
                Id = 101,
                TenantId = "TENANT-1",
                ProductCode = "FG-ROUTER-01",
                ProductName = "Industrial Router",
                WarehouseId = 5,
                FinishedQuantity = 100m,
                ReservedQuantity = 30m,
                ReorderLevel = 10m
            };
            fg.RecalculateAvailable(); // 70m
            db.FinishedGoods.Add(fg);
            await db.SaveChangesAsync();

            // Act: Request 50 units (Available is 70) -> Should be Available
            var resultAvailable = await allocationService.CheckAtpAsync(5, 101, "FG-ROUTER-01", 50m, "TENANT-1");

            // Act: Request 80 units (Available is 70) -> Should be Shortage
            var resultShortage = await allocationService.CheckAtpAsync(5, 101, "FG-ROUTER-01", 80m, "TENANT-1");

            // Assert
            Assert.Equal(100m, resultAvailable.PhysicalStock);
            Assert.Equal(30m, resultAvailable.ReservedStock);
            Assert.Equal(70m, resultAvailable.AvailableQuantity);
            Assert.True(resultAvailable.IsAvailable);
            Assert.Equal(0m, resultAvailable.ShortageQuantity);

            Assert.False(resultShortage.IsAvailable);
            Assert.Equal(10m, resultShortage.ShortageQuantity); // 80 - 70 = 10
        }

        [Fact]
        public async Task Quotation_DeepCloning_Produces_RPlus1_And_Locks_Predecessor()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var revisionService = new QuotationRevisionService(db);

            var quoteRev0 = new Quotation
            {
                Id = 201,
                TenantId = "TENANT-1",
                QuotationNumber = "QT-2026-001",
                RevisionNumber = 0,
                IsCurrentRevision = true,
                Status = QuotationStatuses.Draft,
                CustomerId = "CUST-001",
                CustomerName = "Enterprise Client Ltd",
                QuotationDate = DateOnly.FromDateTime(DateTime.UtcNow),
                ValidUntil = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
                Currency = "INR",
                Items = new List<QuotationItem>
                {
                    new QuotationItem
                    {
                        Id = 1,
                        LineNumber = 1,
                        ItemType = PricingItemType.FinishedGood,
                        ItemId = 101,
                        ItemCode = "FG-ROUTER-01",
                        ItemName = "Industrial Router",
                        Quantity = 10m,
                        UnitPrice = 5000m,
                        LineTotal = 50000m,
                        OriginalSellingRate = 5000m,
                        FloorPriceSnapshot = 4000m,
                        MaxDiscountPctSnapshot = 10m
                    }
                }
            };
            quoteRev0.CalculateTotals();
            db.Quotations.Add(quoteRev0);
            await db.SaveChangesAsync();

            // Act: Revise Rev 0
            var clonedQuote = await revisionService.CreateRevisionAsync(201, "Client requested revised specs", "analyst1");

            // Assert Predecessor is Locked
            var predecessor = await db.Quotations.FindAsync(201);
            Assert.NotNull(predecessor);
            Assert.False(predecessor.IsCurrentRevision);
            Assert.Equal(QuotationStatuses.Revised, predecessor.Status);

            // Assert Cloned Quote has R+1 Lineage
            Assert.Equal("QT-2026-001-R1", clonedQuote.QuotationNumber);
            Assert.Equal(1, clonedQuote.RevisionNumber);
            Assert.True(clonedQuote.IsCurrentRevision);
            Assert.Equal(QuotationStatuses.Draft, clonedQuote.Status);
            Assert.Equal(201, clonedQuote.ParentQuotationId);
            Assert.Equal(201, clonedQuote.RootQuotationId);
            Assert.Single(clonedQuote.Items);
            Assert.Equal(5000m, clonedQuote.Items.First().UnitPrice);
        }

        [Fact]
        public void CommercialCompliance_HMAC_Detects_Tampering_Of_Approved_Quote()
        {
            // Arrange
            var complianceService = new CommercialComplianceService();

            var quote = new Quotation
            {
                Id = 301,
                TenantId = "TENANT-1",
                QuotationNumber = "QT-2026-099",
                RevisionNumber = 0,
                CustomerId = "CUST-005",
                CustomerName = "Apex Industries",
                Subtotal = 100000m,
                DiscountTotal = 5000m,
                TaxTotal = 17100m,
                GrandTotal = 112100m,
                Items = new List<QuotationItem>
                {
                    new QuotationItem
                    {
                        LineNumber = 1,
                        ItemCode = "FG-ROUTER-01",
                        Quantity = 20m,
                        UnitPrice = 5000m,
                        DiscountPercent = 5m,
                        FloorPriceSnapshot = 4000m
                    }
                }
            };

            // Act: Generate HMAC Token
            var token = complianceService.GenerateApprovalToken(quote);

            // Assert: Legitimate quote validates successfully
            Assert.True(complianceService.VerifyApprovalToken(quote, token));

            // Act: Tamper with price (e.g. unauthorized price drop to 4500)
            quote.Items.First().UnitPrice = 4500m;

            // Assert: HMAC signature MUST reject tampered payload
            Assert.False(complianceService.VerifyApprovalToken(quote, token));
        }

        [Fact]
        public async Task SalesOrder_Conversion_Executes_SoftReservation_On_FinishedGoods()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var allocationService = new InventoryAllocationService(db);
            var complianceService = new CommercialComplianceService();

            var fg = new FinishedGood
            {
                Id = 501,
                TenantId = "TENANT-1",
                ProductCode = "FG-CABINET-01",
                ProductName = "Server Rack Cabinet",
                WarehouseId = 3,
                FinishedQuantity = 50m,
                ReservedQuantity = 10m
            };
            fg.RecalculateAvailable(); // 40 available
            db.FinishedGoods.Add(fg);

            var quote = new Quotation
            {
                Id = 401,
                TenantId = "TENANT-1",
                QuotationNumber = "QT-2026-CONV-01",
                RevisionNumber = 0,
                IsCurrentRevision = true,
                Status = QuotationStatuses.Approved,
                CustomerId = "CUST-010",
                CustomerName = "DataCenter Tech",
                QuotationDate = DateOnly.FromDateTime(DateTime.UtcNow),
                ValidUntil = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
                Currency = "INR",
                Items = new List<QuotationItem>
                {
                    new QuotationItem
                    {
                        Id = 1,
                        LineNumber = 1,
                        ItemType = PricingItemType.FinishedGood,
                        ItemId = 501,
                        WarehouseId = 3,
                        ItemCode = "FG-CABINET-01",
                        ItemName = "Server Rack Cabinet",
                        Quantity = 15m,
                        UnitPrice = 20000m,
                        LineTotal = 300000m
                    }
                }
            };
            quote.CalculateTotals();
            quote.ApprovalToken = complianceService.GenerateApprovalToken(quote);
            db.Quotations.Add(quote);
            await db.SaveChangesAsync();

            // Act: Execute Soft Reservation
            await allocationService.SoftReserveQuotationStockAsync(quote);

            // Assert
            var updatedFg = await db.FinishedGoods.FindAsync(501);
            Assert.NotNull(updatedFg);
            Assert.Equal(50m, updatedFg.FinishedQuantity);
            Assert.Equal(25m, updatedFg.ReservedQuantity); // 10 + 15 = 25
            Assert.Equal(25m, updatedFg.AvailableQuantity); // 50 - 25 = 25
        }

        [Fact]
        public async Task SalesOrder_Completion_Deducts_PhysicalStock_Releases_Reservation_And_Logs_Ledger()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var stockService = new StockDecrementService(db);

            var fg = new FinishedGood
            {
                Id = 601,
                TenantId = "TENANT-1",
                ProductCode = "FG-SWITCH-01",
                ProductName = "48-Port Managed Switch",
                WarehouseId = 2,
                FinishedQuantity = 100m,
                ReservedQuantity = 20m
            };
            fg.RecalculateAvailable(); // 80 available
            db.FinishedGoods.Add(fg);

            var salesOrder = new SalesOrder
            {
                Id = 701,
                TenantId = "TENANT-1",
                SalesOrderNumber = "SO-2026-001",
                OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Status = SalesOrderStatuses.Confirmed,
                CustomerName = "Telecom Corp",
                Items = new List<SalesOrderItem>
                {
                    new SalesOrderItem
                    {
                        Id = 1,
                        LineKey = Guid.NewGuid().ToString(),
                        ItemType = PricingItemType.FinishedGood,
                        ItemId = 601,
                        WarehouseId = 2,
                        ItemCode = "FG-SWITCH-01",
                        ItemName = "48-Port Managed Switch",
                        Quantity = 20m,
                        DispatchedQuantity = 0m,
                        Rate = 15000m,
                        Amount = 300000m
                    }
                }
            };
            db.SalesOrders.Add(salesOrder);
            await db.SaveChangesAsync();

            // Act: Complete Sales Order Dispatch
            var deductionResult = await stockService.DeductPhysicalStockForSalesOrderAsync(701, "warehouse_manager");

            // Assert Deduction Result & Order Status
            Assert.Equal(701, deductionResult.SalesOrderId);
            Assert.Single(deductionResult.DeductedItems);
            Assert.Equal(20m, deductionResult.DeductedItems.First().QuantityDeducted);

            var updatedSo = await db.SalesOrders.Include(s => s.Items).FirstOrDefaultAsync(s => s.Id == 701);
            Assert.NotNull(updatedSo);
            Assert.Equal(SalesOrderStatuses.Completed, updatedSo.Status);
            Assert.Equal(20m, updatedSo.Items.First().DispatchedQuantity);

            // Assert Physical Stock Decrements & Reservation Release
            var updatedFg = await db.FinishedGoods.FindAsync(601);
            Assert.NotNull(updatedFg);
            Assert.Equal(80m, updatedFg.FinishedQuantity);   // 100 - 20 = 80
            Assert.Equal(0m, updatedFg.ReservedQuantity);    // 20 - 20 = 0
            Assert.Equal(80m, updatedFg.AvailableQuantity);  // 80 - 0 = 80

            // Assert Double-Entry Stock Ledger
            var txn = await db.StockTransactions.FirstOrDefaultAsync(t => t.ReferenceId == 701);
            Assert.NotNull(txn);
            Assert.Equal("TENANT-1", txn.TenantId);
            Assert.Equal(StockTxnType.Dispatch, txn.TransactionType);
            Assert.Equal("FG-SWITCH-01", txn.MaterialCode);
            Assert.Equal(20m, txn.Quantity);
            Assert.Equal(80m, txn.BalanceAfter);
        }

        [Fact]
        public async Task SalesOrder_Completion_Throws_On_Insufficient_PhysicalStock()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var stockService = new StockDecrementService(db);

            var fg = new FinishedGood
            {
                Id = 801,
                TenantId = "TENANT-1",
                ProductCode = "FG-SERVER-01",
                ProductName = "Blade Server",
                WarehouseId = 1,
                FinishedQuantity = 5m, // Only 5 in physical stock
                ReservedQuantity = 5m
            };
            fg.RecalculateAvailable();
            db.FinishedGoods.Add(fg);

            var salesOrder = new SalesOrder
            {
                Id = 901,
                TenantId = "TENANT-1",
                SalesOrderNumber = "SO-2026-FAIL-01",
                OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Status = SalesOrderStatuses.Confirmed,
                CustomerName = "HyperScale Inc",
                Items = new List<SalesOrderItem>
                {
                    new SalesOrderItem
                    {
                        Id = 1,
                        LineKey = Guid.NewGuid().ToString(),
                        ItemType = PricingItemType.FinishedGood,
                        ItemId = 801,
                        WarehouseId = 1,
                        ItemCode = "FG-SERVER-01",
                        ItemName = "Blade Server",
                        Quantity = 10m, // Demands 10 (exceeds physical 5)
                        DispatchedQuantity = 0m,
                        Rate = 50000m,
                        Amount = 500000m
                    }
                }
            };
            db.SalesOrders.Add(salesOrder);
            await db.SaveChangesAsync();

            // Act & Assert: Negative inventory MUST be strictly blocked
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => stockService.DeductPhysicalStockForSalesOrderAsync(901, "dispatcher"));

            Assert.Contains("Negative inventory violation", ex.Message);
        }
    }
}
