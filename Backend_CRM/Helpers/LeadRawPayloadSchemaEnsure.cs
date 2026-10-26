using CRM.DATA;
using Microsoft.EntityFrameworkCore;

namespace CRM.Helpers
{
    /// <summary>Idempotent ensure for <c>leads.raw_payload</c> (JSONB / TEXT metadata storage).</summary>
    public static class LeadRawPayloadSchemaEnsure
    {
        public static async Task EnsureAsync(TaskDbcontext db, ILogger logger, CancellationToken cancellationToken = default)
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync(
                    """
                    ALTER TABLE leads ADD COLUMN IF NOT EXISTS raw_payload jsonb NULL;
                    """,
                    cancellationToken);

                logger.LogInformation("Lead raw_payload schema verified.");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Lead raw_payload schema ensure skipped or failed.");
            }
        }
    }
}
