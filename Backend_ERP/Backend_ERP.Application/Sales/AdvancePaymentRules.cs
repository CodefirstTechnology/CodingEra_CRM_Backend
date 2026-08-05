using ERP.Domain.Sales;

namespace ERP.Application.Sales
{
    public static class AdvancePaymentCalculator
    {
        public static decimal Round2(decimal value) =>
            Math.Round(value, 2, MidpointRounding.AwayFromZero);

        public static decimal CalcRemaining(decimal advanceAmount, decimal appliedAmount) =>
            Round2(Math.Max(0, advanceAmount) - Math.Max(0, appliedAmount));

        public static bool WouldOverAllocate(decimal remainingAmount, decimal applyAmount) =>
            Round2(applyAmount) > Round2(remainingAmount);

        public static string ResolveStatusAfterApply(decimal remainingAmount) =>
            remainingAmount <= 0
                ? AdvancePaymentStatuses.FullyApplied
                : AdvancePaymentStatuses.PartiallyApplied;
    }

    public static class AdvancePaymentStatusRules
    {
        private static readonly Dictionary<string, string[]> Allowed = new(StringComparer.OrdinalIgnoreCase)
        {
            [AdvancePaymentStatuses.Draft] =
            [
                AdvancePaymentStatuses.Submitted,
                AdvancePaymentStatuses.Cancelled
            ],
            [AdvancePaymentStatuses.Submitted] =
            [
                AdvancePaymentStatuses.FinanceVerification,
                AdvancePaymentStatuses.Rejected
            ],
            [AdvancePaymentStatuses.FinanceVerification] =
            [
                AdvancePaymentStatuses.Received,
                AdvancePaymentStatuses.Rejected
            ],
            [AdvancePaymentStatuses.Received] =
            [
                AdvancePaymentStatuses.PartiallyApplied,
                AdvancePaymentStatuses.FullyApplied
            ],
            [AdvancePaymentStatuses.PartiallyApplied] =
            [
                AdvancePaymentStatuses.PartiallyApplied,
                AdvancePaymentStatuses.FullyApplied
            ],
            [AdvancePaymentStatuses.FullyApplied] = [],
            [AdvancePaymentStatuses.Cancelled] = [],
            [AdvancePaymentStatuses.Rejected] = []
        };

        public static string? Normalize(string status) =>
            AdvancePaymentStatuses.All.FirstOrDefault(s =>
                s.Equals(status, StringComparison.OrdinalIgnoreCase)
                || s.Replace(" ", "", StringComparison.Ordinal)
                    .Equals(status.Replace(" ", "", StringComparison.OrdinalIgnoreCase),
                        StringComparison.OrdinalIgnoreCase));

        public static bool CanTransition(string from, string to)
        {
            var fromNorm = Normalize(from);
            var toNorm = Normalize(to);
            if (fromNorm is null || toNorm is null)
            {
                return false;
            }

            if (fromNorm.Equals(toNorm, StringComparison.Ordinal)
                && fromNorm == AdvancePaymentStatuses.PartiallyApplied)
            {
                return true;
            }

            if (fromNorm.Equals(toNorm, StringComparison.Ordinal))
            {
                return true;
            }

            return Allowed.TryGetValue(fromNorm, out var next)
                && next.Contains(toNorm, StringComparer.Ordinal);
        }

        public static bool CanApply(string status)
        {
            var normalized = Normalize(status);
            return normalized is AdvancePaymentStatuses.Received
                or AdvancePaymentStatuses.PartiallyApplied;
        }
    }

    public static class AdvancePaymentModeRules
    {
        public static string? Normalize(string? mode)
        {
            if (string.IsNullOrWhiteSpace(mode))
            {
                return null;
            }

            return AdvancePaymentModes.All.FirstOrDefault(m =>
                m.Equals(mode.Trim(), StringComparison.OrdinalIgnoreCase)
                || m.Replace(" ", "", StringComparison.Ordinal)
                    .Equals(mode.Replace(" ", "", StringComparison.OrdinalIgnoreCase),
                        StringComparison.OrdinalIgnoreCase));
        }
    }
}
