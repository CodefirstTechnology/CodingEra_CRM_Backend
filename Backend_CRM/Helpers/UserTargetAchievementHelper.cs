using CRM.DATA;
using CRM.Helpers;
using Microsoft.EntityFrameworkCore;

namespace CRM.Helpers
{
    /// <summary>Calculates sales target achievement from qualifying deal pipeline statuses.</summary>
    public static class UserTargetAchievementHelper
    {
        private sealed record DealRow(int Id, decimal Amount, string Status, DateTime UpdatedAt);

        private sealed record HistoryRow(int DealId, string NewStage, DateTime ChangedAt);

        public static async Task<HashSet<string>> LoadQualifyingStatusNamesAsync(
            TaskDbcontext db,
            CancellationToken cancellationToken = default)
        {
            var milestoneNames = new[]
            {
                DealStageMilestoneRules.MaterialDelivered,
                DealStageMilestoneRules.FullPaymentReceived,
            };

            var fromDb = await db.DealStatuses.AsNoTracking()
                .Where(s => s.IsActive)
                .Select(s => s.Name)
                .ToListAsync(cancellationToken);

            return fromDb
                .Where(n => milestoneNames.Any(m =>
                    string.Equals(m, n, StringComparison.OrdinalIgnoreCase)))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public static async Task<decimal> CalculateAchievedAmountAsync(
            TaskDbcontext db,
            int userId,
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken = default)
        {
            var qualifyingNames = await LoadQualifyingStatusNamesAsync(db, cancellationToken);
            if (qualifyingNames.Count == 0)
            {
                return 0m;
            }

            var deals = await db.Deals.AsNoTracking()
                .Where(d => d.DealOwnerId == userId)
                .Select(d => new DealRow(
                    d.Id,
                    d.DealAmount ?? 0m,
                    d.Status,
                    d.UpdatedAt))
                .ToListAsync(cancellationToken);

            if (deals.Count == 0)
            {
                return 0m;
            }

            var missingAmountIds = deals
                .Where(d => d.Amount <= 0)
                .Select(d => d.Id)
                .ToList();

            if (missingAmountIds.Count > 0)
            {
                var quotationRows = await db.Quotations.AsNoTracking()
                    .Where(q => q.DealId != null && missingAmountIds.Contains(q.DealId.Value))
                    .OrderByDescending(q => q.Id)
                    .Select(q => new { DealId = q.DealId!.Value, q.GrandTotal })
                    .ToListAsync(cancellationToken);

                var latestByDeal = new Dictionary<int, decimal>();
                foreach (var row in quotationRows)
                {
                    if (!latestByDeal.ContainsKey(row.DealId))
                    {
                        latestByDeal[row.DealId] = row.GrandTotal;
                    }
                }

                deals = deals
                    .Select(d =>
                    {
                        if (d.Amount > 0)
                        {
                            return d;
                        }

                        return latestByDeal.TryGetValue(d.Id, out var amount) && amount > 0
                            ? d with { Amount = amount }
                            : d;
                    })
                    .Where(d => d.Amount > 0)
                    .ToList();
            }
            else
            {
                deals = deals.Where(d => d.Amount > 0).ToList();
            }

            if (deals.Count == 0)
            {
                return 0m;
            }

            var qualifyingDealIds = deals
                .Where(d => qualifyingNames.Contains(d.Status.Trim()))
                .Select(d => d.Id)
                .ToList();

            if (qualifyingDealIds.Count == 0)
            {
                return 0m;
            }

            var histories = await db.DealStageHistories.AsNoTracking()
                .Where(h => qualifyingDealIds.Contains(h.DealId))
                .Select(h => new HistoryRow(h.DealId, h.NewStage, h.ChangedAt))
                .ToListAsync(cancellationToken);

            var historyByDeal = histories
                .GroupBy(h => h.DealId)
                .ToDictionary(g => g.Key, g => g.ToList());

            decimal total = 0m;
            foreach (var deal in deals)
            {
                if (!qualifyingNames.Contains(deal.Status.Trim()))
                {
                    continue;
                }

                var achievementDate = ResolveAchievementDate(
                    deal,
                    qualifyingNames,
                    historyByDeal);

                if (achievementDate >= startDate && achievementDate <= endDate)
                {
                    total += deal.Amount;
                }
            }

            return total;
        }

        private static DateOnly ResolveAchievementDate(
            DealRow deal,
            HashSet<string> qualifyingNames,
            IReadOnlyDictionary<int, List<HistoryRow>> historyByDeal)
        {
            if (historyByDeal.TryGetValue(deal.Id, out var rows))
            {
                var milestoneDates = rows
                    .Where(h => qualifyingNames.Contains(h.NewStage.Trim()))
                    .Select(h => DateOnly.FromDateTime(
                        h.ChangedAt.Kind == DateTimeKind.Utc
                            ? h.ChangedAt
                            : DateTime.SpecifyKind(h.ChangedAt, DateTimeKind.Utc)))
                    .OrderBy(d => d)
                    .ToList();

                if (milestoneDates.Count > 0)
                {
                    return milestoneDates[0];
                }
            }

            var updated = deal.UpdatedAt.Kind == DateTimeKind.Utc
                ? deal.UpdatedAt
                : DateTime.SpecifyKind(deal.UpdatedAt, DateTimeKind.Utc);
            return DateOnly.FromDateTime(updated);
        }
    }
}
