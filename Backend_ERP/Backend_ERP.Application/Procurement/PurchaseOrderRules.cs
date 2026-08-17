using System;
using System.Collections.Generic;

namespace ERP.Domain.Procurement
{
    public static class PurchaseOrderRules
    {
        public static bool CanTransition(PurchaseOrderStatus current, PurchaseOrderStatus target)
        {
            if (current == target) return true;

            return current switch
            {
                PurchaseOrderStatus.Draft => target is PurchaseOrderStatus.Submitted or PurchaseOrderStatus.Cancelled,
                PurchaseOrderStatus.Submitted => target is PurchaseOrderStatus.Approved or PurchaseOrderStatus.Rejected or PurchaseOrderStatus.RevisionRequired or PurchaseOrderStatus.Cancelled,
                PurchaseOrderStatus.RevisionRequired => target is PurchaseOrderStatus.Submitted or PurchaseOrderStatus.Draft or PurchaseOrderStatus.Cancelled,
                PurchaseOrderStatus.Approved => target is PurchaseOrderStatus.Ordered or PurchaseOrderStatus.Cancelled,
                PurchaseOrderStatus.Ordered => target is PurchaseOrderStatus.PartiallyReceived or PurchaseOrderStatus.Completed or PurchaseOrderStatus.Cancelled,
                PurchaseOrderStatus.PartiallyReceived => target is PurchaseOrderStatus.Completed or PurchaseOrderStatus.Cancelled,
                PurchaseOrderStatus.Completed => false,
                PurchaseOrderStatus.Cancelled => false,
                PurchaseOrderStatus.Rejected => target is PurchaseOrderStatus.Draft or PurchaseOrderStatus.Submitted,
                _ => false
            };
        }

        public static decimal Round2(decimal n)
        {
            return Math.Round(n, 2, MidpointRounding.AwayFromZero);
        }

        public static void CalculateTotals(
            IEnumerable<PurchaseOrderLine> lines,
            out decimal subtotal,
            out decimal discountTotal,
            out decimal taxTotal,
            out decimal grandTotal)
        {
            subtotal = 0m;
            discountTotal = 0m;
            taxTotal = 0m;

            if (lines != null)
            {
                foreach (var line in lines)
                {
                    var baseAmount = Math.Max(0, line.Quantity) * Math.Max(0, line.Rate);
                    var discAmt = Round2(baseAmount * (Math.Clamp(line.Discount, 0, 100) / 100m));
                    var afterDisc = baseAmount - discAmt;
                    var txAmt = Round2(afterDisc * (Math.Clamp(line.Tax, 0, 100) / 100m));
                    var lineAmount = Round2(afterDisc + txAmt);

                    line.Amount = lineAmount;

                    subtotal += Round2(baseAmount);
                    discountTotal += discAmt;
                    taxTotal += txAmt;
                }
            }

            subtotal = Round2(subtotal);
            discountTotal = Round2(discountTotal);
            taxTotal = Round2(taxTotal);
            grandTotal = Round2(subtotal - discountTotal + taxTotal);
        }
    }
}
