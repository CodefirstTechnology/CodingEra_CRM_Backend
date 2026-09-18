using ERP.Domain.Sales;
using System.Text.Json;
using Xunit;

namespace ERP.Tests
{
    public class PerformancePhase2Tests
    {
        [Fact]
        public void Test_Badge_CenturyClub_Earned()
        {
            var badges = PerformanceBadgeService.EvaluateBadges(
                attainmentPercentage: 105m,
                grossMarginPercentage: 20m,
                avgDealVelocityDays: 10m,
                currentRank: 5);

            var json = JsonSerializer.Serialize(badges);

            Assert.Contains("CENTURY_CLUB", json);
            Assert.DoesNotContain("MARGIN_PROTECTOR", json);
            Assert.DoesNotContain("PACESETTER", json);
        }

        [Fact]
        public void Test_Badge_MarginProtector_Earned()
        {
            var badges = PerformanceBadgeService.EvaluateBadges(
                attainmentPercentage: 80m,
                grossMarginPercentage: 28m,
                avgDealVelocityDays: 12m,
                currentRank: 4);

            var json = JsonSerializer.Serialize(badges);

            Assert.Contains("MARGIN_PROTECTOR", json);
            Assert.DoesNotContain("CENTURY_CLUB", json);
        }

        [Fact]
        public void Test_Badge_Pacesetter_For_Rank_1()
        {
            var badges = PerformanceBadgeService.EvaluateBadges(
                attainmentPercentage: 110m,
                grossMarginPercentage: 30m,
                avgDealVelocityDays: 5m,
                currentRank: 1);

            var json = JsonSerializer.Serialize(badges);

            Assert.Contains("PACESETTER", json);
            Assert.Contains("CENTURY_CLUB", json);
            Assert.Contains("MARGIN_PROTECTOR", json);
            Assert.Contains("FAST_CLOSER", json);
        }

        [Fact]
        public void Test_Incentive_Statement_Calculation()
        {
            decimal targetValue = 100000m;
            decimal achievedValue = 130000m;
            decimal achievementPercentage = 130m;

            // Base incentive: 1.5% of target for >= 100% attainment
            decimal baseIncentive = achievementPercentage >= 100m ? (targetValue * 0.015m) : 0m;
            // Super-Achiever Kicker: 2.5% of over-achievement for >= 120% attainment
            decimal kicker = achievementPercentage >= 120m ? ((achievedValue - targetValue) * 0.025m) : 0m;

            Assert.Equal(1500m, Math.Round(baseIncentive, 2));
            Assert.Equal(750m, Math.Round(kicker, 2));
            Assert.Equal(2250m, Math.Round(baseIncentive + kicker, 2));
        }
    }
}
