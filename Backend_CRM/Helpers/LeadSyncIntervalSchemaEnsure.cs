using CRM.DATA;
using Microsoft.EntityFrameworkCore;

namespace CRM.Helpers
{
    /// <summary>
    /// Idempotent rename of lead_sync_interval_options.hours → minutes (hours × 60).
    /// </summary>
    public static class LeadSyncIntervalSchemaEnsure
    {
        public static async Task EnsureAsync(TaskDbcontext db, ILogger logger)
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync(
                    """
                    DO $$
                    BEGIN
                      IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'lead_sync_interval_options'
                          AND column_name = 'hours'
                      ) THEN
                        ALTER INDEX IF EXISTS "IX_lead_sync_interval_options_hours"
                          RENAME TO "IX_lead_sync_interval_options_minutes";
                        ALTER TABLE lead_sync_interval_options RENAME COLUMN hours TO minutes;
                        UPDATE lead_sync_interval_options
                        SET minutes = minutes * 60
                        WHERE minutes > 0 AND minutes < 60;
                      END IF;
                    END $$;
                    """);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Lead sync interval minutes schema ensure skipped.");
            }
        }
    }
}
