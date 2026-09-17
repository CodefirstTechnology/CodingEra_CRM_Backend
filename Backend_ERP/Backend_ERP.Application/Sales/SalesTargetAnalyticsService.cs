using System;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;

namespace ERP.Application.Sales
{
    public static class SalesTargetAnalyticsService
    {
        public static decimal CalculateCommission(decimal targetValue, decimal achievedValue)
        {
            if (targetValue <= 0 || achievedValue <= 0) return 0m;
            decimal attainment = (achievedValue / targetValue) * 100m;

            if (attainment < 80m) return 0m;

            if (attainment < 100m)
                return Math.Round(achievedValue * 0.005m, 2, MidpointRounding.AwayFromZero); // 0.5%

            if (attainment < 120m)
                return Math.Round(achievedValue * 0.015m, 2, MidpointRounding.AwayFromZero); // 1.5%

            // >= 120%: Base 1.5% up to target + 2.5% on over-achievement
            decimal baseCommission = targetValue * 0.015m;
            decimal overAchievedAmount = achievedValue - targetValue;
            decimal kicker = overAchievedAmount * 0.025m;

            return Math.Round(baseCommission + kicker, 2, MidpointRounding.AwayFromZero);
        }

        public static (string Bracket, decimal AcceleratorRate) GetCommissionBracket(decimal targetValue, decimal achievedValue)
        {
            if (targetValue <= 0 || achievedValue <= 0) return ("0-79%", 0m);
            decimal attainment = (achievedValue / targetValue) * 100m;

            if (attainment < 80m) return ("0-79%", 0m);
            if (attainment < 100m) return ("80-99%", 0.005m);
            if (attainment < 120m) return ("100-119%", 0.015m);
            return (">=120%", 0.025m);
        }

        public static ForecastMetricsDto ComputeForecast(SalesTarget target, DateOnly asOfDate)
        {
            var totalDays = Math.Max(1, target.EndDate.DayNumber - target.StartDate.DayNumber + 1);
            var daysElapsed = Math.Clamp(asOfDate.DayNumber - target.StartDate.DayNumber + 1, 1, totalDays);
            var remainingDays = Math.Max(1, target.EndDate.DayNumber - asOfDate.DayNumber);

            decimal currentRunRate = target.AchievedValue / daysElapsed;
            decimal requiredRunRate = target.RemainingValue / remainingDays;
            decimal projectedAttainment = target.TargetValue > 0
                ? Math.Round(((currentRunRate * totalDays) / target.TargetValue) * 100m, 2, MidpointRounding.AwayFromZero)
                : 0m;

            string health = projectedAttainment >= 100m ? "OnTrack" : (projectedAttainment >= 80m ? "AtRisk" : "Critical");

            return new ForecastMetricsDto(
                Math.Round(requiredRunRate, 2, MidpointRounding.AwayFromZero),
                Math.Round(currentRunRate, 2, MidpointRounding.AwayFromZero),
                projectedAttainment,
                health
            );
        }
    }
}
