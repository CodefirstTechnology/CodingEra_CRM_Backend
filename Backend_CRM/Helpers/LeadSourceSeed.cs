using CRM.DATA;
using CRM.models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Helpers
{
    /// <summary>Idempotent seed and schema ensure for CRM lead sources.</summary>
    public static class LeadSourceSeed
    {
        public static async Task EnsureAsync(
            TaskDbcontext db,
            ILogger logger,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Ensure table exists even if EF migration has not executed yet
                const string createSql = @"
CREATE TABLE IF NOT EXISTS lead_sources (
    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name character varying(128) NOT NULL,
    description text NOT NULL DEFAULT '',
    sort_order integer NOT NULL DEFAULT 0,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
    updated_at timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
    last_modified timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
    created_by integer NULL,
    updated_by integer NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_lead_sources_name ON lead_sources (name);
";
                await db.Database.ExecuteSqlRawAsync(createSql, cancellationToken);

                var now = DateTime.UtcNow;
                var seeds = new (string Name, string Description, int SortOrder)[]
                {
                    ("Manual", "Manually entered leads", 1),
                    ("IndiaMART", "IndiaMART marketplace inquiries", 2),
                    ("TradeIndia", "TradeIndia marketplace inquiries", 3),
                    ("Justdial", "Justdial inquiries", 4),
                    ("Website", "Website inquiries and forms", 5),
                    ("Referral", "Customer and partner referrals", 6),
                    ("Cold Outreach", "Cold calling, email, and outreach", 7),
                };

                var existing = await db.LeadSources.AsNoTracking()
                    .Select(s => s.Name.ToLower())
                    .ToListAsync(cancellationToken);
                var existingSet = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

                var added = 0;
                foreach (var (name, description, sortOrder) in seeds)
                {
                    if (existingSet.Contains(name)) continue;

                    db.LeadSources.Add(new LeadSource
                    {
                        Name = name,
                        Description = description,
                        SortOrder = sortOrder,
                        IsActive = true,
                        CreatedAt = now,
                        UpdatedAt = now,
                        LastModified = now,
                    });
                    added++;
                }

                if (added > 0)
                {
                    await db.SaveChangesAsync(cancellationToken);
                    logger.LogInformation("Lead source seed: added {Count} source(s).", added);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Lead source seed could not complete (tables may not exist yet).");
            }
        }
    }
}
