using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;

namespace ERP.Application.Sales
{
    public static class ProformaInvoiceCalculator
    {
        public static decimal Round2(decimal value) =>
            Math.Round(value, 2, MidpointRounding.AwayFromZero);

        public static decimal CalcLineDiscountAmount(decimal quantity, decimal rate, decimal discountPercent)
        {
            var baseAmount = Math.Max(0, quantity) * Math.Max(0, rate);
            return Round2(baseAmount * (Math.Clamp(discountPercent, 0, 100) / 100m));
        }

        public static decimal CalcLineTaxAmount(
            decimal quantity,
            decimal rate,
            decimal discountPercent,
            decimal gstPercent)
        {
            var baseAmount = Math.Max(0, quantity) * Math.Max(0, rate);
            var afterDiscount = baseAmount - CalcLineDiscountAmount(quantity, rate, discountPercent);
            return Round2(afterDiscount * (Math.Clamp(gstPercent, 0, 100) / 100m));
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

        public static (decimal Subtotal, decimal DiscountTotal, decimal TaxTotal, decimal GrandTotal)
            Summarize(IEnumerable<ProformaInvoiceItemDto> items)
        {
            decimal subtotal = 0;
            decimal discountTotal = 0;
            decimal taxTotal = 0;

            foreach (var item in items)
            {
                var qty = Math.Max(0, item.Quantity);
                var rate = Math.Max(0, item.Rate);
                subtotal += qty * rate;
                discountTotal += CalcLineDiscountAmount(qty, rate, item.Discount);
                taxTotal += CalcLineTaxAmount(qty, rate, item.Discount, item.Gst);
            }

            subtotal = Round2(subtotal);
            discountTotal = Round2(discountTotal);
            taxTotal = Round2(taxTotal);
            return (subtotal, discountTotal, taxTotal, Round2(subtotal - discountTotal + taxTotal));
        }
    }

    public static class ProformaInvoiceStatusRules
    {
        private static readonly Dictionary<string, string[]> Allowed = new(StringComparer.OrdinalIgnoreCase)
        {
            [ProformaInvoiceStatuses.Draft] =
            [
                ProformaInvoiceStatuses.Submitted,
                ProformaInvoiceStatuses.Cancelled
            ],
            [ProformaInvoiceStatuses.Submitted] =
            [
                ProformaInvoiceStatuses.PendingFinanceApproval,
                ProformaInvoiceStatuses.Returned,
                ProformaInvoiceStatuses.Cancelled
            ],
            [ProformaInvoiceStatuses.PendingFinanceApproval] =
            [
                ProformaInvoiceStatuses.Approved,
                ProformaInvoiceStatuses.Rejected,
                ProformaInvoiceStatuses.Returned,
                ProformaInvoiceStatuses.Cancelled
            ],
            [ProformaInvoiceStatuses.Approved] =
            [
                ProformaInvoiceStatuses.Sent,
                ProformaInvoiceStatuses.Cancelled
            ],
            [ProformaInvoiceStatuses.Rejected] =
            [
                ProformaInvoiceStatuses.Draft,
                ProformaInvoiceStatuses.Cancelled
            ],
            [ProformaInvoiceStatuses.Returned] =
            [
                ProformaInvoiceStatuses.Draft,
                ProformaInvoiceStatuses.Submitted,
                ProformaInvoiceStatuses.Cancelled
            ],
            [ProformaInvoiceStatuses.Sent] =
            [
                ProformaInvoiceStatuses.Accepted,
                ProformaInvoiceStatuses.Expired,
                ProformaInvoiceStatuses.Cancelled
            ],
            [ProformaInvoiceStatuses.Accepted] =
            [
                ProformaInvoiceStatuses.Converted,
                ProformaInvoiceStatuses.Cancelled
            ],
            [ProformaInvoiceStatuses.Expired] = [ProformaInvoiceStatuses.Cancelled],
            [ProformaInvoiceStatuses.Cancelled] = [],
            [ProformaInvoiceStatuses.Converted] = []
        };

        public static string? Normalize(string status) =>
            ProformaInvoiceStatuses.All.FirstOrDefault(s =>
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

            if (fromNorm.Equals(toNorm, StringComparison.Ordinal))
            {
                return true;
            }

            return Allowed.TryGetValue(fromNorm, out var next)
                && next.Contains(toNorm, StringComparer.Ordinal);
        }
    }
}
