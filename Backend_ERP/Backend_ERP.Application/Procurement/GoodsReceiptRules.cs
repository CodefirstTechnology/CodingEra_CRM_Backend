using System;
using System.Collections.Generic;
using System.Linq;
using ERP.Domain.Procurement;

namespace ERP.Application.Procurement
{
    public static class GoodsReceiptRules
    {
        public static bool CanTransition(GoodsReceiptStatus current, GoodsReceiptStatus target)
        {
            if (current == target) return true;

            return current switch
            {
                GoodsReceiptStatus.Draft => target is GoodsReceiptStatus.Submitted or GoodsReceiptStatus.Quarantined or GoodsReceiptStatus.Completed or GoodsReceiptStatus.Cancelled,
                GoodsReceiptStatus.Submitted => target is GoodsReceiptStatus.Quarantined or GoodsReceiptStatus.QcCompleted or GoodsReceiptStatus.Completed or GoodsReceiptStatus.Posted or GoodsReceiptStatus.Cancelled,
                GoodsReceiptStatus.Quarantined => target is GoodsReceiptStatus.QcCompleted or GoodsReceiptStatus.Completed or GoodsReceiptStatus.Cancelled,
                GoodsReceiptStatus.QcCompleted => target is GoodsReceiptStatus.Completed or GoodsReceiptStatus.Posted or GoodsReceiptStatus.Cancelled,
                GoodsReceiptStatus.Completed => target is GoodsReceiptStatus.Posted,
                GoodsReceiptStatus.Posted => false,
                GoodsReceiptStatus.Cancelled => false,
                _ => false
            };
        }

        public static decimal Round2(decimal n)
        {
            return Math.Round(n, 2, MidpointRounding.AwayFromZero);
        }

        public static string? ValidateItems(IEnumerable<GoodsReceiptItem> items)
        {
            var list = items?.ToList() ?? new List<GoodsReceiptItem>();
            if (list.Count == 0) return "At least one line item is required.";

            bool anyReceived = false;
            foreach (var item in list)
            {
                var recv = Math.Max(0, item.ReceivedQuantity);
                var rej = Math.Max(0, item.RejectedQuantity);

                decimal tolerancePct = item.PurchaseOrderLine?.OverDeliveryTolerancePercent ?? 2.0m;
                decimal allowedLimit = Math.Round(item.RemainingQuantity * (1m + (tolerancePct / 100m)), 4);

                if (recv > allowedLimit + 0.0001m)
                {
                    return $"Received quantity for \"{item.ItemName}\" exceeds remaining ({item.RemainingQuantity}) plus allowed tolerance ({tolerancePct}%).";
                }
                if (rej > allowedLimit + 0.0001m)
                {
                    return $"Rejected quantity for \"{item.ItemName}\" exceeds remaining ({item.RemainingQuantity}).";
                }
                if (recv > 0) anyReceived = true;
            }

            if (!anyReceived) return "Enter at least one received quantity.";
            return null;
        }

        public static (decimal ordered, decimal received, decimal remaining, decimal rejected, decimal receiptPercentage) CalculateSummary(IEnumerable<GoodsReceiptItem> items)
        {
            decimal ordered = 0m;
            decimal received = 0m;
            decimal rejected = 0m;

            if (items != null)
            {
                foreach (var item in items)
                {
                    ordered += Math.Max(0, item.OrderedQuantity);
                    received += Math.Max(0, item.ReceivedQuantity);
                    rejected += Math.Max(0, item.RejectedQuantity);
                }
            }

            ordered = Round2(ordered);
            received = Round2(received);
            rejected = Round2(rejected);
            decimal remaining = Math.Max(0, Round2(ordered - received));
            decimal pct = ordered > 0 ? Round2((received / ordered) * 100m) : 0m;

            return (ordered, received, remaining, rejected, pct);
        }
    }
}
