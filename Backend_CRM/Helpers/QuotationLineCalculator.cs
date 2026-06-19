namespace CRM.Helpers
{
    public static class QuotationLineCalculator
    {
        public sealed record LineAmounts(
            decimal Amount,
            decimal TaxAmount,
            decimal LineTotal,
            decimal LineWeight);

        public sealed record QuotationTotals(
            decimal Subtotal,
            decimal AdditionalChargesTotal,
            decimal TaxableAmount,
            decimal TaxTotal,
            decimal GrandTotal,
            decimal TotalQuantity,
            decimal TotalWeight);

        /// <summary>Line amount without GST (GST is applied on quotation subtotal).</summary>
        public static LineAmounts CalculateLine(
            decimal quantity,
            decimal unitRate,
            decimal discountPercent,
            decimal weight,
            decimal unitWeight)
        {
            var qty = quantity < 0 ? 0 : quantity;
            var rate = unitRate < 0 ? 0 : unitRate;
            var disc = discountPercent < 0 ? 0 : (discountPercent > 100 ? 100 : discountPercent);

            var amount = Round(qty * rate);
            var afterDiscount = Round(amount * (1 - disc / 100m));
            var lineWeight = weight > 0
                ? weight
                : Round(qty * (unitWeight < 0 ? 0 : unitWeight));

            return new LineAmounts(afterDiscount, 0, afterDiscount, lineWeight);
        }

        /// <summary>Legacy line calc when per-line GST was stored on old quotations.</summary>
        public static LineAmounts CalculateLineLegacy(
            decimal quantity,
            decimal unitRate,
            decimal discountPercent,
            decimal gstPercent,
            decimal weight,
            decimal unitWeight)
        {
            var qty = quantity < 0 ? 0 : quantity;
            var rate = unitRate < 0 ? 0 : unitRate;
            var disc = discountPercent < 0 ? 0 : (discountPercent > 100 ? 100 : discountPercent);
            var gst = gstPercent < 0 ? 0 : gstPercent;

            var amount = Round(qty * rate);
            var afterDiscount = Round(amount * (1 - disc / 100m));
            var taxAmount = Round(afterDiscount * (gst / 100m));
            var lineTotal = Round(afterDiscount + taxAmount);
            var lineWeight = weight > 0
                ? weight
                : Round(qty * (unitWeight < 0 ? 0 : unitWeight));

            return new LineAmounts(amount, taxAmount, lineTotal, lineWeight);
        }

        public static QuotationTotals AggregateLines(
            IEnumerable<(decimal Quantity, LineAmounts Amounts)> rows,
            decimal headerGstPercent,
            decimal additionalChargesTotal = 0)
        {
            var list = rows.ToList();
            var lineSubtotal = Round(list.Sum(r => r.Amounts.LineTotal));
            var totalQty = Round(list.Sum(r => r.Quantity < 0 ? 0 : r.Quantity));
            var totalWeight = Round(list.Sum(r => r.Amounts.LineWeight));
            var additional = additionalChargesTotal < 0 ? 0 : additionalChargesTotal;
            var taxableAmount = Round(lineSubtotal + additional);

            if (headerGstPercent > 0)
            {
                var taxTotal = Round(taxableAmount * (headerGstPercent / 100m));
                var grandTotal = Round(taxableAmount + taxTotal);
                return new QuotationTotals(
                    lineSubtotal,
                    additional,
                    taxableAmount,
                    taxTotal,
                    grandTotal,
                    totalQty,
                    totalWeight);
            }

            var legacyTax = Round(list.Sum(r => r.Amounts.TaxAmount));
            if (legacyTax > 0)
            {
                var legacyGrand = Round(list.Sum(r => r.Amounts.LineTotal) + additional);
                var legacySubtotal = Round(legacyGrand - legacyTax);
                return new QuotationTotals(
                    legacySubtotal,
                    additional,
                    Round(legacySubtotal + additional),
                    legacyTax,
                    legacyGrand,
                    totalQty,
                    totalWeight);
            }

            return new QuotationTotals(
                lineSubtotal,
                additional,
                taxableAmount,
                0,
                taxableAmount,
                totalQty,
                totalWeight);
        }

        public static decimal SumAdditionalCharges(
            decimal transportationCharges,
            decimal loadingCharges,
            decimal serviceCharges,
            IEnumerable<decimal>? customChargeAmounts)
        {
            var transport = transportationCharges < 0 ? 0 : transportationCharges;
            var loading = loadingCharges < 0 ? 0 : loadingCharges;
            var service = serviceCharges < 0 ? 0 : serviceCharges;
            var custom = customChargeAmounts?.Where(a => a > 0).Sum() ?? 0;
            return Round(transport + loading + service + custom);
        }

        public static decimal ResolveUnitRate(decimal unitWeight, decimal steelRate, decimal fallbackRate)
        {
            if (unitWeight > 0 && steelRate > 0)
            {
                return Round(unitWeight * steelRate);
            }

            return fallbackRate < 0 ? 0 : fallbackRate;
        }

        private static decimal Round(decimal value) =>
            Math.Round(value, 4, MidpointRounding.AwayFromZero);
    }
}
