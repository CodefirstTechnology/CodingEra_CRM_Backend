using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;

namespace ERP.Application.Sales
{
    public static class SalesOrderCalculator
    {
        public static decimal Round2(decimal value) =>
            Math.Round(value, 2, MidpointRounding.AwayFromZero);

        public static decimal CalcLineDiscountAmount(decimal quantity, decimal rate, decimal discountPercent)
        {
            var baseAmount = Math.Max(0, quantity) * Math.Max(0, rate);
            var pct = Math.Clamp(discountPercent, 0, 100);
            return Round2(baseAmount * (pct / 100m));
        }

        public static decimal CalcLineGstAmount(
            decimal quantity,
            decimal rate,
            decimal discountPercent,
            decimal gstPercent)
        {
            var baseAmount = Math.Max(0, quantity) * Math.Max(0, rate);
            var afterDiscount = baseAmount - CalcLineDiscountAmount(quantity, rate, discountPercent);
            var gst = Math.Clamp(gstPercent, 0, 100);
            return Round2(afterDiscount * (gst / 100m));
        }

        public static decimal CalcLineAmount(
            decimal quantity,
            decimal rate,
            decimal discountPercent,
            decimal gstPercent)
        {
            var baseAmount = Math.Max(0, quantity) * Math.Max(0, rate);
            var afterDiscount = baseAmount * (1 - Math.Clamp(discountPercent, 0, 100) / 100m);
            return Round2(afterDiscount * (1 + Math.Clamp(gstPercent, 0, 100) / 100m));
        }

        public static (decimal Subtotal, decimal DiscountTotal, decimal GstTotal, decimal GrandTotal)
            Summarize(IEnumerable<SalesOrderItemDto> items)
        {
            decimal subtotal = 0;
            decimal discountTotal = 0;
            decimal gstTotal = 0;

            foreach (var item in items)
            {
                var qty = Math.Max(0, item.Quantity);
                var rate = Math.Max(0, item.Rate);
                var baseAmount = qty * rate;
                subtotal += baseAmount;
                discountTotal += CalcLineDiscountAmount(qty, rate, item.Discount);
                gstTotal += CalcLineGstAmount(qty, rate, item.Discount, item.Gst);
            }

            subtotal = Round2(subtotal);
            discountTotal = Round2(discountTotal);
            gstTotal = Round2(gstTotal);
            var grandTotal = Round2(subtotal - discountTotal + gstTotal);
            return (subtotal, discountTotal, gstTotal, grandTotal);
        }
    }

    public static class SalesOrderStatusRules
    {
        private static readonly Dictionary<string, string[]> AllowedTransitions = new(StringComparer.OrdinalIgnoreCase)
        {
            [SalesOrderStatuses.Draft] = [SalesOrderStatuses.Submitted, SalesOrderStatuses.Cancelled],
            [SalesOrderStatuses.Submitted] = [SalesOrderStatuses.Confirmed, SalesOrderStatuses.Cancelled],
            [SalesOrderStatuses.Confirmed] = [SalesOrderStatuses.Processing, SalesOrderStatuses.Cancelled],
            [SalesOrderStatuses.Processing] =
            [
                SalesOrderStatuses.PartiallyDelivered,
                SalesOrderStatuses.Completed,
                SalesOrderStatuses.Cancelled
            ],
            [SalesOrderStatuses.PartiallyDelivered] =
            [
                SalesOrderStatuses.Completed,
                SalesOrderStatuses.Cancelled
            ],
            [SalesOrderStatuses.Completed] = [],
            [SalesOrderStatuses.Cancelled] = []
        };

        public static bool IsKnownStatus(string status) =>
            SalesOrderStatuses.All.Any(s => s.Equals(status, StringComparison.OrdinalIgnoreCase));

        public static string? Normalize(string status)
        {
            return SalesOrderStatuses.All.FirstOrDefault(s =>
                s.Equals(status, StringComparison.OrdinalIgnoreCase));
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

            return AllowedTransitions.TryGetValue(fromNorm, out var next)
                && next.Contains(toNorm, StringComparer.Ordinal);
        }

        public static string TimelineLabel(string status) => status switch
        {
            SalesOrderStatuses.Draft => "Draft Saved",
            SalesOrderStatuses.Submitted => "Submitted",
            SalesOrderStatuses.Confirmed => "Confirmed",
            SalesOrderStatuses.Processing => "Processing",
            SalesOrderStatuses.PartiallyDelivered => "Partially Delivered",
            SalesOrderStatuses.Completed => "Completed",
            SalesOrderStatuses.Cancelled => "Cancelled",
            _ => status
        };
    }
}
