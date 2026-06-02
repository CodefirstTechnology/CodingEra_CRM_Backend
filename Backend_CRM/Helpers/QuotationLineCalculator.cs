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
            decimal TaxTotal,
            decimal GrandTotal,
            decimal TotalQuantity,
            decimal TotalWeight);

        public static LineAmounts CalculateLine(
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
            IEnumerable<(decimal Quantity, LineAmounts Amounts)> rows)
        {
            var list = rows.ToList();
            var subtotal = Round(list.Sum(r => r.Amounts.LineTotal - r.Amounts.TaxAmount));
            var taxTotal = Round(list.Sum(r => r.Amounts.TaxAmount));
            var grandTotal = Round(list.Sum(r => r.Amounts.LineTotal));
            var totalQty = Round(list.Sum(r => r.Quantity < 0 ? 0 : r.Quantity));
            var totalWeight = Round(list.Sum(r => r.Amounts.LineWeight));
            return new QuotationTotals(subtotal, taxTotal, grandTotal, totalQty, totalWeight);
        }

        private static decimal Round(decimal value) =>
            Math.Round(value, 4, MidpointRounding.AwayFromZero);
    }
}
