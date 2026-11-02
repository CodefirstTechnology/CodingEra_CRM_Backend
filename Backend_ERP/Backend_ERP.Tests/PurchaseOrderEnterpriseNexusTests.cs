using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Backend_ERP.Tests
{
    public class PurchaseOrderEnterpriseNexusTests
    {
        private ERPDbContext CreateInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ERPDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new ERPDbContext(options);
        }

        [Theory]
        [InlineData(50000, PurchaseOrderApprovalTier.Tier1)]
        [InlineData(100000, PurchaseOrderApprovalTier.Tier1)]
        [InlineData(500000, PurchaseOrderApprovalTier.Tier2)]
        [InlineData(1000000, PurchaseOrderApprovalTier.Tier2)]
        [InlineData(2500000, PurchaseOrderApprovalTier.Tier3)]
        [InlineData(5000000, PurchaseOrderApprovalTier.Tier3)]
        [InlineData(7500000, PurchaseOrderApprovalTier.Tier4)]
        public async Task DOATierEvaluation_MonetaryBoundaries_SelectsCorrectTier(decimal orderAmount, PurchaseOrderApprovalTier expectedTier)
        {
            using var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var engine = new PurchaseOrderApprovalEngine(db, NullLogger<PurchaseOrderApprovalEngine>.Instance);

            var po = new PurchaseOrder
            {
                TotalAmount = orderAmount,
                GrandTotalBaseCurrency = orderAmount,
                PaymentTermsDays = 30
            };

            var (tier, isException, reasons) = await engine.EvaluateDOARequirementsAsync(po, vendorRatingScore: 85m);

            Assert.Equal(expectedTier, tier);
            Assert.False(isException);
            Assert.Empty(reasons);
        }

        [Fact]
        public async Task DOATierEvaluation_UnbudgetedCapEx_EscalatesToTier4()
        {
            using var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var engine = new PurchaseOrderApprovalEngine(db, NullLogger<PurchaseOrderApprovalEngine>.Instance);

            // Amount ₹40,000 would normally be Tier 1, but unbudgeted CapEx must escalate to Tier 4
            var po = new PurchaseOrder
            {
                TotalAmount = 40_000m,
                GrandTotalBaseCurrency = 40_000m,
                PaymentTermsDays = 30
            };

            var (tier, isException, reasons) = await engine.EvaluateDOARequirementsAsync(
                po, 
                vendorRatingScore: 90m, 
                isUnbudgetedCapEx: true);

            Assert.Equal(PurchaseOrderApprovalTier.Tier4, tier);
            Assert.True(isException);
            Assert.Contains(reasons, r => r.Contains("Unbudgeted CapEx"));
        }

        [Fact]
        public async Task DOATierEvaluation_MsmePaymentTermsBreach_EscalatesToTier4()
        {
            using var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var engine = new PurchaseOrderApprovalEngine(db, NullLogger<PurchaseOrderApprovalEngine>.Instance);

            // Amount ₹50,000 but payment terms 60 days (> 45 days)
            var po = new PurchaseOrder
            {
                TotalAmount = 50_000m,
                GrandTotalBaseCurrency = 50_000m,
                PaymentTermsDays = 60
            };

            var (tier, isException, reasons) = await engine.EvaluateDOARequirementsAsync(po, vendorRatingScore: 88m);

            Assert.Equal(PurchaseOrderApprovalTier.Tier4, tier);
            Assert.True(isException);
            Assert.Contains(reasons, r => r.Contains("MSME Section 43B(h)"));
        }

        [Fact]
        public async Task DOATierEvaluation_LowVendorRatingScore_EscalatesToTier3()
        {
            using var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var engine = new PurchaseOrderApprovalEngine(db, NullLogger<PurchaseOrderApprovalEngine>.Instance);

            // Amount ₹80,000 (normally Tier 1), but vendor score is 62.50 (< 70.00)
            var po = new PurchaseOrder
            {
                TotalAmount = 80_000m,
                GrandTotalBaseCurrency = 80_000m,
                PaymentTermsDays = 30
            };

            var (tier, isException, reasons) = await engine.EvaluateDOARequirementsAsync(po, vendorRatingScore: 62.50m);

            Assert.Equal(PurchaseOrderApprovalTier.Tier3, tier);
            Assert.True(isException);
            Assert.Contains(reasons, r => r.Contains("score is critically low"));
        }

        [Fact]
        public async Task DOATierEvaluation_NonAvlEmergency_EscalatesToTier3()
        {
            using var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var engine = new PurchaseOrderApprovalEngine(db, NullLogger<PurchaseOrderApprovalEngine>.Instance);

            var po = new PurchaseOrder
            {
                TotalAmount = 75_000m,
                GrandTotalBaseCurrency = 75_000m,
                PaymentTermsDays = 30
            };

            var (tier, isException, reasons) = await engine.EvaluateDOARequirementsAsync(
                po, 
                vendorRatingScore: 85m,
                isNonAvlEmergencyOrder: true);

            Assert.Equal(PurchaseOrderApprovalTier.Tier3, tier);
            Assert.True(isException);
            Assert.Contains(reasons, r => r.Contains("Emergency procurement placed with non-AVL vendor"));
        }

        [Fact]
        public void HmacSignOff_GenerateAndVerify_PassesForValidToken_FailsWhenTampered()
        {
            using var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var engine = new PurchaseOrderApprovalEngine(db, NullLogger<PurchaseOrderApprovalEngine>.Instance);

            var actionedAt = new DateTime(2026, 11, 2, 10, 30, 0, DateTimeKind.Utc);
            string token = engine.GenerateHmacSignature(
                tenantId: 1, 
                poNumber: "PO-2026-00042", 
                revisionNumber: 0, 
                totalAmount: 150000.50m, 
                approverUserId: "usr-cfo-99", 
                actionedAt: actionedAt);

            Assert.False(string.IsNullOrWhiteSpace(token));

            // Valid verification
            bool isValid = engine.VerifyHmacSignature(
                token, 
                tenantId: 1, 
                poNumber: "PO-2026-00042", 
                revisionNumber: 0, 
                totalAmount: 150000.50m, 
                approverUserId: "usr-cfo-99", 
                actionedAt: actionedAt);
            Assert.True(isValid);

            // Tampered amount fails
            bool isTamperedAmount = engine.VerifyHmacSignature(
                token, 
                tenantId: 1, 
                poNumber: "PO-2026-00042", 
                revisionNumber: 0, 
                totalAmount: 150000.51m, // corrupted by 1 paisa
                approverUserId: "usr-cfo-99", 
                actionedAt: actionedAt);
            Assert.False(isTamperedAmount);

            // Tampered user fails
            bool isTamperedUser = engine.VerifyHmacSignature(
                token, 
                tenantId: 1, 
                poNumber: "PO-2026-00042", 
                revisionNumber: 0, 
                totalAmount: 150000.50m, 
                approverUserId: "usr-impostor-01", 
                actionedAt: actionedAt);
            Assert.False(isTamperedUser);
        }

        [Fact]
        public async Task POAmendmentService_SpawnsRevisionR1_FreezesPredecessor_TracksDeltaAudit()
        {
            using var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var approvalEngine = new PurchaseOrderApprovalEngine(db, NullLogger<PurchaseOrderApprovalEngine>.Instance);
            var amendmentService = new PurchaseOrderAmendmentService(db, approvalEngine, NullLogger<PurchaseOrderAmendmentService>.Instance);

            // Seed approved predecessor PO R0
            var predecessor = new PurchaseOrder
            {
                TenantId = 1,
                PurchaseOrderNumber = "PO-2026-00088",
                RevisionNumber = 0,
                IsCurrentRevision = true,
                Status = PurchaseOrderStatus.Approved,
                VendorName = "Precision Castings Ltd",
                VendorGstin = "27AABCP1234D1Z5",
                VendorStateCode = "27",
                PaymentTermsDays = 30,
                ExchangeRate = 1.0m,
                TotalAmount = 250000m,
                GrandTotalBaseCurrency = 250000m,
                Lines = new List<PurchaseOrderLine>
                {
                    new()
                    {
                        LineNumber = 1,
                        ItemName = "High-Tensile Bolts M12",
                        Unit = "Nos",
                        Quantity = 1000m,
                        Rate = 250m,
                        Amount = 250000m,
                        LineTotal = 250000m,
                        LineStatus = PurchaseOrderLineStatus.Pending
                    }
                }
            };
            db.PurchaseOrders.Add(predecessor);
            await db.SaveChangesAsync();

            // Execute amendment request: change quantity and payment terms
            var amendmentReq = new POAmendmentRequestDto
            {
                AmendmentReason = "Client order quantity increased by 20% and extended terms",
                AmendmentType = POAmendmentType.Commercial,
                PaymentTermsDays = 40,
                Lines = new List<EnterprisePOLineInputDto>
                {
                    new()
                    {
                        LineNumber = 1,
                        ItemName = "High-Tensile Bolts M12",
                        Unit = "Nos",
                        Quantity = 1200m, // 1000 -> 1200
                        Rate = 250m,
                        DiscountPercent = 0m,
                        GstRatePercentage = 18m,
                        OverDeliveryTolerancePercent = 0m
                    }
                }
            };

            var newRevision = await amendmentService.AmendPurchaseOrderAsync(predecessor.Id, amendmentReq, "lead_buyer");

            // Verify predecessor state
            var refreshedPredecessor = await db.PurchaseOrders.FindAsync(predecessor.Id);
            Assert.NotNull(refreshedPredecessor);
            Assert.False(refreshedPredecessor.IsCurrentRevision);
            Assert.Equal(PurchaseOrderStatus.Superseded, refreshedPredecessor.Status);
            Assert.NotNull(refreshedPredecessor.SupersededAt);

            // Verify new revision state
            Assert.NotNull(newRevision);
            Assert.Equal("PO-2026-00088", newRevision.PurchaseOrderNumber);
            Assert.Equal(1, newRevision.RevisionNumber);
            Assert.True(newRevision.IsCurrentRevision);
            Assert.Equal(predecessor.Id, newRevision.ParentPOId);
            Assert.Equal(PurchaseOrderStatus.PendingApproval, newRevision.Status);
            Assert.Equal(40, newRevision.PaymentTermsDays);

            // Verify delta history
            var auditDeltas = await amendmentService.GetAmendmentAuditTrailAsync(newRevision.Id);
            Assert.NotEmpty(auditDeltas);
            Assert.Contains(auditDeltas, d => d.ChangedField == "TotalAmount" || d.ChangedField == "PaymentTermsDays");
        }

        [Fact]
        public async Task StaggeredCallOff_DeliveryScheduleSum_ValidationThrowsOnMismatch()
        {
            using var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var approvalEngine = new PurchaseOrderApprovalEngine(db, NullLogger<PurchaseOrderApprovalEngine>.Instance);
            var amendmentService = new PurchaseOrderAmendmentService(db, approvalEngine, NullLogger<PurchaseOrderAmendmentService>.Instance);

            var po = new PurchaseOrder
            {
                TenantId = 1,
                PurchaseOrderNumber = "PO-2026-00099",
                RevisionNumber = 0,
                IsCurrentRevision = true,
                Status = PurchaseOrderStatus.Approved,
                VendorName = "Steel Industries Ltd",
                VendorGstin = "27AABCS1234F1Z8",
                VendorStateCode = "27",
                TotalAmount = 500000m
            };
            db.PurchaseOrders.Add(po);
            await db.SaveChangesAsync();

            // Mismatched schedules: ordered 500 kg, but schedules sum to 400 kg
            var amendmentReq = new POAmendmentRequestDto
            {
                AmendmentReason = "Introduce call-off schedules",
                Lines = new List<EnterprisePOLineInputDto>
                {
                    new()
                    {
                        LineNumber = 1,
                        ItemName = "Hot Rolled Steel Coils",
                        Unit = "Kg",
                        Quantity = 500m,
                        Rate = 80m,
                        DeliverySchedules = new List<DeliveryScheduleItemDto>
                        {
                            new() { ScheduleNumber = 1, Quantity = 200m, ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7) },
                            new() { ScheduleNumber = 2, Quantity = 200m, ExpectedDeliveryDate = DateTime.UtcNow.AddDays(14) }
                            // Total is only 400m != 500m
                        }
                    }
                }
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                amendmentService.AmendPurchaseOrderAsync(po.Id, amendmentReq, "buyer_user"));
        }
    }
}

