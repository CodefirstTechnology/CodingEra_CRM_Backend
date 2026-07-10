using CRM.DATA;
using Microsoft.EntityFrameworkCore;

namespace CRM.Helpers
{
    /// <summary>Idempotent ensure for <c>leads.deal_amount</c> (lead amount / pre-conversion deal value).</summary>
    public static class LeadDealAmountSchemaEnsure
    {
        public static async Task EnsureAsync(TaskDbcontext db, ILogger logger, CancellationToken cancellationToken = default)
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync(
                    """
                    ALTER TABLE leads ADD COLUMN IF NOT EXISTS deal_amount numeric NULL;
                    """,
                    cancellationToken);

                logger.LogInformation("Lead deal_amount schema verified.");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Lead deal_amount schema ensure skipped or failed.");
            }
        }
    }
}
