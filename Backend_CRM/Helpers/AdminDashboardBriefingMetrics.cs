using CRM.DTO;
using CRM.Helpers;
using CRM.models;

namespace CRM.Helpers
{
    internal static class AdminDashboardBriefingMetrics
    {
        public const decimal UsdToInrRate = 83m;
        public const decimal MonthlyTargetUsd = 1_000_000m;
        public const int StuckDealInactiveHours = 24;

        public static decimal ToInr(decimal? usd) => (usd ?? 0m) * UsdToInrRate;

        public static string FormatInrCompact(decimal inr)
        {
            var abs = Math.Abs(inr);
            if (abs >= 10_000_000m)
            {
                return $"₹{Math.Round(inr / 10_000_000m, abs >= 100_000_000m ? 0 : 1)} Cr";
            }

            if (abs >= 100_000m)
            {
                return $"₹{Math.Round(inr / 100_000m, 1)} L";
            }

            return $"₹{Math.Round(inr):N0}";
        }

        public static int CountByStatus(IEnumerable<Lead> leads, string status) =>
            leads.Count(l => string.Equals(l.LeadStatus?.Name, status, StringComparison.OrdinalIgnoreCase));

        public static decimal ConversionRatePct(IReadOnlyList<Lead> leads)
        {
            var converted = CountByStatus(leads, "Converted");
            var junk = CountByStatus(leads, "Junk");
            var denom = Math.Max(1, leads.Count - junk);
            return leads.Count == 0 ? 0m : Math.Round((decimal)converted / denom * 100m, 1);
        }

        public static bool IsLeadInCurrentMonth(Lead lead, DateTime monthStart, DateTime now)
        {
            var date = lead.CreatedAt?.ToUniversalTime() ?? lead.UpdatedAt.ToUniversalTime();
            return date >= monthStart
                && date.Year == now.Year
                && date.Month == now.Month;
        }

        public static int CountStuckDeals(IEnumerable<Deal> activeDeals, DateTime nowUtc)
        {
            var threshold = nowUtc.AddHours(-StuckDealInactiveHours);
            return activeDeals.Count(d => d.LastModified.ToUniversalTime() <= threshold);
        }

        public static IReadOnlyList<DashboardPipelineSegmentDto> BuildPipelineSegments(
            IEnumerable<Deal> activeDeals)
        {
            var byStatus = new Dictionary<string, (int Count, decimal RevenueUsd)>(StringComparer.OrdinalIgnoreCase);

            foreach (var deal in activeDeals)
            {
                var key = string.IsNullOrWhiteSpace(deal.Status) ? "Unknown" : deal.Status.Trim();
                var revenue = deal.AnnualRevenue ?? deal.DealAmount ?? 0m;
                if (byStatus.TryGetValue(key, out var cur))
                {
                    byStatus[key] = (cur.Count + 1, cur.RevenueUsd + revenue);
                }
                else
                {
                    byStatus[key] = (1, revenue);
                }
            }

            return byStatus
                .OrderByDescending(x => x.Value.RevenueUsd)
                .ThenByDescending(x => x.Value.Count)
                .Take(4)
                .Select(x => new DashboardPipelineSegmentDto
                {
                    Label = x.Key,
                    Count = x.Value.Count,
                    RevenueInr = ToInr(x.Value.RevenueUsd),
                })
                .ToList();
        }

        public static bool IsActiveDeal(string status, IReadOnlyList<DealStatus> dealStatuses) =>
            !DealStageValidationHelper.IsClosed(status, dealStatuses);
    }
}
