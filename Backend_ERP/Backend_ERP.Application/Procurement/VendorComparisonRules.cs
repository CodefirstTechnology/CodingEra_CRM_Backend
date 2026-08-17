using System;
using System.Collections.Generic;
using System.Linq;
using ERP.Domain.Procurement;

namespace ERP.Application.Procurement
{
    public static class VendorComparisonRules
    {
        public static bool CanTransition(VendorComparisonStatus current, VendorComparisonStatus target)
        {
            if (current == target) return true;

            return current switch
            {
                VendorComparisonStatus.Draft => target is VendorComparisonStatus.Pending or VendorComparisonStatus.Compared,
                VendorComparisonStatus.Pending => target is VendorComparisonStatus.Compared or VendorComparisonStatus.Completed or VendorComparisonStatus.Approved,
                VendorComparisonStatus.Compared => target is VendorComparisonStatus.Approved or VendorComparisonStatus.Completed or VendorComparisonStatus.Awarded,
                VendorComparisonStatus.Completed => target is VendorComparisonStatus.Approved or VendorComparisonStatus.Awarded,
                VendorComparisonStatus.Approved => target is VendorComparisonStatus.Awarded,
                VendorComparisonStatus.Awarded => false,
                _ => false
            };
        }

        public static void EvaluateMetrics(List<VendorComparisonEntry> entries)
        {
            if (entries == null || entries.Count == 0) return;

            var minPrice = entries.Min(e => e.TotalCost > 0 ? e.TotalCost : e.UnitPrice);
            var minDelivery = entries.Min(e => e.DeliveryTimeDays > 0 ? e.DeliveryTimeDays : 999);

            foreach (var e in entries)
            {
                var cost = e.TotalCost > 0 ? e.TotalCost : e.UnitPrice;
                e.IsLowestPrice = Math.Abs(cost - minPrice) < 0.0001m;
                e.IsBestDelivery = e.DeliveryTimeDays > 0 && e.DeliveryTimeDays == minDelivery;

                double score = 70.0;
                if (e.IsLowestPrice) score += 20.0;
                if (e.IsBestDelivery) score += 10.0;

                e.RecommendationScore = score;
            }

            var ordered = entries.OrderByDescending(e => e.RecommendationScore).ThenBy(e => e.TotalCost).ToList();
            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].Ranking = i + 1;
            }
        }
    }
}
