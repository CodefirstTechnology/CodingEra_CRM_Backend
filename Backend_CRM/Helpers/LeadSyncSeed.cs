using CRM.DATA;
using CRM.models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Helpers
{
    /// <summary>Idempotent seed for lead sync sources and interval options.</summary>
    public static class LeadSyncSeed
    {
        public static async Task EnsureAsync(
            TaskDbcontext db,
            ILogger logger,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var now = DateTime.UtcNow;
                await EnsureIntervalsAsync(db, now, cancellationToken);
                await EnsureSourcesAsync(db, now, cancellationToken);
                await NormalizeAutoSyncConfigsAsync(db, now, cancellationToken);
                logger.LogInformation("Lead sync seed verified (sources + intervals).");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Lead sync seed could not complete (tables may not exist yet).");
            }
        }

        private static async Task EnsureIntervalsAsync(
            TaskDbcontext db,
            DateTime now,
            CancellationToken cancellationToken)
        {
            // SortOrder 1 = default when auto sync is enabled without an explicit choice.
            var seeds = new (int Minutes, string Label, int SortOrder)[]
            {
                (10, "Every 10 Minutes", 1),
                (15, "Every 15 Minutes", 2),
                (30, "Every 30 Minutes", 3),
                (60, "Every 1 Hour", 4),
                (120, "Every 2 Hours", 5),
                (180, "Every 3 Hours", 6),
                (240, "Every 4 Hours", 7),
                (360, "Every 6 Hours", 8),
                (720, "Every 12 Hours", 9),
                (1440, "Every 24 Hours", 10),
            };

            foreach (var seed in seeds)
            {
                var existing = await db.LeadSyncIntervalOptions
                    .FirstOrDefaultAsync(o => o.Minutes == seed.Minutes, cancellationToken);
                if (existing == null)
                {
                    db.LeadSyncIntervalOptions.Add(new LeadSyncIntervalOption
                    {
                        Minutes = seed.Minutes,
                        Label = seed.Label,
                        SortOrder = seed.SortOrder,
                        IsActive = true,
                        CreatedAt = now,
                        UpdatedAt = now,
                    });
                }
                else if (!existing.IsActive || existing.Label != seed.Label || existing.SortOrder != seed.SortOrder)
                {
                    existing.Label = seed.Label;
                    existing.SortOrder = seed.SortOrder;
                    existing.IsActive = true;
                    existing.UpdatedAt = now;
                }
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Fills missing interval / next-sync values for enabled configs.
        /// Does not force NextSyncAt = now (avoids restart stampede).
        /// </summary>
        private static async Task NormalizeAutoSyncConfigsAsync(
            TaskDbcontext db,
            DateTime now,
            CancellationToken cancellationToken)
        {
            var defaultIntervalId = await LeadSyncScheduleHelper.GetDefaultIntervalOptionIdAsync(
                db,
                cancellationToken);
            if (defaultIntervalId == null)
            {
                return;
            }

            var enabled = await db.LeadSyncSourceConfigs
                .Where(c => c.AutoSyncEnabled)
                .ToListAsync(cancellationToken);

            var changed = false;
            foreach (var config in enabled)
            {
                if (config.IntervalOptionId == null)
                {
                    config.IntervalOptionId = defaultIntervalId;
                    config.UpdatedAt = now;
                    changed = true;
                }

                if (config.NextSyncAt == null)
                {
                    var minutes = await LeadSyncScheduleHelper.ResolveIntervalMinutesAsync(
                        db,
                        config.IntervalOptionId,
                        cancellationToken);
                    config.NextSyncAt = LeadSyncScheduleHelper.ComputeNextSyncAt(now, minutes);
                    config.UpdatedAt = now;
                    changed = true;
                }
            }

            if (changed)
            {
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        private static async Task EnsureSourcesAsync(
            TaskDbcontext db,
            DateTime now,
            CancellationToken cancellationToken)
        {
            var seeds = new (string Code, string DisplayName, string MarkerName, int SortOrder)[]
            {
                ("indiamart", "IndiaMART", "IndiaMART", 1),
                ("justdial", "Justdial", "Justdial", 2),
                ("tradeindia", "TradeIndia", "TradeIndia", 3),
            };

            foreach (var seed in seeds)
            {
                var source = await db.LeadSyncSources
                    .Include(s => s.Config)
                    .Include(s => s.RoundRobinState)
                    .FirstOrDefaultAsync(s => s.Code == seed.Code, cancellationToken);

                if (source == null)
                {
                    source = new LeadSyncSource
                    {
                        Code = seed.Code,
                        DisplayName = seed.DisplayName,
                        MarkerName = seed.MarkerName,
                        SortOrder = seed.SortOrder,
                        IsActive = true,
                        ApiIntegrationReady = false,
                        CreatedAt = now,
                        UpdatedAt = now,
                    };
                    db.LeadSyncSources.Add(source);
                    await db.SaveChangesAsync(cancellationToken);

                    db.LeadSyncSourceConfigs.Add(new LeadSyncSourceConfig
                    {
                        SourceId = source.Id,
                        AutoSyncEnabled = false,
                        UpdatedAt = now,
                    });
                    db.LeadSyncRoundRobinStates.Add(new LeadSyncRoundRobinState
                    {
                        SourceId = source.Id,
                        NextIndex = 0,
                        UpdatedAt = now,
                    });
                }
                else
                {
                    source.DisplayName = seed.DisplayName;
                    source.MarkerName = seed.MarkerName;
                    source.SortOrder = seed.SortOrder;
                    source.IsActive = true;
                    source.UpdatedAt = now;

                    if (source.Config == null)
                    {
                        db.LeadSyncSourceConfigs.Add(new LeadSyncSourceConfig
                        {
                            SourceId = source.Id,
                            AutoSyncEnabled = false,
                            UpdatedAt = now,
                        });
                    }

                    if (source.RoundRobinState == null)
                    {
                        db.LeadSyncRoundRobinStates.Add(new LeadSyncRoundRobinState
                        {
                            SourceId = source.Id,
                            NextIndex = 0,
                            UpdatedAt = now,
                        });
                    }
                }
            }

            await db.SaveChangesAsync(cancellationToken);
            await RefreshIntegrationReadyFlagsAsync(db, now, cancellationToken);
        }

        private static async Task RefreshIntegrationReadyFlagsAsync(
            TaskDbcontext db,
            DateTime now,
            CancellationToken cancellationToken)
        {
            var sources = await db.LeadSyncSources
                .Include(s => s.Credentials)
                .ToListAsync(cancellationToken);

            foreach (var source in sources)
            {
                var configured = source.Credentials != null
                    && !string.IsNullOrWhiteSpace(source.Credentials.PullApiUrl)
                    && !string.IsNullOrWhiteSpace(source.Credentials.ApiKeyEncrypted);
                if (source.ApiIntegrationReady != configured)
                {
                    source.ApiIntegrationReady = configured;
                    source.UpdatedAt = now;
                }
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
