using CRM.DATA;
using Microsoft.EntityFrameworkCore;

namespace CRM.Helpers
{
    /// <summary>Idempotent ensure for company profile browser-tab icon columns.</summary>
    public static class CompanyProfileFaviconSchemaEnsure
    {
        public static async Task EnsureAsync(TaskDbcontext db, ILogger logger, CancellationToken cancellationToken = default)
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync(
                    """
                    ALTER TABLE company_profiles ADD COLUMN IF NOT EXISTS favicon_content_type character varying(64) NOT NULL DEFAULT '';
                    ALTER TABLE company_profiles ADD COLUMN IF NOT EXISTS favicon_base64 text NOT NULL DEFAULT '';
                    """,
                    cancellationToken);

                logger.LogInformation("Company profile favicon schema verified.");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Company profile favicon schema ensure skipped or failed.");
            }
        }
    }
}
