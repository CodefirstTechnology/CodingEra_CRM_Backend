using CRM.DATA;
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

        public static async Task<HashSet<string>> LoadClosedWonStatusNamesAsync(
            TaskDbcontext db,
            CancellationToken cancellationToken = default)
        {
            var allStatuses = await db.DealStatuses.AsNoTracking().ToListAsync(cancellationToken);

            return allStatuses
                .Where(s => s.IsActive
                    && (DealStageValidationHelper.IsClosedWon(s.Name, allStatuses)
                        || LooksLikeClosedWonName(s.Name)))
                .Select(s => s.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static bool LooksLikeClosedWonName(string name)
        {
            var normalized = name.Trim();
            return normalized.Contains("won", StringComparison.OrdinalIgnoreCase)
                && normalized.Contains("closed", StringComparison.OrdinalIgnoreCase)
                && !normalized.Contains("lost", StringComparison.OrdinalIgnoreCase);
        }

        public static async Task<decimal> CalculateAchievedAmountAsync(
            TaskDbcontext db,
            int userId,
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken = default)
        {
            var qualifyingNames = await LoadQualifyingStatusNamesAsync(db, cancellationToken);
            var closedWonNames = await LoadClosedWonStatusNamesAsync(db, cancellationToken);

            if (qualifyingNames.Count == 0 && closedWonNames.Count == 0)
            {
                return 0m;
            }

            var deals = await db.Deals.AsNoTracking()
                .Where(d => d.DealOwnerId == userId || d.AssignedToUserId == userId)
                .Select(d => new DealRow(
                    d.Id,
                    d.DealAmount ?? 0m,
                    d.Status,
                    d.UpdatedAt))
                .ToListAsync(cancellationToken);

            deals = deals.Where(d => d.Amount > 0).ToList();
            if (deals.Count == 0)
            {
                return 0m;
            }

            var dealIds = deals.Select(d => d.Id).ToList();
            var histories = await db.DealStageHistories.AsNoTracking()
                .Where(h => dealIds.Contains(h.DealId))
                .Select(h => new HistoryRow(h.DealId, h.NewStage, h.ChangedAt))
                .ToListAsync(cancellationToken);

            var historyByDeal = histories
                .GroupBy(h => h.DealId)
                .ToDictionary(g => g.Key, g => g.ToList());

            decimal total = 0m;
            foreach (var deal in deals)
            {
                historyByDeal.TryGetValue(deal.Id, out var historyRows);
                var historyStages = historyRows?.Select(h => h.NewStage).ToList()
                    ?? new List<string>();

                if (!QualifiesForAchievement(
                        deal.Status,
                        historyStages,
                        qualifyingNames,
                        closedWonNames))
                {
                    continue;
                }

                var achievementDate = ResolveAchievementDate(
                    deal,
                    qualifyingNames,
                    closedWonNames,
                    historyByDeal);

                if (achievementDate >= startDate && achievementDate <= endDate)
                {
                    total += deal.Amount;
                }
            }

            return total;
        }

        private static bool QualifiesForAchievement(
            string currentStatus,
            IReadOnlyList<string> historyStages,
            HashSet<string> qualifyingNames,
            HashSet<string> closedWonNames)
        {
            foreach (var milestone in qualifyingNames)
            {
                if (DealStageMilestoneRules.HasReachedStage(currentStatus, historyStages, milestone))
                {
                    return true;
                }
            }

            if (closedWonNames.Count == 0)
            {
                return false;
            }

            if (closedWonNames.Contains(currentStatus.Trim()))
            {
                return true;
            }

            return historyStages.Any(h => closedWonNames.Contains(h.Trim()));
        }

        private static DateOnly ResolveAchievementDate(
            DealRow deal,
            HashSet<string> qualifyingNames,
            HashSet<string> closedWonNames,
            IReadOnlyDictionary<int, List<HistoryRow>> historyByDeal)
        {
            if (historyByDeal.TryGetValue(deal.Id, out var rows))
            {
                var milestoneDates = rows
                    .Where(h =>
                        qualifyingNames.Contains(h.NewStage.Trim())
                        || closedWonNames.Contains(h.NewStage.Trim()))
                    .Select(h => ToDateOnly(h.ChangedAt))
                    .OrderBy(d => d)
                    .ToList();

                if (milestoneDates.Count > 0)
                {
                    return milestoneDates[0];
                }
            }

            return ToDateOnly(deal.UpdatedAt);
        }

        private static DateOnly ToDateOnly(DateTime dt)
        {
            var utc = dt.Kind == DateTimeKind.Utc
                ? dt
                : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            return DateOnly.FromDateTime(utc);
        }
    }
}
