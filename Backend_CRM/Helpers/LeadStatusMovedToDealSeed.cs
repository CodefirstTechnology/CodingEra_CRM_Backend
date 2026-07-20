using CRM.DATA;
using Microsoft.EntityFrameworkCore;

namespace CRM.Helpers
{
    /// <summary>
    /// Ensures <c>lead_statuses.is_conversion_status</c> exists and exactly one conversion
    /// status is flagged. Display name is free-form (default seed: "Converted").
    /// </summary>
    public static class LeadStatusMovedToDealSeed
    {
        public const string DefaultName = "Converted";

        public static async Task EnsureAsync(TaskDbcontext db, ILogger logger, CancellationToken cancellationToken = default)
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync(
                    """
                    ALTER TABLE lead_statuses
                    ADD COLUMN IF NOT EXISTS is_conversion_status boolean NOT NULL DEFAULT false;
                    """,
                    cancellationToken);

                var flagged = await db.LeadStatuses
                    .Where(s => s.IsConversionStatus)
                    .OrderBy(s => s.Id)
                    .ToListAsync(cancellationToken);

                if (flagged.Count > 1)
                {
                    foreach (var extra in flagged.Skip(1))
                    {
                        extra.IsConversionStatus = false;
                    }

                    await db.SaveChangesAsync(cancellationToken);
                    logger.LogInformation("Normalized multiple conversion lead statuses to a single flag.");
                    return;
                }

                if (flagged.Count == 1)
                {
                    return;
                }

                // Promote known default/legacy conversion names if present (do not rename).
                var byName = await db.LeadStatuses
                    .Where(s =>
                        s.Name.ToLower() == DefaultName.ToLower() ||
                        s.Name.ToLower() == "moved to deal")
                    .OrderBy(s => s.Name.ToLower() == DefaultName.ToLower() ? 0 : 1)
                    .ThenBy(s => s.Id)
                    .ToListAsync(cancellationToken);

                if (byName.Count > 0)
                {
                    var primary = byName[0];
                    primary.IsConversionStatus = true;
                    if (string.IsNullOrWhiteSpace(primary.Description))
                    {
                        primary.Description =
                            "Lead has been converted into a deal (not Won / not revenue).";
                    }

                    // Live-safe: remap leads to the primary conversion row, then
                    // deactivate duplicates (do not delete master rows).
                    foreach (var dup in byName.Skip(1))
                    {
                        await db.Database.ExecuteSqlInterpolatedAsync(
                            $"UPDATE leads SET lead_status_id = {primary.Id} WHERE lead_status_id = {dup.Id}",
                            cancellationToken);
                        dup.IsConversionStatus = false;
                        dup.IsActive = false;
                        dup.UpdatedAt = DateTime.UtcNow;
                        dup.LastModified = DateTime.UtcNow;
                    }

                    await db.SaveChangesAsync(cancellationToken);
                    logger.LogInformation(
                        "Flagged lead status '{Name}' (id {Id}) as conversion status.",
                        primary.Name,
                        primary.Id);
                    return;
                }

                var now = DateTime.UtcNow;
                await db.LeadStatuses.AddAsync(
                    new models.LeadStatus
                    {
                        Name = DefaultName,
                        Description =
                            "Lead has been converted into a deal (not Won / not revenue).",
                        IsActive = true,
                        IsConversionStatus = true,
                        CreatedAt = now,
                        UpdatedAt = now,
                        LastModified = now,
                    },
                    cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Created conversion lead status '{Name}'.", DefaultName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to ensure conversion lead status flag.");
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    throw;
                }
            }
        }

        /// <summary>True for known default/legacy conversion status labels (fallback).</summary>
        public static bool IsConversionStatusName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            var key = name.Trim().ToLowerInvariant();
            return key is "converted" or "moved to deal";
        }

        public static async Task<IReadOnlyList<string>> ConversionStatusLookupNamesAsync(
            TaskDbcontext db,
            string? requested = null,
            CancellationToken cancellationToken = default)
        {
            var list = new List<string>();
            void Add(string? n)
            {
                if (string.IsNullOrWhiteSpace(n)) return;
                var t = n.Trim();
                if (list.Any(x => string.Equals(x, t, StringComparison.OrdinalIgnoreCase))) return;
                list.Add(t);
            }

            var flaggedName = await db.LeadStatuses.AsNoTracking()
                .Where(s => s.IsConversionStatus && s.IsActive)
                .OrderBy(s => s.Id)
                .Select(s => s.Name)
                .FirstOrDefaultAsync(cancellationToken);

            var req = requested?.Trim() ?? "";
            var wantsConversionAliases =
                string.IsNullOrWhiteSpace(req) ||
                IsConversionStatusName(req) ||
                (!string.IsNullOrWhiteSpace(flaggedName) &&
                 string.Equals(flaggedName, req, StringComparison.OrdinalIgnoreCase));

            if (wantsConversionAliases)
            {
                // Only expand to conversion aliases when filtering/resolving conversion.
                Add(flaggedName);
                Add(DefaultName);
                Add("Moved to Deal");
            }
            else
            {
                // Exact non-conversion name (e.g. Qualified) — do not mix in conversion.
                Add(req);
            }

            return list;
        }

        public static IReadOnlyList<string> ConversionStatusLookupNames(string? requested = null)
        {
            var list = new List<string>();
            void Add(string? n)
            {
                if (string.IsNullOrWhiteSpace(n)) return;
                var t = n.Trim();
                if (list.Any(x => string.Equals(x, t, StringComparison.OrdinalIgnoreCase))) return;
                list.Add(t);
            }

            if (IsConversionStatusName(requested))
            {
                Add(DefaultName);
                Add("Moved to Deal");
            }
            else
            {
                Add(requested);
            }

            return list;
        }
    }
}
