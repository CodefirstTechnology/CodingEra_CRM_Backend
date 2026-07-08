using CRM.DATA;
using Microsoft.EntityFrameworkCore;

namespace CRM.Helpers
{
    /// <summary>Idempotent schema ensure for quotation template columns.</summary>
    public static class QuotationTemplateSchemaEnsure
    {
        public static async Task EnsureAsync(TaskDbcontext db, ILogger logger, CancellationToken cancellationToken = default)
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync(
                    """
                    ALTER TABLE quotations ADD COLUMN IF NOT EXISTS quotation_template character varying(32) NOT NULL DEFAULT 'Standard';
                    ALTER TABLE quotations ADD COLUMN IF NOT EXISTS template_payload_json text NOT NULL DEFAULT '';
                    """,
                    cancellationToken);

                logger.LogInformation("Quotation template schema verified.");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Quotation template schema ensure skipped or failed.");
            }
        }
    }
}
