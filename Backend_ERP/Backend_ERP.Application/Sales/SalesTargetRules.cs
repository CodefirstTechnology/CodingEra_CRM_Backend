using ERP.Domain.Sales;

namespace ERP.Application.Sales
{
    public static class SalesTargetCalculator
    {
        public static decimal Round2(decimal value) =>
            Math.Round(value, 2, MidpointRounding.AwayFromZero);

        public static decimal CalcRemaining(decimal targetValue, decimal achievedValue) =>
            Round2(Math.Max(0, targetValue) - Math.Max(0, achievedValue));

        public static decimal CalcAchievementPercentage(decimal targetValue, decimal achievedValue)
        {
            if (targetValue <= 0)
            {
                return 0m;
            }

            return Round2(Math.Max(0, achievedValue) / targetValue * 100m);
        }

        public static bool WouldExceedTarget(decimal targetValue, decimal achievedValue) =>
            achievedValue > targetValue;

        public static void RecalculateProgress(SalesTarget target)
        {
            target.RemainingValue = Math.Max(0m, target.TargetValue - target.AchievedValue);
            target.OverAchievementValue = Math.Max(0m, target.AchievedValue - target.TargetValue);

            target.AchievementPercentage = target.TargetValue > 0
                ? Math.Round((target.AchievedValue / target.TargetValue) * 100m, 2, MidpointRounding.AwayFromZero)
                : 0m;
        }

        public static string ResolveStatusAfterProgress(string currentStatus, decimal achievementPercentage)
        {
            // Do NOT prematurely complete; targets stay Active until EndDate lapses to track over-achievement
            return currentStatus;
        }
    }

    public static class SalesTargetRules
    {
        public static void RecalculateProgress(SalesTarget target)
        {
            SalesTargetCalculator.RecalculateProgress(target);
        }
    }

    public static class SalesTargetStatusRules
    {
        private static readonly Dictionary<string, string[]> Allowed = new(StringComparer.OrdinalIgnoreCase)
        {
            [SalesTargetStatuses.Draft] =
            [
                SalesTargetStatuses.Active,
                SalesTargetStatuses.Cancelled
            ],
            [SalesTargetStatuses.Active] =
            [
                SalesTargetStatuses.Completed,
                SalesTargetStatuses.Expired,
                SalesTargetStatuses.Cancelled,
                SalesTargetStatuses.Closed
            ],
            [SalesTargetStatuses.Completed] = [SalesTargetStatuses.Closed],
            [SalesTargetStatuses.Expired] = [SalesTargetStatuses.Closed],
            [SalesTargetStatuses.Cancelled] = [],
            [SalesTargetStatuses.Closed] = []
        };

        public static string? Normalize(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return null;
            }

            return SalesTargetStatuses.All.FirstOrDefault(s =>
                s.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public static bool CanTransition(string from, string to)
        {
            var fromNorm = Normalize(from);
            var toNorm = Normalize(to);
            if (fromNorm is null || toNorm is null)
            {
                return false;
            }

            if (fromNorm.Equals(toNorm, StringComparison.Ordinal))
            {
                return true;
            }

            return Allowed.TryGetValue(fromNorm, out var next)
                && next.Contains(toNorm, StringComparer.Ordinal);
        }
    }

    public static class SalesTargetTypeRules
    {
        public static string? Normalize(string? value) => SalesTargetEnumLookup.Normalize(SalesTargetTypes.All, value);
    }

    public static class SalesTargetCategoryRules
    {
        public static string? Normalize(string? value) =>
            SalesTargetEnumLookup.Normalize(SalesTargetCategories.All, value);
    }

    public static class SalesTargetAssignmentTypeRules
    {
        public static string? Normalize(string? value) =>
            SalesTargetEnumLookup.Normalize(SalesTargetAssignmentTypes.All, value);
    }

    internal static class SalesTargetEnumLookup
    {
        public static string? Normalize(IEnumerable<string> all, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return all.FirstOrDefault(s =>
                s.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase)
                || s.Replace(" ", "", StringComparison.Ordinal)
                    .Equals(value.Replace(" ", "", StringComparison.OrdinalIgnoreCase),
                        StringComparison.OrdinalIgnoreCase));
        }
    }
}
