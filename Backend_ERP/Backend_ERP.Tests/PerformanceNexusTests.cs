using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Domain.Sales.Events;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Sales;
using ERP.Infrastructure.Sales.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Backend_ERP.Tests
{
    public class PerformanceNexusTests
    {
        private ERPDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ERPDbContext>()
                .UseInMemoryDatabase(databaseName: $"PerformanceNexusTestDb_{Guid.NewGuid()}")
                .Options;

            return new ERPDbContext(options);
        }

        [Fact]
        public async Task DualHurdleMarginCap_WhenMarginBelowHurdle_CapsAcceleratorAtOneX()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var engine = new SalesIncentiveEngine(context, NullLogger<SalesIncentiveEngine>.Instance);

            // Add rep performance with 130% attainment (Tier 3 accelerator eligible)
            var perf = new SalespersonPerformance
            {
                SalesPersonUserId = 101,
                SalesPersonName = "Sarah Jenkins",
                FinancialYear = 2026,
                TotalTargetValue = 1000000m,
                TotalAchievedValue = 1300000m,
                AttainmentPercentage = 130.00m,
                GrossMarginPercentage = 18.00m // Breaches 22.0% hurdle!
            };
            context.SalespersonPerformances.Add(perf);
            await context.SaveChangesAsync();

            // Act - Order with 18.0% gross margin
            var result = await engine.CalculateCommissionForOrderAsync(
                salesPersonUserId: 101,
                financialYear: 2026,
                orderAmount: 200000m,
                grossMarginPercentage: 18.00m,
                orderNumber: "SO-101",
                orderId: 1,
                advancePaymentClearedPercentage: 60m);

            // Assert
            Assert.False(result.MarginHurdlePassed);
            Assert.Equal(1.0m, result.AcceleratorMultiplier); // Capped at 1.0x!
            Assert.Contains("capped at 1.0x", result.Notes);
            Assert.Equal(6000m, result.CalculatedCommission); // 200,000 * 3.0% * 1.0x = 6,000
        }

        [Fact]
        public async Task DualHurdleMarginCap_WhenMarginAboveHurdle_UnlocksTierTwoAccelerator()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var engine = new SalesIncentiveEngine(context, NullLogger<SalesIncentiveEngine>.Instance);

            // Rep with 110% attainment (eligible for 1.5x)
            var perf = new SalespersonPerformance
            {
                SalesPersonUserId = 102,
                SalesPersonName = "Michael Scott",
                FinancialYear = 2026,
                TotalTargetValue = 1000000m,
                TotalAchievedValue = 1100000m,
                AttainmentPercentage = 110.00m,
                GrossMarginPercentage = 25.00m // Exceeds 22.0% hurdle!
            };
            context.SalespersonPerformances.Add(perf);
            await context.SaveChangesAsync();

            // Act - Order with 25.0% gross margin
            var result = await engine.CalculateCommissionForOrderAsync(
                salesPersonUserId: 102,
                financialYear: 2026,
                orderAmount: 100000m,
                grossMarginPercentage: 25.00m,
                orderNumber: "SO-102",
                orderId: 2,
                advancePaymentClearedPercentage: 70m);

            // Assert
            Assert.True(result.MarginHurdlePassed);
            Assert.Equal(1.5m, result.AcceleratorMultiplier); // Unlocked 1.5x!
            Assert.Equal(4500m, result.CalculatedCommission); // 100,000 * 3.0% * 1.5x = 4,500
        }

        [Fact]
        public async Task CashGatedState_WhenAdvanceBelowMinClearance_GatesCommissionUntilCleared()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var engine = new SalesIncentiveEngine(context, NullLogger<SalesIncentiveEngine>.Instance);

            var perf = new SalespersonPerformance
            {
                SalesPersonUserId = 103,
                SalesPersonName = "Jim Halpert",
                FinancialYear = 2026,
                TotalTargetValue = 500000m,
                TotalAchievedValue = 500000m,
                AttainmentPercentage = 100.00m,
                GrossMarginPercentage = 24.00m
            };
            context.SalespersonPerformances.Add(perf);
            await context.SaveChangesAsync();

            // Act 1: Accrue commission with only 20% advance cleared (< 50% min clearance required)
            var ledger = await engine.AccrueOrderCommissionAsync(
                salesPersonUserId: 103,
                financialYear: 2026,
                orderId: 301,
                orderNumber: "SO-301",
                orderAmount: 300000m,
                grossMarginPercentage: 24.00m,
                advancePaymentClearedPercentage: 20.00m);

            // Assert 1
            Assert.Equal("GatedPendingCash", ledger.PayoutStatus);

            // Act 2: Advance cleared reaches 65%
            int releasedCount = await engine.ReleaseCashGatedCommissionsAsync(301, 65.00m);

            // Assert 2
            Assert.Equal(1, releasedCount);
            var updatedLedger = await context.SalesCommissionLedgers.FindAsync(ledger.Id);
            Assert.Equal("ApprovedForPayment", updatedLedger?.PayoutStatus);
        }

        [Fact]
        public async Task Clawback_WhenOrderCancelled_RecordsCompensatingReversalEntry()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var engine = new SalesIncentiveEngine(context, NullLogger<SalesIncentiveEngine>.Instance);

            var perf = new SalespersonPerformance
            {
                SalesPersonUserId = 104,
                SalesPersonName = "Pam Beesly",
                FinancialYear = 2026,
                TotalTargetValue = 500000m,
                TotalAchievedValue = 300000m,
                AttainmentPercentage = 60.00m
            };
            context.SalespersonPerformances.Add(perf);
            await context.SaveChangesAsync();

            // Accrue order commission
            var accrued = await engine.AccrueOrderCommissionAsync(
                salesPersonUserId: 104,
                financialYear: 2026,
                orderId: 401,
                orderNumber: "SO-401",
                orderAmount: 200000m,
                grossMarginPercentage: 23.00m,
                advancePaymentClearedPercentage: 80.00m);

            // Act: Process Clawback
            var clawback = await engine.RecordClawbackAsync(
                salesPersonUserId: 104,
                financialYear: 2026,
                orderId: 401,
                orderNumber: "SO-401",
                originalCommissionAmount: accrued.AccruedCommission,
                reason: "Customer Insolvency & Cancellation");

            // Assert
            Assert.NotNull(clawback);
            Assert.True(clawback.IsClawback);
            Assert.Equal("Clawback", clawback.TransactionType);
            Assert.Equal("ClawedBack", clawback.PayoutStatus);
            Assert.Equal(accrued.AccruedCommission, clawback.AccruedCommission);
            Assert.Equal("Customer Insolvency & Cancellation", clawback.ClawbackReason);
        }

        [Fact]
        public async Task RedisLeaderboard_UpdatesScoreAndComputesRankingsWithDeltas()
        {
            // Arrange
            var cache = new PerformanceRedisCacheManager(null); // in-memory fallback

            // Act
            await cache.UpdateLeaderboardScoreAsync("default", 2026, "weightedscore", 201, 95.5);
            await cache.UpdateLeaderboardScoreAsync("default", 2026, "weightedscore", 202, 88.0);
            await cache.UpdateLeaderboardScoreAsync("default", 2026, "weightedscore", 203, 72.5);

            var topRanks = await cache.GetTopRanksAsync("default", 2026, "weightedscore", 3);
            var rank201 = await cache.GetUserRankAsync("default", 2026, "weightedscore", 201);
            var rank202 = await cache.GetUserRankAsync("default", 2026, "weightedscore", 202);

            // Assert
            Assert.Equal(3, topRanks.Count);
            Assert.Equal(201, topRanks[0].SalesPersonUserId);
            Assert.Equal(95.5, topRanks[0].Score);
            Assert.Equal(1, rank201);
            Assert.Equal(2, rank202);
        }

        [Fact]
        public void PerformanceScoringEngine_CalculatesCompositeScoreAndBadgesAccurately()
        {
            // Arrange
            var engine = new PerformanceScoringEngine();

            // Act
            var eval = engine.EvaluateRepScore(
                attainmentPercentage: 115.00m,
                cashCollectionPercentage: 95.00m,
                grossMarginPercentage: 27.50m,
                avgDealVelocityDays: 8.5m,
                conversionRate: 45.0m,
                pacingIndex: 1.20m);

            var radar = engine.CalculateRadarSkills(
                attainmentPercentage: 115.00m,
                cashCollectionPercentage: 95.00m,
                grossMarginPercentage: 27.50m,
                avgDealVelocityDays: 8.5m,
                conversionRate: 45.0m);

            var badges = engine.EvaluateBadges(
                salesPersonUserId: 301,
                profileId: 1,
                financialYear: 2026,
                attainmentPercentage: 115.00m,
                cashCollectionPercentage: 95.00m,
                grossMarginPercentage: 28.50m,
                avgDealVelocityDays: 10.0m,
                currentRank: 1,
                winningStreakMonths: 4,
                pacingIndex: 1.25m);

            // Assert
            Assert.True(eval.WeightedScore >= 80m);
            Assert.Contains(eval.TierBadge, new[] { "Diamond", "Platinum" });

            Assert.Equal(100m, radar.QuotaAttainmentScore); // capped at 100 for radar
            Assert.Equal(95m, radar.CashVelocityScore);
            Assert.True(radar.MarginDisciplineScore >= 95m);

            Assert.Contains(badges, b => b.BadgeCode == "CENTURY_CLUB");
            Assert.Contains(badges, b => b.BadgeCode == "MARGIN_GUARDIAN");
            Assert.Contains(badges, b => b.BadgeCode == "FAST_CLOSER");
            Assert.Contains(badges, b => b.BadgeCode == "STREAK_MASTER");
            Assert.Contains(badges, b => b.BadgeCode == "PACESETTER");
        }

        [Fact]
        public async Task PerformanceReports_GeneratesAccuratePipAuditAndVelocityMatrix()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var reportService = new PerformanceReportService(context, NullLogger<PerformanceReportService>.Instance);

            // Add at-risk rep
            context.SalespersonPerformances.Add(new SalespersonPerformance
            {
                SalesPersonUserId = 501,
                SalesPersonName = "Dwight Schrute",
                FinancialYear = 2026,
                TotalTargetValue = 1000000m,
                TotalAchievedValue = 250000m,
                AttainmentPercentage = 25.00m,
                PacingIndex = 0.45m, // Critical!
                Status = "Active"
            });

            // Add high velocity rep
            context.SalespersonPerformances.Add(new SalespersonPerformance
            {
                SalesPersonUserId = 502,
                SalesPersonName = "Stanley Hudson",
                FinancialYear = 2026,
                TotalTargetValue = 1000000m,
                TotalAchievedValue = 950000m,
                AttainmentPercentage = 95.00m,
                ConfirmedOrderCount = 20,
                TotalQuotationCount = 25,
                ConversionRate = 80.00m,
                AvgDealVelocityDays = 7.5m, // Fast!
                TotalBookedRevenue = 950000m,
                PacingIndex = 1.05m,
                Status = "Active"
            });

            await context.SaveChangesAsync();

            // Act 1: PIP Audit
            var pipReport = await reportService.GetPipEarlyWarningAuditAsync(2026, null, null);

            // Act 2: Velocity Matrix
            var velReport = await reportService.GetRepCohortVelocityMatrixAsync(2026, null, null);

            // Act 3: CSV Export
            var csv = await reportService.ExportReportToCsvAsync("pip", 2026, null, null);

            // Assert 1: PIP Audit
            var atRisk = pipReport.AtRiskReps.FirstOrDefault(r => r.SalesPersonUserId == 501);
            Assert.NotNull(atRisk);
            Assert.Equal("Critical", atRisk.RiskLevel);
            Assert.Contains("30-day PIP", atRisk.RecommendedRemediation);

            // Assert 2: Velocity Matrix
            var fastRep = velReport.Rows.FirstOrDefault(r => r.SalesPersonUserId == 502);
            Assert.NotNull(fastRep);
            Assert.Equal("Fast", fastRep.VelocityCohort);
            Assert.Equal(47500m, fastRep.AverageDealSize);

            // Assert 3: CSV Export
            Assert.NotNull(csv);
            Assert.True(csv.Length > 0);
        }
    }
}

