using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ERP.Tests
{
    public class ProcurementReportsAndPayablesEnterpriseTests
    {
        private ERPDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ERPDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new ERPDbContext(options);
        }

        private IConfiguration CreateConfiguration()
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "Security:AuditHmacSecret", "TestHmacSecretKey2026AntigravityProcurementGovernance" }
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        [Fact]
        public async Task VendorLedger_PostBillPayable_CreatesCreditVoucherWithCorrectBalances()
        {
            using var db = CreateDbContext();
            var analytics = new ProcurementSpendAnalyticsService(db, NullLogger<ProcurementSpendAnalyticsService>.Instance);
            var engine = new VendorPayablesLedgerEngine(db, NullLogger<VendorPayablesLedgerEngine>.Instance, analytics);

            var vendor = new Vendor { Id = 101, Name = "Apex Steel Mill", Code = "VEND-101" };
            db.Vendors.Add(vendor);

            var bill = new PurchaseBill
            {
                Id = 501,
                BillNumber = "BILL-2026-000501",
                VendorId = vendor.Id,
                VendorName = vendor.Name,
                InvoiceNumber = "INV-APEX-9921",
                InvoiceDate = DateTime.UtcNow.AddDays(-5),
                GrandTotal = 150000m,
                PaidAmount = 0m,
                BalanceAmount = 150000m,
                Status = PurchaseBillStatus.Posted
            };
            db.PurchaseBills.Add(bill);
            await db.SaveChangesAsync();

            var entry = await engine.PostBillPayableAsync(bill.Id);

            Assert.NotNull(entry);
            Assert.Equal("Bill", entry.VoucherType);
            Assert.Equal(150000m, entry.CreditAmount);
            Assert.Equal(0m, entry.DebitAmount);
            Assert.Equal(150000m, entry.OutstandingAmount);
            Assert.Equal("Unsettled", entry.SettlementStatus);
            Assert.Equal(vendor.Id, entry.VendorId);
        }

        [Fact]
        public async Task VendorLedger_PostPayment_CreatesDebitVoucher()
        {
            using var db = CreateDbContext();
            var analytics = new ProcurementSpendAnalyticsService(db, NullLogger<ProcurementSpendAnalyticsService>.Instance);
            var engine = new VendorPayablesLedgerEngine(db, NullLogger<VendorPayablesLedgerEngine>.Instance, analytics);

            var payment = new VendorPayment
            {
                Id = 701,
                VendorId = 101,
                PaymentNumber = "PAY-2026-000701",
                PaymentDate = DateTime.UtcNow,
                PaymentMethod = "NEFT",
                TotalAmount = 50000m,
                UnallocatedAmount = 0m,
                Status = "Cleared",
                ReferenceNumber = "UTR-99128381"
            };
            db.VendorPayments.Add(payment);
            await db.SaveChangesAsync();

            var entry = await engine.PostPaymentDisbursementAsync(payment.Id);

            Assert.NotNull(entry);
            Assert.Equal("Payment", entry.VoucherType);
            Assert.Equal(50000m, entry.DebitAmount);
            Assert.Equal(0m, entry.CreditAmount);
            Assert.Equal("FullySettled", entry.SettlementStatus);
        }

        [Fact]
        public async Task BillSettlement_InvoiceSpecific_LiquidatesOutstandingAndMarksPaid()
        {
            using var db = CreateDbContext();
            var analytics = new ProcurementSpendAnalyticsService(db, NullLogger<ProcurementSpendAnalyticsService>.Instance);
            var engine = new VendorPayablesLedgerEngine(db, NullLogger<VendorPayablesLedgerEngine>.Instance, analytics);

            var vendor = new Vendor { Id = 201, Name = "Precision Fasteners Ltd", Code = "VEND-201" };
            db.Vendors.Add(vendor);

            var bill = new PurchaseBill
            {
                Id = 601,
                BillNumber = "BILL-2026-000601",
                VendorId = vendor.Id,
                VendorName = vendor.Name,
                InvoiceNumber = "INV-FAST-100",
                GrandTotal = 25000m,
                PaidAmount = 0m,
                BalanceAmount = 25000m,
                Status = PurchaseBillStatus.Posted
            };
            db.PurchaseBills.Add(bill);

            var ledgerEntry = new VendorLedgerEntry
            {
                Id = 901,
                VendorId = vendor.Id,
                PurchaseBillId = bill.Id,
                VoucherType = "Bill",
                CreditAmount = 25000m,
                DebitAmount = 0m,
                OutstandingAmount = 25000m,
                SettledAmount = 0m,
                SettlementStatus = "Unsettled"
            };
            db.VendorLedgerEntries.Add(ledgerEntry);
            await db.SaveChangesAsync();

            var request = new SettleBillRequestDto
            {
                VendorId = vendor.Id,
                TotalSettlementAmount = 25000m,
                Allocations = new List<SettleBillAllocationItemDto>
                {
                    new SettleBillAllocationItemDto
                    {
                        PurchaseBillId = bill.Id,
                        AllocatedAmount = 25000m,
                        PaymentExchangeRate = 1.0m
                    }
                }
            };

            var result = await engine.SettleBillAsync(request);

            Assert.True(result.Success);
            Assert.Equal(25000m, result.TotalSettledAmount);

            var updatedBill = await db.PurchaseBills.FindAsync(bill.Id);
            Assert.NotNull(updatedBill);
            Assert.Equal(0m, updatedBill.OutstandingAmount);
            Assert.Equal(25000m, updatedBill.PaidAmount);
            Assert.Equal(PurchaseBillStatus.Paid, updatedBill.Status);
            Assert.Equal(PurchaseBillPaymentStatus.Paid, updatedBill.PaymentStatus);

            var updatedLedger = await db.VendorLedgerEntries.FindAsync(ledgerEntry.Id);
            Assert.NotNull(updatedLedger);
            Assert.Equal(0m, updatedLedger.OutstandingAmount);
            Assert.Equal(25000m, updatedLedger.SettledAmount);
            Assert.Equal("FullySettled", updatedLedger.SettlementStatus);
        }

        [Fact]
        public async Task BillSettlement_RealizedFx_CalculatesGainAndLoss()
        {
            using var db = CreateDbContext();
            var analytics = new ProcurementSpendAnalyticsService(db, NullLogger<ProcurementSpendAnalyticsService>.Instance);
            var engine = new VendorPayablesLedgerEngine(db, NullLogger<VendorPayablesLedgerEngine>.Instance, analytics);

            var vendor = new Vendor { Id = 301, Name = "Global Robotics Inc (USA)", Code = "VEND-301" };
            db.Vendors.Add(vendor);

            // Import Bill booked at exchange rate ₹85.00 / USD
            var bill = new PurchaseBill
            {
                Id = 602,
                BillNumber = "BILL-2026-USD-01",
                VendorId = vendor.Id,
                VendorName = vendor.Name,
                InvoiceNumber = "INV-USA-4412",
                Currency = "USD",
                ExchangeRate = 85.00m,
                GrandTotal = 1000m, // 1,000 USD
                PaidAmount = 0m,
                BalanceAmount = 1000m,
                Status = PurchaseBillStatus.Posted
            };
            db.PurchaseBills.Add(bill);
            await db.SaveChangesAsync();

            // Case A: Settlement payment executed at ₹83.00 / USD (Dollar depreciated => Realized FX Gain = 1000 * (85 - 83) = ₹2,000)
            var request = new SettleBillRequestDto
            {
                VendorId = vendor.Id,
                TotalSettlementAmount = 1000m,
                Allocations = new List<SettleBillAllocationItemDto>
                {
                    new SettleBillAllocationItemDto
                    {
                        PurchaseBillId = bill.Id,
                        AllocatedAmount = 1000m,
                        PaymentExchangeRate = 83.00m
                    }
                }
            };

            var result = await engine.SettleBillAsync(request);

            Assert.True(result.Success);
            Assert.Equal(2000.00m, result.TotalRealizedFxGainLoss);

            var settlement = await db.VendorBillSettlements.FirstOrDefaultAsync(s => s.PurchaseBillId == bill.Id);
            Assert.NotNull(settlement);
            Assert.Equal(2000.00m, settlement.RealizedFxGainLoss);
        }

        [Fact]
        public async Task BillSettlement_FifoKnockOff_KnocksOffOldestInvoicesFirst()
        {
            using var db = CreateDbContext();
            var analytics = new ProcurementSpendAnalyticsService(db, NullLogger<ProcurementSpendAnalyticsService>.Instance);
            var engine = new VendorPayablesLedgerEngine(db, NullLogger<VendorPayablesLedgerEngine>.Instance, analytics);

            var vendor = new Vendor { Id = 401, Name = "Industrial Tools Corp", Code = "VEND-401" };
            db.Vendors.Add(vendor);

            var billOld = new PurchaseBill
            {
                Id = 701,
                BillNumber = "BILL-001",
                VendorId = vendor.Id,
                VendorName = vendor.Name,
                InvoiceDate = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
                GrandTotal = 10000m,
                BalanceAmount = 10000m,
                Status = PurchaseBillStatus.Posted
            };
            var billNew = new PurchaseBill
            {
                Id = 702,
                BillNumber = "BILL-002",
                VendorId = vendor.Id,
                VendorName = vendor.Name,
                InvoiceDate = new DateTime(2026, 1, 25, 0, 0, 0, DateTimeKind.Utc),
                GrandTotal = 20000m,
                BalanceAmount = 20000m,
                Status = PurchaseBillStatus.Posted
            };
            db.PurchaseBills.AddRange(billOld, billNew);

            var payment = new VendorPayment
            {
                Id = 801,
                VendorId = vendor.Id,
                PaymentNumber = "PAY-FIFO-01",
                TotalAmount = 15000m,
                PaymentMethod = "BankTransfer"
            };
            db.VendorPayments.Add(payment);
            await db.SaveChangesAsync();

            // Knock off ₹15,000 using FIFO:
            // Should completely clear Bill 701 (₹10,000) and partially clear Bill 702 (₹5,000, leaving ₹15,000 open).
            var request = new FifoKnockOffRequestDto
            {
                VendorId = vendor.Id,
                PaymentVoucherId = payment.Id,
                AvailablePaymentAmount = 15000m
            };

            var result = await engine.AutoKnockOffFifoAsync(request);

            Assert.True(result.Success);
            Assert.Equal(15000m, result.TotalSettledAmount);
            Assert.Equal(2, result.BillsSettledCount);

            var bOld = await db.PurchaseBills.FindAsync(701);
            Assert.Equal(0m, bOld!.OutstandingAmount);
            Assert.Equal(PurchaseBillStatus.Paid, bOld.Status);

            var bNew = await db.PurchaseBills.FindAsync(702);
            Assert.Equal(15000m, bNew!.OutstandingAmount);
            Assert.Equal(5000m, bNew.PaidAmount);
            Assert.Equal(PurchaseBillPaymentStatus.PartiallyPaid, bNew.PaymentStatus);
        }

        [Fact]
        public async Task ApAging_CategorizesOpenBillsInto5Buckets()
        {
            using var db = CreateDbContext();
            var agingService = new AccountsPayableAgingService(db);

            var vendor = new Vendor
            {
                Id = 501,
                Name = "Apex Electricals",
                Code = "VEND-501",
                MsmeCategory = MsmeCategory.Medium
            };
            db.Vendors.Add(vendor);

            var asOfDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

            var billNotDue = new PurchaseBill
            {
                Id = 801,
                VendorId = vendor.Id,
                BillNumber = "BILL-NOTDUE",
                InvoiceDate = new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc),
                DueDate = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc), // Future
                GrandTotal = 5000m,
                BalanceAmount = 5000m,
                Status = PurchaseBillStatus.Posted
            };

            var bill10DaysOverdue = new PurchaseBill
            {
                Id = 802,
                VendorId = vendor.Id,
                BillNumber = "BILL-10DAYS",
                InvoiceDate = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
                DueDate = new DateTime(2026, 2, 19, 0, 0, 0, DateTimeKind.Utc), // 10 days past due
                GrandTotal = 10000m,
                BalanceAmount = 10000m,
                Status = PurchaseBillStatus.Posted
            };

            var bill50DaysOverdue = new PurchaseBill
            {
                Id = 803,
                VendorId = vendor.Id,
                BillNumber = "BILL-50DAYS",
                InvoiceDate = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                DueDate = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc), // 50 days past due
                GrandTotal = 20000m,
                BalanceAmount = 20000m,
                Status = PurchaseBillStatus.Posted
            };

            db.PurchaseBills.AddRange(billNotDue, bill10DaysOverdue, bill50DaysOverdue);
            await db.SaveChangesAsync();

            var aging = await agingService.CalculateAgingScheduleAsync(asOfDate, vendor.Id);

            Assert.Equal(35000m, aging.TotalOutstanding);
            Assert.Equal(5000m, aging.TotalNotDue);
            Assert.Equal(10000m, aging.Total1To15Days);
            Assert.Equal(20000m, aging.TotalOver45Days);

            var row = aging.VendorRows.First();
            Assert.Equal("Medium", row.MsmeCategory);
            Assert.True(row.HasOverdueBreach);
        }

        [Fact]
        public async Task ApAging_MsmeSection43B_FlagsOverdueBillsAndComputesCompoundInterest()
        {
            using var db = CreateDbContext();
            var agingService = new AccountsPayableAgingService(db);

            var msmeVendor = new Vendor
            {
                Id = 601,
                Name = "Micro Precision Components",
                Code = "VEND-601",
                MsmeCategory = MsmeCategory.Micro
            };
            db.Vendors.Add(msmeVendor);

            var bill = new PurchaseBill
            {
                Id = 901,
                BillNumber = "BILL-MSME-01",
                VendorId = msmeVendor.Id,
                InvoiceDate = DateTime.UtcNow.AddDays(-60),
                MsmeDueDate = DateTime.UtcNow.AddDays(-15), // 15 days overdue past statutory cap
                MsmeAllowedDays = 45,
                IsMsmeSubjectTo43B = true,
                GrandTotal = 100000m,
                BalanceAmount = 100000m,
                Status = PurchaseBillStatus.Posted
            };
            db.PurchaseBills.Add(bill);
            await db.SaveChangesAsync();

            var alerts = await agingService.GetMsmeSection43BAlertsAsync();

            Assert.Single(alerts);
            var alert = alerts[0];
            Assert.Equal("BILL-MSME-01", alert.BillNumber);
            Assert.True(alert.OverdueDays >= 14);
            Assert.True(alert.ProjectedInterestAt3xRepoRate > 0);
        }

        [Fact]
        public void WorkflowSla_BusinessHoursCalculation_OmitsWeekendsAndComputesAccurateDuration()
        {
            // Friday 15:00 UTC to Monday 12:00 UTC (9:00 to 18:00 window):
            // Friday: 15:00 to 18:00 = 3.0 hours
            // Saturday: 0 hours (weekend)
            // Sunday: 0 hours (weekend)
            // Monday: 09:00 to 12:00 = 3.0 hours
            // Total = 6.0 business hours
            var friday1500 = new DateTime(2026, 2, 20, 15, 0, 0, DateTimeKind.Utc);
            var monday1200 = new DateTime(2026, 2, 23, 12, 0, 0, DateTimeKind.Utc);

            decimal elapsed = ProcurementWorkflowSlaEngine.CalculateBusinessHours(friday1500, monday1200);

            Assert.Equal(6.00m, elapsed);
        }

        [Fact]
        public async Task WorkflowSla_BreachDetection_FlagsBreachedStages()
        {
            using var db = CreateDbContext();
            var engine = new ProcurementWorkflowSlaEngine(db, NullLogger<ProcurementWorkflowSlaEngine>.Instance);

            // Target SLA = 2.0 hours
            var log = await engine.StartStageAsync("PO", 101, "PO-2026-000101", 4, "4. Matrix Award -> PO Authorization", 2.0m, "Executive Management");

            // Complete stage manually after elapsed time exceeds target SLA
            var mondayStart = new DateTime(2026, 2, 23, 9, 0, 0, DateTimeKind.Utc); // Monday 09:00 UTC
            log.StartedAt = mondayStart;
            await db.SaveChangesAsync();

            // Completed at 15:00 UTC = 6 business hours > 2.0 target SLA
            var completed = await engine.CompleteStageAsync("PO", 101, 4, 1, "CEO", "Authorized with DOA", mondayStart.AddHours(6));

            Assert.NotNull(completed.CompletedAt);
            Assert.True(completed.IsSlaBreached);
        }

        [Fact]
        public async Task SpendAnalytics_Ppv_IdentifiesFavorableAndUnfavorableVariances()
        {
            using var db = CreateDbContext();
            var analytics = new ProcurementSpendAnalyticsService(db, NullLogger<ProcurementSpendAnalyticsService>.Instance);

            var rawMaterial = new RawMaterial
            {
                Id = 11,
                MaterialCode = "RM-STEEL-01",
                MaterialName = "Cold Rolled Steel Sheet",
                UnitCost = 100.00m // Standard cost = ₹100
            };
            db.RawMaterials.Add(rawMaterial);

            var grn = new GoodsReceipt
            {
                Id = 201,
                GRNNumber = "GRN-2026-000201",
                ReceiptDate = DateTime.UtcNow,
                VendorName = "Jindal Steel",
                Status = GoodsReceiptStatus.Completed
            };

            // Item 1: Purchased at ₹90 (Favorable: savings ₹10/unit * 100 = -₹1,000)
            var itemFavorable = new GoodsReceiptItem
            {
                Id = 301,
                GoodsReceiptId = grn.Id,
                GoodsReceipt = grn,
                ItemId = rawMaterial.Id,
                RawMaterial = rawMaterial,
                ItemName = rawMaterial.MaterialName,
                OrderedQuantity = 100m,
                QcAcceptedQuantity = 100m,
                PoRate = 90.00m
            };

            // Item 2: Purchased at ₹115 (Unfavorable: premium ₹15/unit * 50 = +₹750)
            var itemUnfavorable = new GoodsReceiptItem
            {
                Id = 302,
                GoodsReceiptId = grn.Id,
                GoodsReceipt = grn,
                ItemId = rawMaterial.Id,
                RawMaterial = rawMaterial,
                ItemName = rawMaterial.MaterialName,
                OrderedQuantity = 50m,
                QcAcceptedQuantity = 50m,
                PoRate = 115.00m
            };

            grn.Items.Add(itemFavorable);
            grn.Items.Add(itemUnfavorable);
            db.GoodsReceipts.Add(grn);
            db.GoodsReceiptItems.AddRange(itemFavorable, itemUnfavorable);
            await db.SaveChangesAsync();

            var dbGrns = await db.GoodsReceipts.Include(g => g.Items).ToListAsync();
            Assert.Single(dbGrns);
            Assert.Equal(2, dbGrns[0].Items.Count);

            var report = await analytics.CalculatePpvAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));

            Assert.Equal(2, report.TotalLineItemsEvaluated);
            // Net PPV = -1000 + 750 = -250 (Net Favorable)
            Assert.Equal(-250.00m, report.NetPpvAmount);

            var fav = report.Items.First(i => i.ActualUnitPrice == 90m);
            Assert.Equal("Favorable", fav.VarianceType);
            Assert.Equal(-1000m, fav.TotalPpv);

            var unfav = report.Items.First(i => i.ActualUnitPrice == 115m);
            Assert.Equal("Unfavorable", unfav.VarianceType);
            Assert.Equal(750m, unfav.TotalPpv);
        }

        [Fact]
        public async Task ForensicAuditChaining_ValidatesCleanChainAndDetectsTampering()
        {
            using var db = CreateDbContext();
            var config = CreateConfiguration();
            var auditEngine = new ForensicAuditChainingEngine(db, config, NullLogger<ForensicAuditChainingEngine>.Instance);

            // 1. Append 3 blocks into cryptographic chain
            var block1 = await auditEngine.AppendAuditLogAsync("PurchaseBill", "101", "CREATE", "AdminUser", "127.0.0.1", null, new { Total = 50000 });
            var block2 = await auditEngine.AppendAuditLogAsync("PurchaseBill", "101", "DOA_OVERRIDE", "FinanceDirector", "127.0.0.1", new { Status = "Hold" }, new { Status = "Approved", Reason = "Urgent Delivery" });
            var block3 = await auditEngine.AppendAuditLogAsync("PurchaseBill", "101", "POST_LEDGER", "System", null, null, new { LedgerId = 991 });

            Assert.Equal(1, block1.BlockIndex);
            Assert.Equal(2, block2.BlockIndex);
            Assert.Equal(3, block3.BlockIndex);
            Assert.Equal(new string('0', 64), block1.PreviousLogHash);
            Assert.Equal(block1.CurrentLogHash, block2.PreviousLogHash);
            Assert.Equal(block2.CurrentLogHash, block3.PreviousLogHash);

            // 2. Verify clean chain
            var verifyClean = await auditEngine.VerifyChainIntegrityAsync();
            Assert.True(verifyClean.IsChainValid);
            Assert.Equal(3, verifyClean.TotalBlocksVerified);

            // 3. Simulate unauthorized out-of-band tampering in PostgreSQL: modify Block 2 payload directly
            block2.NewValuesJson = "{\"Status\":\"Approved\",\"Reason\":\"TAMPERED_WITHOUT_APPROVAL\"}";
            await db.SaveChangesAsync();

            // 4. Verify chain fails and pinpoints Block 2
            var verifyTampered = await auditEngine.VerifyChainIntegrityAsync();
            Assert.False(verifyTampered.IsChainValid);
            Assert.Equal(2, verifyTampered.TamperedBlockIndex);
            Assert.Contains("Tampering detected at Block #2", verifyTampered.VerificationSummary);
        }
    }
}
