using ERP.Domain.Sales;

namespace ERP.Application.Sales
{
    public static class DiscountApprovalStatusRules
    {
        private static readonly Dictionary<string, string[]> Allowed = new(StringComparer.OrdinalIgnoreCase)
        {
            [DiscountApprovalStatuses.Pending] =
            [
                DiscountApprovalStatuses.UnderReview,
                DiscountApprovalStatuses.Approved,
                DiscountApprovalStatuses.Rejected,
                DiscountApprovalStatuses.Returned,
                DiscountApprovalStatuses.Cancelled
            ],
            [DiscountApprovalStatuses.UnderReview] =
            [
                DiscountApprovalStatuses.Approved,
                DiscountApprovalStatuses.Rejected,
                DiscountApprovalStatuses.Returned,
                DiscountApprovalStatuses.Cancelled
            ],
            [DiscountApprovalStatuses.Returned] =
            [
                DiscountApprovalStatuses.Pending
            ],
            [DiscountApprovalStatuses.Approved] = [],
            [DiscountApprovalStatuses.Rejected] = [],
            [DiscountApprovalStatuses.Cancelled] = []
        };

        public static string? Normalize(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return null;
            }

            return DiscountApprovalStatuses.All.FirstOrDefault(s =>
                s.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase)
                || s.Replace(" ", "", StringComparison.Ordinal)
                    .Equals(status.Replace(" ", "", StringComparison.OrdinalIgnoreCase),
                        StringComparison.OrdinalIgnoreCase));
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

        public static bool IsEditable(string status) =>
            Normalize(status) is DiscountApprovalStatuses.Pending
                or DiscountApprovalStatuses.Returned;

        public static bool IsTerminal(string status) =>
            Normalize(status) is DiscountApprovalStatuses.Approved
                or DiscountApprovalStatuses.Rejected
                or DiscountApprovalStatuses.Cancelled;
    }

    public static class DiscountApprovalPriorityRules
    {
        public static string? Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return DiscountApprovalPriorities.All.FirstOrDefault(s =>
                s.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }

    public static class DiscountApprovalLevelRules
    {
        public static string? Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return DiscountApprovalLevels.All.FirstOrDefault(s =>
                s.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Approval matrix: maps requested discount % to the minimum required level.
        /// </summary>
        public static string RequiredLevelForDiscount(decimal discountPercentage) =>
            discountPercentage switch
            {
                <= 5m => DiscountApprovalLevels.Auto,
                <= 10m => DiscountApprovalLevels.Manager,
                <= 20m => DiscountApprovalLevels.Admin,
                _ => DiscountApprovalLevels.Escalated
            };

        public static bool LevelSatisfies(string providedLevel, string requiredLevel)
        {
            var order = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                [DiscountApprovalLevels.Auto] = 1,
                [DiscountApprovalLevels.Manager] = 2,
                [DiscountApprovalLevels.Admin] = 3,
                [DiscountApprovalLevels.Escalated] = 4
            };

            var provided = Normalize(providedLevel);
            var required = Normalize(requiredLevel);
            if (provided is null || required is null)
            {
                return false;
            }

            return order[provided] >= order[required];
        }
    }

    public static class DiscountApprovalSourceTypeRules
    {
        public static string? Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return DiscountApprovalSourceTypes.All.FirstOrDefault(s =>
                s.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase)
                || s.Replace(" ", "", StringComparison.Ordinal)
                    .Equals(value.Replace(" ", "", StringComparison.OrdinalIgnoreCase),
                        StringComparison.OrdinalIgnoreCase));
        }
    }
}
