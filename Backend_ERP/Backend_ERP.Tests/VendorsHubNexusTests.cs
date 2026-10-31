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
    public class VendorsHubNexusTests
    {
        private ERPDbContext CreateInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ERPDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new ERPDbContext(options);
        }

        [Fact]
        public void GstinMod36Checksum_ValidGstin_PassesVerification()
        {
            // Standard valid Maharashtra GSTIN format: 27AAAAA0000A1Z5
            // Let's compute the valid check digit for first 14 chars
            var first14 = "27AABCB1234F1Z";
            var checkDigit = VendorRules.CalculateGstinCheckDigit(first14);
            var validGstin = first14 + checkDigit;

            Assert.True(VendorRules.ValidateGstinMod36Checksum(validGstin));
            Assert.True(VendorRules.IsValidGstin(validGstin));
        }

        [Fact]
        public void GstinMod36Checksum_TamperedCheckDigit_FailsVerification()
        {
            var first14 = "27AABCB1234F1Z";
            var checkDigit = VendorRules.CalculateGstinCheckDigit(first14);
            // Intentionally corrupt check digit
            var corruptedChar = checkDigit == "5" ? "9" : "5";
            var invalidGstin = first14 + corruptedChar;

            Assert.False(VendorRules.ValidateGstinMod36Checksum(invalidGstin));
            Assert.False(VendorRules.IsValidGstin(invalidGstin));
        }

        [Fact]
        public void GstinStateCodeInvariance_MatchingState_Passes()
        {
            var gstin = "27AABCB1234F1Z5"; // State code 27 = Maharashtra
            Assert.True(VendorRules.ValidateGstinStateMatch(gstin, "Maharashtra"));
            Assert.True(VendorRules.ValidateGstinStateMatch(gstin, "27"));
        }

        [Fact]
        public void GstinStateCodeInvariance_MismatchedState_Fails()
        {
            var gstin = "27AABCB1234F1Z5"; // State code 27 = Maharashtra
            Assert.False(VendorRules.ValidateGstinStateMatch(gstin, "Karnataka")); // Karnataka is 29
            Assert.False(VendorRules.ValidateGstinStateMatch(gstin, "Gujarat")); // Gujarat is 24
        }

        [Fact]
        public void MsmeSection43Bh_PaymentTerms_AutoCappedAt45Days()
        {
            // Requested 60 days for Micro/Small vendor
            var cappedDays = VendorRules.ComputeMsmePaymentTermsDays(60, hasWrittenContract: true);
            Assert.Equal(45, cappedDays);

            // Without written contract, auto-capped at 15 days
            var statutoryDaysNoContract = VendorRules.ComputeMsmePaymentTermsDays(30, hasWrittenContract: false);
            Assert.Equal(15, statutoryDaysNoContract);
        }

        [Fact]
        public void MsmeSection43Bh_DueDateCalculation_EnforcesStatutoryBoundaries()
        {
            var billDate = new DateOnly(2026, 10, 1);

            // With agreement (max 45 days)
            var dueDateContract = VendorRules.CalculateMsmeStatutoryDueDate(billDate, 60, hasWrittenAgreement: true);
            Assert.Equal(billDate.AddDays(45), dueDateContract);

            // Without agreement (max 15 days)
            var dueDateNoContract = VendorRules.CalculateMsmeStatutoryDueDate(billDate, 30, hasWrittenAgreement: false);
            Assert.Equal(billDate.AddDays(15), dueDateNoContract);
        }

        [Fact]
        public async Task SourcingGate_RestrictedItem_ValidatesMoqAndMultiples()
        {
            var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var catalogService = new VendorCatalogService(db);

            var vendor = new Vendor
            {
                Id = 1,
                TenantId = "default",
                VendorCode = "VND-TEST-001",
                Name = "Apex Fasteners Ltd",
                Status = VendorStatus.Active,
                IsKycVerified = true
            };
            db.Vendors.Add(vendor);

            var catalogItem = new VendorItemCatalog
            {
                Id = 1,
                TenantId = "default",
                VendorId = 1,
                ItemCode = "RM-STEEL-001",
                ItemType = "RawMaterial",
                StandardLeadTimeDays = 14,
                MinimumOrderQuantity = 50.0000m,
                OrderMultiple = 10.0000m,
                PackagingUnit = "Bundles",
                BaseUomConversionMultiplier = 1.000000m,
                UnitPriceSnapshot = 1200.00m,
                EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
                EffectiveTo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(100)),
                IsApproved = true
            };
            db.VendorItemCatalogs.Add(catalogItem);
            await db.SaveChangesAsync();

            // 1. Quantity below MOQ (30 < 50) -> BLOCKED
            var moqResult = await catalogService.ValidateItemSourcingGateAsync("default", 1, "RM-STEEL-001", 30m);
            Assert.False(moqResult.IsAllowed);
            Assert.Contains("below the Approved Vendor MOQ", moqResult.Reason);

            // 2. Quantity not in multiples of 10 (55 % 10 != 0) -> BLOCKED
            var multipleResult = await catalogService.ValidateItemSourcingGateAsync("default", 1, "RM-STEEL-001", 55m);
            Assert.False(multipleResult.IsAllowed);
            Assert.Contains("exact packaging multiples", multipleResult.Reason);

            // 3. Valid Order (60 is >= 50 and multiple of 10) -> ALLOWED
            var validResult = await catalogService.ValidateItemSourcingGateAsync("default", 1, "RM-STEEL-001", 60m);
            Assert.True(validResult.IsAllowed);
            Assert.NotNull(validResult.CatalogItem);
        }

        [Fact]
        public async Task SourcingGate_UnverifiedKycVendor_BlocksPurchaseOrder()
        {
            var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var catalogService = new VendorCatalogService(db);

            var vendor = new Vendor
            {
                Id = 2,
                TenantId = "default",
                VendorCode = "VND-TEST-002",
                Name = "Unverified Supplier Co",
                Status = VendorStatus.Active,
                IsKycVerified = false
            };
            db.Vendors.Add(vendor);
            await db.SaveChangesAsync();

            var gateResult = await catalogService.ValidateItemSourcingGateAsync("default", 2, "RM-STEEL-001", 100m);
            Assert.False(gateResult.IsAllowed);
            Assert.Contains("not KYC verified", gateResult.Reason);

            // Also test PendingKycApproval status
            vendor.Status = VendorStatus.PendingKycApproval;
            await db.SaveChangesAsync();
            var pendingResult = await catalogService.ValidateItemSourcingGateAsync("default", 2, "RM-STEEL-001", 100m);
            Assert.False(pendingResult.IsAllowed);
            Assert.Contains("POs can only be placed with 'Active' vendors", pendingResult.Reason);
        }

        [Fact]
        public async Task VendorScoringEngine_BlendedFormula_CalculatesWeightedScoreAccurately()
        {
            var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var scoringEngine = new VendorScoringEngine(db, NullLogger<VendorScoringEngine>.Instance);

            var vendor = new Vendor
            {
                Id = 3,
                TenantId = "default",
                VendorCode = "VND-TEST-003",
                Name = "High Quality Supplier",
                Status = VendorStatus.Active,
                IsKycVerified = true,
                RatingScore = 100.00m
            };
            db.Vendors.Add(vendor);
            await db.SaveChangesAsync();

            // Formula: (0.35 * 90) + (0.35 * 100) + (0.15 * 80) + (0.15 * 90)
            // = 31.5 + 35.0 + 12.0 + 13.5 = 92.00
            var ingestion = new VendorLotInspectionIngestionDto
            {
                VendorId = 3,
                FiscalYear = 2026,
                Month = 10,
                OtifPercentage = 90.00m,
                QualityPassRate = 100.00m,
                PriceVarianceScore = 80.00m,
                ComplianceScore = 90.00m,
                LotsReceived = 10,
                LotsRejected = 0,
                HasCriticalSafetyDefect = false
            };

            var score = await scoringEngine.IngestLotInspectionAndScoreAsync(3, ingestion, "qa_lead");

            Assert.Equal(92.00m, score.BlendedRatingScore);
            Assert.Equal(0m, score.DefectPartsPerMillion);
            Assert.Equal(VendorStatus.Active, vendor.Status);
        }

        [Fact]
        public async Task VendorScoringEngine_ScoreBelow60_TriggersCircuitBreakerDisqualification()
        {
            var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var scoringEngine = new VendorScoringEngine(db, NullLogger<VendorScoringEngine>.Instance);

            var vendor = new Vendor
            {
                Id = 4,
                TenantId = "default",
                VendorCode = "VND-TEST-004",
                Name = "Struggling Vendor",
                Status = VendorStatus.Active,
                IsKycVerified = true,
                RatingScore = 80.00m
            };
            db.Vendors.Add(vendor);
            await db.SaveChangesAsync();

            // Severe failure: OTIF 40%, Quality 50%, Price 40%, Compliance 50%
            // Score = (0.35*40) + (0.35*50) + (0.15*40) + (0.15*50) = 14 + 17.5 + 6 + 7.5 = 45.00 (< 60.00)
            var ingestion = new VendorLotInspectionIngestionDto
            {
                VendorId = 4,
                FiscalYear = 2026,
                Month = 10,
                OtifPercentage = 40.00m,
                QualityPassRate = 50.00m,
                PriceVarianceScore = 40.00m,
                ComplianceScore = 50.00m,
                LotsReceived = 10,
                LotsRejected = 5,
                HasCriticalSafetyDefect = false
            };

            var score = await scoringEngine.IngestLotInspectionAndScoreAsync(4, ingestion, "qa_lead");

            Assert.Equal(45.00m, score.BlendedRatingScore);
            Assert.Equal(500000.00m, score.DefectPartsPerMillion); // 5/10 * 1,000,000 = 500,000 PPM
            Assert.Equal(VendorStatus.OnHold, vendor.Status);
            Assert.Contains("RATING CIRCUIT-BREAKER", vendor.Notes);
        }

        [Fact]
        public async Task VendorScoringEngine_CriticalSafetyDefect_TriggersImmediateDisqualification()
        {
            var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var scoringEngine = new VendorScoringEngine(db, NullLogger<VendorScoringEngine>.Instance);

            var vendor = new Vendor
            {
                Id = 5,
                TenantId = "default",
                VendorCode = "VND-TEST-005",
                Name = "Safety Critical Vendor",
                Status = VendorStatus.Active,
                IsKycVerified = true,
                RatingScore = 95.00m
            };
            db.Vendors.Add(vendor);
            await db.SaveChangesAsync();

            var ingestion = new VendorLotInspectionIngestionDto
            {
                VendorId = 5,
                FiscalYear = 2026,
                Month = 10,
                OtifPercentage = 95.00m,
                QualityPassRate = 85.00m,
                PriceVarianceScore = 90.00m,
                ComplianceScore = 90.00m,
                LotsReceived = 10,
                LotsRejected = 1,
                HasCriticalSafetyDefect = true,
                CriticalDefectDescription = "Structural hairline crack detected in primary turbine housing lot."
            };

            var score = await scoringEngine.IngestLotInspectionAndScoreAsync(5, ingestion, "qa_lead");

            Assert.Equal(VendorStatus.Disqualified, vendor.Status);
            Assert.Contains("CRITICAL DEFECT", vendor.Notes);
            Assert.Contains("Structural hairline crack", vendor.Notes);
        }
    }
}

