using CRM.DATA;
using Microsoft.EntityFrameworkCore;

namespace CRM.Helpers
{
    /// <summary>
    /// Idempotent seed for deal pipeline master data. Safe to run on every startup
    /// when migrations were not applied (e.g. missing Designer.cs).
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

                    INSERT INTO deal_statuses (name, description, is_active, created_at, updated_at, last_modified)
                    VALUES
                      ('Quotation Shared', 'Quotation shared with customer', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                      ('Follow-Up Ongoing', 'Active follow-up in progress', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                      ('Site Visit / Meeting Done', 'Site visit or meeting completed', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                      ('Technical Approval', 'Technical approval stage', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                      ('Sample Approval', 'Sample approval stage', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                      ('Negotiation Stage', 'Deal under negotiation', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                      ('PO Received', 'Purchase order received', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                      ('Advance Payment Pending', 'Awaiting advance payment', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                      ('Advance Payment Received', 'Advance payment received', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                      ('Production Started', 'Production has started', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                      ('Material Ready For Dispatch', 'Material ready for dispatch', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                      ('Full Payment Pending', 'Awaiting full payment', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                      ('Full Payment Received', 'Full payment received', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                      ('Material Dispatched', 'Material dispatched', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                      ('Material Delivered', 'Material delivered', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                      ('Lead Closed - Won', 'Deal closed as won', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                      ('Lead Closed - Lost', 'Deal closed as lost', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc')
                    ON CONFLICT (name) DO UPDATE SET
                      is_active = true,
                      updated_at = NOW() AT TIME ZONE 'utc',
                      last_modified = NOW() AT TIME ZONE 'utc';

                    UPDATE deal_statuses
                    SET is_active = false,
                        updated_at = NOW() AT TIME ZONE 'utc',
                        last_modified = NOW() AT TIME ZONE 'utc'
                    WHERE name IN (
                      'Qualification', 'Proposal', 'Negotiation', 'Demo/Making', 'Closed Won', 'Closed Lost'
                    );
                    """,
                    cancellationToken);

                logger.LogInformation("Deal pipeline stages verified in database.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to seed deal pipeline stages.");
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    throw;
                }
            }
        }
    }
}
