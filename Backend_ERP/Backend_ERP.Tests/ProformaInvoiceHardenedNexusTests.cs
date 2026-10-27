using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Backend_ERP.Tests
{
    public class ProformaInvoiceHardenedNexusTests
    {
        private class TestMtoProductionTriggerService : IMtoProductionTriggerService
        {
            public Task<MtoEvaluationResultDto> EvaluateAndSpawnProductionAsync(
                int salesOrderId,
                string actingUser,
                CancellationToken ct = default)
            {
                return Task.FromResult(new MtoEvaluationResultDto
                {
                    SalesOrderId = salesOrderId,
                    IsMtoRequired = true,
                    SpawnedProductionPlanIds = new List<int> { 101 },
                    Details = new List<string> { "Test Work Order spawned" }
                });
            }
        }

        private ERPDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ERPDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new ERPDbContext(options);
        }

        private ProformaInvoiceService CreatePiService(ERPDbContext db)
        {
            var repo = new ProformaInvoiceRepository(db);
            return new ProformaInvoiceService(repo, db);
        }

        [Fact]
        public async Task Exceeding_Available_Sales_Order_Quantity_Throws_Ceiling_Violation()
        {
            using var db = CreateInMemoryDbContext();
            var piService = CreatePiService(db);

            var so = new SalesOrder
            {
                Id = 1,
                SalesOrderNumber = "SO-2026-0001",
                CustomerName = "Apex Tech",
                GrandTotal = 50000m,
                Items = new List<SalesOrderItem>
                {
                    new()
                    {
                        Id = 101,
                        ItemCode = "MOT-100",
                        ItemName = "High Speed Motor",
                        Quantity = 10m,
                        ProformaInvoicedQuantity = 8m, // Only 2 remaining!
                        Rate = 5000m,
                        Amount = 50000m
                    }
                }
            };
            db.SalesOrders.Add(so);
            await db.SaveChangesAsync();

            // Try to bill 5 units (exceeds remaining 2 units)
            var request = new ProformaInvoiceCreateRequestDto
            {
                InvoiceDate = "2026-10-27",
                ValidUntil = "2026-11-27",
                SalesOrderId = 1,
                SalesOrderNumber = "SO-2026-0001",
                Customer = new ProformaInvoiceCustomerDto { CustomerName = "Apex Tech" },
                Items = new List<ProformaInvoiceItemDto>
                {
                    new()
                    {
                        SalesOrderItemId = 101,
                        ItemCode = "MOT-100",
                        ItemName = "High Speed Motor",
                        Quantity = 5m,
                        Rate = 5000m,
                        Amount = 25000m
                    }
                }
            };

            // Assert ceiling violation error
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                piService.CreateAsync(request, "test-user"));

            Assert.Contains("exceeds unbilled balance", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Milestone_Percentage_Prorates_Line_Items_And_Taxes_Correctly()
        {
            using var db = CreateInMemoryDbContext();
            var piService = CreatePiService(db);

            var so = new SalesOrder
            {
                Id = 2,
                SalesOrderNumber = "SO-2026-0002",
                CustomerName = "Titan Heavy Eng",
                Status = SalesOrderStatuses.Confirmed,
                Subtotal = 100000m,
                GrandTotal = 118000m,
                PlaceOfSupplyStateCode = "27",
                Items = new List<SalesOrderItem>
                {
                    new()
                    {
                        Id = 201,
                        ItemCode = "TURB-01",
                        ItemName = "Gas Turbine",
                        Quantity = 1m,
                        Rate = 100000m,
                        Discount = 0m,
                        Gst = 18m,
                        TaxSlabPercentSnapshot = 18m,
                        Amount = 118000m
                    }
                }
            };
            db.SalesOrders.Add(so);
            await db.SaveChangesAsync();

            // Act: Generate 30% milestone proforma
            var result = await piService.GenerateFromSalesOrderAsync(2, "test-user", "Milestone", 30m);

            // Assert: Total and Rate prorated to 30%
            Assert.NotNull(result);
            Assert.Equal(35400m, result.GrandTotal); // 30% of 118000 = 35400
            Assert.Equal(30000m, result.Subtotal);   // 30% of 100000 = 30000
            Assert.Equal(5400m, result.TaxTotal);    // 30% of 18000 = 5400
            Assert.Contains("30% Milestone", result.Items[0].ItemName);

            // Verify SO cumulative tracking was updated
            var updatedSo = await db.SalesOrders.FindAsync(2);
            Assert.NotNull(updatedSo);
            Assert.Equal(35400m, updatedSo.ProformaInvoicedAmount);
        }

        [Fact]
        public async Task Receipt_Settlement_Thresholds_Unlock_Two_Tier_Operational_Gates()
        {
            using var db = CreateInMemoryDbContext();
            var mtoService = new TestMtoProductionTriggerService();
            var settlementService = new ProformaSettlementService(db, mtoService);

            var so = new SalesOrder
            {
                Id = 3,
                SalesOrderNumber = "SO-2026-0003",
                CustomerName = "Omega Fabricators",
                GrandTotal = 100000m,
                IsProductionReleased = false,
                IsDispatchReleased = false
            };
            db.SalesOrders.Add(so);

            var pi = new ProformaInvoice
            {
                Id = 30,
                PiNumber = "PI-2026-00030",
                SalesOrderId = 3,
                CustomerName = "Omega Fabricators",
                GrandTotal = 100000m,
                BaseCurrencyEquivalentAmount = 100000m,
                AdvanceReceivedAmount = 0m,
                IsProductionReleased = false,
                IsDispatchReleased = false,
                Status = ProformaInvoiceStatuses.Approved
            };
            db.ProformaInvoices.Add(pi);

            var advance = new AdvancePayment
            {
                Id = 301,
                PaymentNumber = "ADV-2026-001",
                CustomerName = "Omega Fabricators",
                AdvanceAmount = 100000m,
                AppliedAmount = 0m,
                RemainingAmount = 100000m
            };
            db.AdvancePayments.Add(advance);
            await db.SaveChangesAsync();

            // Step 1: Settle 30% (30,000) -> Should unlock Tier 1: Production Release ONLY
            var req1 = new ProformaSettlementRequestDto
            {
                AdvancePaymentId = 301,
                AllocatedAmount = 30000m,
                ReceiptVoucherNumber = "RCP-001"
            };

            var res1 = await settlementService.SettleReceiptAsync(30, req1, "finance-officer");

            Assert.Equal(30000m, res1.AdvanceReceivedAmount);
            Assert.True(res1.IsProductionReleased, "Tier 1: Production Release should be TRUE at 30%");
            Assert.False(res1.IsDispatchReleased, "Tier 2: Dispatch Release must remain FALSE under 100%");

            var piCheck1 = await db.ProformaInvoices.FindAsync(30);
            var soCheck1 = await db.SalesOrders.FindAsync(3);
            Assert.True(piCheck1!.IsProductionReleased);
            Assert.False(piCheck1.IsDispatchReleased);
            Assert.True(soCheck1!.IsProductionReleased);
            Assert.False(soCheck1.IsDispatchReleased);

            // Step 2: Settle remaining 70% (70,000) -> Should unlock Tier 2: Dispatch Release
            var req2 = new ProformaSettlementRequestDto
            {
                AdvancePaymentId = 301,
                AllocatedAmount = 70000m,
                ReceiptVoucherNumber = "RCP-002"
            };

            var res2 = await settlementService.SettleReceiptAsync(30, req2, "finance-officer");

            Assert.Equal(100000m, res2.AdvanceReceivedAmount);
            Assert.True(res2.IsProductionReleased);
            Assert.True(res2.IsDispatchReleased, "Tier 2: Dispatch Release should be TRUE at 100%");

            var piCheck2 = await db.ProformaInvoices.FindAsync(30);
            var soCheck2 = await db.SalesOrders.FindAsync(3);
            Assert.True(piCheck2!.IsDispatchReleased);
            Assert.True(soCheck2!.IsDispatchReleased);
        }

        [Fact]
        public async Task Cancelling_Proforma_Rolls_Back_Reserved_Balances_On_Parent_SO()
        {
            using var db = CreateInMemoryDbContext();
            var piService = CreatePiService(db);

            var so = new SalesOrder
            {
                Id = 4,
                SalesOrderNumber = "SO-2026-0004",
                CustomerName = "Delta Dynamics",
                GrandTotal = 40000m,
                ProformaInvoicedAmount = 40000m,
                Items = new List<SalesOrderItem>
                {
                    new()
                    {
                        Id = 401,
                        ItemCode = "VALVE-01",
                        ItemName = "Pressure Relief Valve",
                        Quantity = 4m,
                        ProformaInvoicedQuantity = 4m,
                        Rate = 10000m,
                        Amount = 40000m
                    }
                }
            };
            db.SalesOrders.Add(so);

            var pi = new ProformaInvoice
            {
                Id = 40,
                PiNumber = "PI-2026-00040",
                SalesOrderId = 4,
                SalesOrderNumber = "SO-2026-0004",
                CustomerName = "Delta Dynamics",
                GrandTotal = 40000m,
                Status = ProformaInvoiceStatuses.Approved,
                Items = new List<ProformaInvoiceItem>
                {
                    new()
                    {
                        Id = 4001,
                        SalesOrderItemId = 401,
                        ItemCode = "VALVE-01",
                        ItemName = "Pressure Relief Valve",
                        Quantity = 4m,
                        Rate = 10000m,
                        Amount = 40000m
                    }
                }
            };
            db.ProformaInvoices.Add(pi);
            await db.SaveChangesAsync();

            // Act: Cancel Proforma
            var updateReq = new ProformaInvoiceStatusUpdateRequestDto
            {
                Status = ProformaInvoiceStatuses.Cancelled,
                Remarks = "Order cancelled by client"
            };

            var cancelled = await piService.UpdateStatusAsync(40, updateReq, "admin");

            // Assert
            Assert.NotNull(cancelled);
            Assert.Equal(ProformaInvoiceStatuses.Cancelled, cancelled.Status);

            var updatedSo = await db.SalesOrders.Include(s => s.Items).FirstOrDefaultAsync(s => s.Id == 4);
            Assert.NotNull(updatedSo);
            Assert.Equal(0m, updatedSo.ProformaInvoicedAmount);
            Assert.Equal(0m, updatedSo.Items.First().ProformaInvoicedQuantity);
        }

        [Fact]
        public void Non_Repudiation_HMAC_Signature_Generation_And_Verification_Succeeds()
        {
            var config = new ConfigurationBuilder().Build();
            var approvalService = new ProformaApprovalService(null!, config);

            int proformaId = 108;
            string userId = "mgr-45";
            string decision = "Approved";
            string tenantId = "default";

            string token = approvalService.GenerateHmacToken(proformaId, userId, decision, tenantId);
            Assert.NotNull(token);
            Assert.NotEmpty(token);

            // Valid signature verification
            bool isValid = approvalService.VerifyHmacToken(proformaId, userId, decision, token, tenantId);
            Assert.True(isValid);

            // Tampered user fails verification
            bool tamperedUser = approvalService.VerifyHmacToken(proformaId, "tampered-user", decision, token, tenantId);
            Assert.False(tamperedUser);

            // Tampered decision fails verification
            bool tamperedDecision = approvalService.VerifyHmacToken(proformaId, userId, "Rejected", token, tenantId);
            Assert.False(tamperedDecision);
        }
    }
}
