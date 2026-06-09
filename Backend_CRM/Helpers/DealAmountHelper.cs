using CRM.models;
using CRM.DATA;
using Microsoft.EntityFrameworkCore;

namespace CRM.Helpers
{
    /// <summary>Fills missing deal amounts from the latest linked quotation grand total.</summary>
    public static class DealAmountHelper
    {
        public static async Task ApplyLatestQuotationAmountsAsync(
            TaskDbcontext context,
            IList<Deal> deals,
            CancellationToken cancellationToken = default)
        {
            if (deals.Count == 0)
            {
                return;
            }

            var dealIds = deals
                .Where(d => d.DealAmount is null or <= 0)
                .Select(d => d.Id)
                .Distinct()
                .ToList();

            if (dealIds.Count == 0)
            {
                return;
            }

            var latestByDeal = await LoadLatestQuotationTotalsAsync(context, dealIds, cancellationToken);
            foreach (var deal in deals)
            {
                if (deal.DealAmount is > 0)
                {
                    continue;
                }

                if (latestByDeal.TryGetValue(deal.Id, out var amount) && amount > 0)
                {
                    deal.DealAmount = amount;
                }
            }
        }

        public static async Task<decimal?> ResolveEffectiveAmountAsync(
            TaskDbcontext context,
            int dealId,
            decimal? storedAmount,
            CancellationToken cancellationToken = default)
        {
            if (storedAmount is > 0)
            {
                return storedAmount;
            }

            var latest = await LoadLatestQuotationTotalsAsync(
                context,
                new[] { dealId },
                cancellationToken);

            return latest.TryGetValue(dealId, out var amount) && amount > 0 ? amount : storedAmount;
        }

        private static async Task<Dictionary<int, decimal>> LoadLatestQuotationTotalsAsync(
            TaskDbcontext context,
            IEnumerable<int> dealIds,
            CancellationToken cancellationToken)
        {
            var ids = dealIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return new Dictionary<int, decimal>();
            }

            var rows = await context.Quotations.AsNoTracking()
                .Where(q => q.DealId != null && ids.Contains(q.DealId.Value))
                .OrderByDescending(q => q.Id)
                .Select(q => new { DealId = q.DealId!.Value, q.GrandTotal })
                .ToListAsync(cancellationToken);

            var latest = new Dictionary<int, decimal>();
            foreach (var row in rows)
            {
                if (!latest.ContainsKey(row.DealId))
                {
                    latest[row.DealId] = row.GrandTotal;
                }
            }

            return latest;
        }
    }
}
