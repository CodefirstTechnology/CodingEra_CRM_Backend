using CRM.DATA;
using CRM.models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CRM.Helpers
{
    /// <summary>Idempotent seed for lead sync sources and interval options.</summary>
    public static class LeadSyncSeed
    {
        public static async Task EnsureAsync(
            TaskDbcontext db,
            IConfiguration configuration,
            ILogger logger,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var now = DateTime.UtcNow;
                await EnsureIntervalsAsync(db, now, cancellationToken);
                await EnsureSourcesAsync(db, configuration, now, cancellationToken);
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
            var seeds = new (int Hours, string Label, int SortOrder)[]
            {
                (1, "Every 1 Hour", 1),
                (2, "Every 2 Hours", 2),
                (3, "Every 3 Hours", 3),
                (4, "Every 4 Hours", 4),
                (6, "Every 6 Hours", 5),
                (12, "Every 12 Hours", 6),
                (24, "Every 24 Hours", 7),
            };

            foreach (var seed in seeds)
            {
                var existing = await db.LeadSyncIntervalOptions
                    .FirstOrDefaultAsync(o => o.Hours == seed.Hours, cancellationToken);
                if (existing == null)
                {
                    db.LeadSyncIntervalOptions.Add(new LeadSyncIntervalOption
                    {
                        Hours = seed.Hours,
                        Label = seed.Label,
                        SortOrder = seed.SortOrder,
                        IsActive = true,
                        CreatedAt = now,
                        UpdatedAt = now,
                    });
                }
                else if (!existing.IsActive || existing.Label != seed.Label)
                {
                    existing.Label = seed.Label;
                    existing.SortOrder = seed.SortOrder;
                    existing.IsActive = true;
                    existing.UpdatedAt = now;
                }
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        private static async Task EnsureSourcesAsync(
            TaskDbcontext db,
            IConfiguration configuration,
            DateTime now,
            CancellationToken cancellationToken)
        {
            var indiaMartReady = IsIndiaMartConfigured(configuration);

            var seeds = new (string Code, string DisplayName, string MarkerName, int SortOrder, bool ApiReady)[]
            {
                ("indiamart", "IndiaMART", "IndiaMART", 1, indiaMartReady),
                ("justdial", "Justdial", "Justdial", 2, false),
                ("tradeindia", "TradeIndia", "TradeIndia", 3, false),
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
                        ApiIntegrationReady = seed.ApiReady,
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
                    if (seed.Code == "indiamart")
                    {
                        source.ApiIntegrationReady = seed.ApiReady;
                    }

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
        }

        private static bool IsIndiaMartConfigured(IConfiguration configuration)
        {
            var section = configuration.GetSection("LeadSync:IndiaMart");
            var url = section["PullApiUrl"]?.Trim();
            var key = section["ApiKey"]?.Trim();
            return !string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(key);
        }
    }
}
