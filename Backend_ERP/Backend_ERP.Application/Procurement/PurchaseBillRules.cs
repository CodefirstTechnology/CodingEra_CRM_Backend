using System;
using ERP.Domain.Procurement;

namespace ERP.Application.Procurement
{
    public static class PurchaseBillRules
    {
        public static bool CanTransition(PurchaseBillStatus current, PurchaseBillStatus target)
        {
            if (current == target) return true;

            return current switch
            {
                PurchaseBillStatus.Draft => target is PurchaseBillStatus.Approved or PurchaseBillStatus.Void,
                PurchaseBillStatus.Approved => target is PurchaseBillStatus.Posted or PurchaseBillStatus.Void,
                PurchaseBillStatus.Posted => target is PurchaseBillStatus.Paid or PurchaseBillStatus.Void,
                PurchaseBillStatus.Paid => false,
                PurchaseBillStatus.Void => false,
                _ => false
            };
        }

        public static bool CanEdit(PurchaseBillStatus status)
        {
            return status == PurchaseBillStatus.Draft;
        }

        public static (decimal SubTotal, decimal DiscountTotal, decimal TaxTotal, decimal RoundOff, decimal GrandTotal) CalculateTotals(
            System.Collections.Generic.IEnumerable<(decimal Quantity, decimal UnitPrice, decimal DiscountAmount, decimal TaxPercent)> lines)
        {
            decimal subTotal = 0m;
            decimal discountTotal = 0m;
            decimal taxTotal = 0m;

            foreach (var line in lines)
            {
                var lineSub = line.Quantity * line.UnitPrice;
                var disc = line.DiscountAmount;
                var taxable = Math.Max(0m, lineSub - disc);
                var tax = (taxable * line.TaxPercent) / 100m;

                subTotal += lineSub;
                discountTotal += disc;
                taxTotal += tax;
            }

            var rawGrand = subTotal - discountTotal + taxTotal;
            var grandTotal = Math.Round(rawGrand);
            var roundOff = grandTotal - rawGrand;

            return (subTotal, discountTotal, taxTotal, roundOff, grandTotal);
        }
    }
}
