using CRM.DATA;
using Microsoft.EntityFrameworkCore;

namespace CRM.Helpers
{
    /// <summary>
    /// Idempotent schema ensure for deal pipeline tables/columns only.
    /// Stage data is managed exclusively via <c>deal_statuses</c> master data.
    /// </summary>
    public static class DealPipelineStageSeed
    {
        public static async Task EnsureAsync(TaskDbcontext db, ILogger logger, CancellationToken cancellationToken = default)
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync(
                    """
                    ALTER TABLE deals ADD COLUMN IF NOT EXISTS next_follow_up_date timestamp with time zone;
                    ALTER TABLE deals ADD COLUMN IF NOT EXISTS lost_reason text NOT NULL DEFAULT '';
                    ALTER TABLE deal_statuses ADD COLUMN IF NOT EXISTS sort_order integer NOT NULL DEFAULT 0;
                    ALTER TABLE deal_statuses ADD COLUMN IF NOT EXISTS is_won boolean NOT NULL DEFAULT false;
                    ALTER TABLE deal_statuses ADD COLUMN IF NOT EXISTS is_lost boolean NOT NULL DEFAULT false;

                    CREATE TABLE IF NOT EXISTS deal_stage_histories (
                        id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                        deal_id integer NOT NULL REFERENCES deals(id) ON DELETE CASCADE,
                        previous_stage character varying(128) NOT NULL,
                        new_stage character varying(128) NOT NULL,
                        changed_by_user_id integer REFERENCES users(id) ON DELETE SET NULL,
                        changed_at timestamp with time zone NOT NULL,
                        comment text NOT NULL DEFAULT ''
                    );
                    CREATE INDEX IF NOT EXISTS "IX_deal_stage_histories_deal_id_changed_at"
                        ON deal_stage_histories (deal_id, changed_at);
                    """,
                    cancellationToken);

                logger.LogInformation("Deal pipeline schema verified.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to ensure deal pipeline schema.");
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    throw;
                }
            }
        }
    }
}
