using CRM.DATA;
using Microsoft.EntityFrameworkCore;

namespace CRM.Helpers
{
    /// <summary>
    /// Idempotent schema ensure for quotation-level terms columns.
    /// </summary>
    public static class QuotationTermsSchemaEnsure
    {
        public static async Task EnsureAsync(TaskDbcontext db, ILogger logger, CancellationToken cancellationToken = default)
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync(
                    """
                    ALTER TABLE quotations ADD COLUMN IF NOT EXISTS customize_terms boolean NOT NULL DEFAULT false;
                    ALTER TABLE quotations ADD COLUMN IF NOT EXISTS intro_text text NOT NULL DEFAULT '';
                    ALTER TABLE quotations ADD COLUMN IF NOT EXISTS transportation_label character varying(128) NOT NULL DEFAULT '';
                    ALTER TABLE quotations ADD COLUMN IF NOT EXISTS jurisdiction character varying(256) NOT NULL DEFAULT '';
                    ALTER TABLE quotations ADD COLUMN IF NOT EXISTS terms_conditions_json text NOT NULL DEFAULT '';
                    """,
                    cancellationToken);

                logger.LogInformation("Quotation terms schema verified.");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Quotation terms schema ensure skipped or failed.");
            }
        }
    }
}
