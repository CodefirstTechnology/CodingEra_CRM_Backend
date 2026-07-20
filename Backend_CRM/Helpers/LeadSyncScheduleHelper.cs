using CRM.DATA;
using Microsoft.EntityFrameworkCore;

namespace CRM.Helpers
{
    /// <summary>Resolves auto-sync intervals from DB options (no hardcoded gaps).</summary>
    public static class LeadSyncScheduleHelper
    {
        public static async Task<int?> GetDefaultIntervalOptionIdAsync(
            TaskDbcontext db,
            CancellationToken cancellationToken = default)
        {
            return await db.LeadSyncIntervalOptions.AsNoTracking()
                .Where(o => o.IsActive && o.Minutes > 0)
                .OrderBy(o => o.SortOrder)
                .ThenBy(o => o.Minutes)
                .Select(o => (int?)o.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public static async Task<int> ResolveIntervalMinutesAsync(
            TaskDbcontext db,
            int? intervalOptionId,
            CancellationToken cancellationToken = default)
        {
            if (intervalOptionId is > 0)
            {
                var minutes = await db.LeadSyncIntervalOptions.AsNoTracking()
                    .Where(o => o.Id == intervalOptionId.Value && o.IsActive && o.Minutes > 0)
                    .Select(o => (int?)o.Minutes)
                    .FirstOrDefaultAsync(cancellationToken);
                if (minutes is > 0)
                {
                    return minutes.Value;
                }
            }

            var fallback = await db.LeadSyncIntervalOptions.AsNoTracking()
                .Where(o => o.IsActive && o.Minutes > 0)
                .OrderBy(o => o.SortOrder)
                .ThenBy(o => o.Minutes)
                .Select(o => (int?)o.Minutes)
                .FirstOrDefaultAsync(cancellationToken);

            if (fallback is > 0)
            {
                return fallback.Value;
            }

            throw new InvalidOperationException(
                "No active lead sync interval options are configured. Run database seed/migration.");
        }

        public static DateTime ComputeNextSyncAt(DateTime fromUtc, int intervalMinutes)
        {
            if (intervalMinutes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(intervalMinutes));
            }

            return fromUtc.AddMinutes(intervalMinutes);
        }
    }
}
