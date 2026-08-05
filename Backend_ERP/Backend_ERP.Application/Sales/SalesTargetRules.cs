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
            Round2(achievedValue) > Round2(targetValue);

        public static string ResolveStatusAfterProgress(string currentStatus, decimal achievementPercentage)
        {
            if (achievementPercentage >= 100m
                && currentStatus is SalesTargetStatuses.Active or SalesTargetStatuses.Completed)
            {
                return SalesTargetStatuses.Completed;
            }

            return currentStatus;
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
                SalesTargetStatuses.Cancelled
            ],
            [SalesTargetStatuses.Completed] = [],
            [SalesTargetStatuses.Expired] = [],
            [SalesTargetStatuses.Cancelled] = []
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
