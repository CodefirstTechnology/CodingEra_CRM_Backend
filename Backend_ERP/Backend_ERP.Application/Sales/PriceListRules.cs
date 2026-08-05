using ERP.Domain.Sales;

namespace ERP.Application.Sales
{
    public static class PriceListCalculator
    {
        public static decimal Round2(decimal value) =>
            Math.Round(value, 2, MidpointRounding.AwayFromZero);

        public static decimal CalcSellingFromBase(decimal basePrice, decimal discountPercentage) =>
            Round2(Math.Max(0, basePrice) * (1 - Math.Clamp(discountPercentage, 0, 100) / 100m));
    }

    public static class PriceListStatusRules
    {
        private static readonly Dictionary<string, string[]> Allowed = new(StringComparer.OrdinalIgnoreCase)
        {
            [PriceListStatuses.Draft] =
            [
                PriceListStatuses.Active,
                PriceListStatuses.Archived
            ],
            [PriceListStatuses.Active] =
            [
                PriceListStatuses.Expired,
                PriceListStatuses.Archived
            ],
            [PriceListStatuses.Expired] =
            [
                PriceListStatuses.Archived
            ],
            [PriceListStatuses.Archived] = []
        };

        public static string? Normalize(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return null;
            }

            return PriceListStatuses.All.FirstOrDefault(s =>
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

        public static bool IsEditable(string status) =>
            Normalize(status) is PriceListStatuses.Draft or PriceListStatuses.Active;

        public static bool IsReadOnly(string status) =>
            Normalize(status) is PriceListStatuses.Expired or PriceListStatuses.Archived;
    }

    public static class PriceListCustomerCategoryRules
    {
        public static string? Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return PriceListCustomerCategories.All.FirstOrDefault(s =>
                s.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }
}
