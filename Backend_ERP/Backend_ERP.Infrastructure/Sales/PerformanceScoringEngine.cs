using System;
using System.Collections.Generic;
using System.Text.Json;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;

namespace ERP.Infrastructure.Sales
{
    public class PerformanceScoringEngine : IPerformanceScoringEngine
    {
        public PerformanceScoreEvaluationResult EvaluateRepScore(
            decimal attainmentPercentage,
            decimal cashCollectionPercentage,
            decimal grossMarginPercentage,
            decimal avgDealVelocityDays,
            decimal conversionRate,
            decimal pacingIndex)
        {
            // 1. Quota Attainment Component (Weight 40%) - Cap normalized at 120
            decimal sQuota = Math.Min(120m, Math.Max(0m, attainmentPercentage));

            // 2. Cash Velocity Component (Weight 25%) - Collections relative to bookings
            decimal sCash = Math.Min(100m, Math.Max(0m, cashCollectionPercentage));

            // 3. Margin Discipline Component (Weight 20%) - Target 25% GM = 100
            decimal sMargin = Math.Min(100m, Math.Max(0m, (grossMarginPercentage / 25.00m) * 100m));

            // 4. Deal Velocity Component (Weight 15%) - < 14 days = high score
            decimal sVelocity = 70m;
            if (avgDealVelocityDays > 0m)
            {
                if (avgDealVelocityDays <= 7m) sVelocity = 100m;
                else if (avgDealVelocityDays <= 14m) sVelocity = 90m;
                else if (avgDealVelocityDays <= 30m) sVelocity = 75m;
                else sVelocity = Math.Max(20m, 100m - (avgDealVelocityDays * 1.5m));
            }

            // Weighted Composite Calculation
            decimal rawScore = (sQuota * 0.40m) + (sCash * 0.25m) + (sMargin * 0.20m) + (sVelocity * 0.15m);
            decimal weightedScore = Math.Round(rawScore, 2, MidpointRounding.AwayFromZero);

            // Tier Badge Allocation
            string tierBadge = weightedScore switch
            {
                >= 90.00m => "Diamond",
                >= 80.00m => "Platinum",
                >= 70.00m => "Gold",
                _ => "Silver"
            };

            // Discount Discipline Score (0 to 100)
            decimal discountDisciplineScore = Math.Round(
                Math.Clamp(100m - Math.Max(0m, (25.0m - grossMarginPercentage) * 4.0m), 0m, 100m),
                1, MidpointRounding.AwayFromZero);

            return new PerformanceScoreEvaluationResult(
                WeightedScore: weightedScore,
                TierBadge: tierBadge,
                DiscountDisciplineScore: discountDisciplineScore
            );
        }

        public RadarSkillMatrixDto CalculateRadarSkills(
            decimal attainmentPercentage,
            decimal cashCollectionPercentage,
            decimal grossMarginPercentage,
            decimal avgDealVelocityDays,
            decimal conversionRate)
        {
            return new RadarSkillMatrixDto
            {
                QuotaAttainmentScore = Math.Round(Math.Clamp(attainmentPercentage, 0m, 100m), 1),
                CashVelocityScore = Math.Round(Math.Clamp(cashCollectionPercentage, 0m, 100m), 1),
                MarginDisciplineScore = Math.Round(Math.Clamp((grossMarginPercentage / 28.0m) * 100m, 0m, 100m), 1),
                DealVelocityScore = Math.Round(avgDealVelocityDays <= 7m && avgDealVelocityDays > 0m ? 100m : Math.Clamp(100m - (avgDealVelocityDays * 2m), 20m, 100m), 1),
                WinRateScore = Math.Round(Math.Clamp(conversionRate, 0m, 100m), 1)
            };
        }

        public List<SalesRepBadge> EvaluateBadges(
            int salesPersonUserId,
            int profileId,
            int financialYear,
            decimal attainmentPercentage,
            decimal cashCollectionPercentage,
            decimal grossMarginPercentage,
            decimal avgDealVelocityDays,
            int currentRank,
            int winningStreakMonths,
            decimal pacingIndex)
        {
            var badges = new List<SalesRepBadge>();

            if (attainmentPercentage >= 100m)
            {
                badges.Add(new SalesRepBadge
                {
                    ProfileId = profileId,
                    SalesPersonUserId = salesPersonUserId,
                    BadgeCode = "CENTURY_CLUB",
                    BadgeTitle = "Century Club",
                    BadgeCategory = "Quota",
                    Icon = "military_tech",
                    Color = "emerald",
                    EvaluationCriteriaSnapshot = JsonSerializer.Serialize(new { attainmentPercentage }),
                    AwardedDate = DateTimeOffset.UtcNow
                });
            }

            if (grossMarginPercentage >= 28.0m)
            {
                badges.Add(new SalesRepBadge
                {
                    ProfileId = profileId,
                    SalesPersonUserId = salesPersonUserId,
                    BadgeCode = "MARGIN_GUARDIAN",
                    BadgeTitle = "Margin Guardian",
                    BadgeCategory = "Margin",
                    Icon = "verified_user",
                    Color = "blue",
                    EvaluationCriteriaSnapshot = JsonSerializer.Serialize(new { grossMarginPercentage }),
                    AwardedDate = DateTimeOffset.UtcNow
                });
            }

            if (cashCollectionPercentage >= 100m)
            {
                badges.Add(new SalesRepBadge
                {
                    ProfileId = profileId,
                    SalesPersonUserId = salesPersonUserId,
                    BadgeCode = "CASH_VELOCITY_CHAMPION",
                    BadgeTitle = "Cash Velocity Champion",
                    BadgeCategory = "Cash",
                    Icon = "payments",
                    Color = "cyan",
                    EvaluationCriteriaSnapshot = JsonSerializer.Serialize(new { cashCollectionPercentage }),
                    AwardedDate = DateTimeOffset.UtcNow
                });
            }

            if (avgDealVelocityDays > 0m && avgDealVelocityDays <= 14m)
            {
                badges.Add(new SalesRepBadge
                {
                    ProfileId = profileId,
                    SalesPersonUserId = salesPersonUserId,
                    BadgeCode = "FAST_CLOSER",
                    BadgeTitle = "Fast Closer",
                    BadgeCategory = "Velocity",
                    Icon = "speed",
                    Color = "amber",
                    EvaluationCriteriaSnapshot = JsonSerializer.Serialize(new { avgDealVelocityDays }),
                    AwardedDate = DateTimeOffset.UtcNow
                });
            }

            if (winningStreakMonths >= 3)
            {
                badges.Add(new SalesRepBadge
                {
                    ProfileId = profileId,
                    SalesPersonUserId = salesPersonUserId,
                    BadgeCode = "STREAK_MASTER",
                    BadgeTitle = "Streak Master",
                    BadgeCategory = "Consistency",
                    Icon = "local_fire_department",
                    Color = "orange",
                    EvaluationCriteriaSnapshot = JsonSerializer.Serialize(new { winningStreakMonths }),
                    AwardedDate = DateTimeOffset.UtcNow
                });
            }

            if (currentRank == 1 || pacingIndex >= 1.15m)
            {
                badges.Add(new SalesRepBadge
                {
                    ProfileId = profileId,
                    SalesPersonUserId = salesPersonUserId,
                    BadgeCode = "PACESETTER",
                    BadgeTitle = "Pacesetter",
                    BadgeCategory = "Leadership",
                    Icon = "workspace_premium",
                    Color = "gold",
                    EvaluationCriteriaSnapshot = JsonSerializer.Serialize(new { currentRank, pacingIndex }),
                    AwardedDate = DateTimeOffset.UtcNow
                });
            }

            return badges;
        }

        public decimal CalculatePacingIndex(
            decimal attainmentPercentage,
            int financialYear)
        {
            var fyStart = new DateTime(financialYear - 1, 4, 1, 0, 0, 0, DateTimeKind.Utc);
            var fyEnd = new DateTime(financialYear, 3, 31, 23, 59, 59, DateTimeKind.Utc);
            var now = DateTime.UtcNow;

            if (now < fyStart) return 1.0m;
            if (now > fyEnd) return Math.Round(attainmentPercentage / 100m, 2);

            double totalDays = (fyEnd - fyStart).TotalDays;
            double elapsedDays = (now - fyStart).TotalDays;
            decimal expectedAttainment = (decimal)(elapsedDays / totalDays) * 100m;

            if (expectedAttainment <= 0m) return 1.0m;

            return Math.Round(attainmentPercentage / expectedAttainment, 2, MidpointRounding.AwayFromZero);
        }
    }
}
