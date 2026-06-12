using CRM.DATA;
using Microsoft.EntityFrameworkCore;

namespace CRM.Helpers
{
    /// <summary>Idempotent schema ensure for admin daily briefing preferences and cache.</summary>
    public static class DashboardBriefingSchemaSeed
    {
        public static async Task EnsureAsync(TaskDbcontext db, ILogger logger, CancellationToken cancellationToken = default)
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync(
                    """
                    CREATE TABLE IF NOT EXISTS user_dashboard_preferences (
                        user_id integer NOT NULL PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
                        morning_briefing_enabled boolean NOT NULL DEFAULT true,
                        last_briefing_played_date date,
                        updated_at timestamp with time zone NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc')
                    );

                    ALTER TABLE user_dashboard_preferences
                        ADD COLUMN IF NOT EXISTS cached_briefing_date date;

                    ALTER TABLE user_dashboard_preferences
                        ADD COLUMN IF NOT EXISTS cached_briefing_message character varying(1000);

                    ALTER TABLE user_dashboard_preferences
                        ADD COLUMN IF NOT EXISTS cached_briefing_source character varying(32);
                    """,
                    cancellationToken);

                logger.LogInformation("Dashboard briefing schema verified.");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Dashboard briefing schema ensure could not complete.");
            }
        }
    }
}
