using CRM.DATA;
using Microsoft.EntityFrameworkCore;

namespace CRM.Helpers
{
    public static class OrganizationAddressSchemaEnsure
    {
        public static async Task EnsureAsync(TaskDbcontext db, ILogger logger)
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE organizations ADD COLUMN IF NOT EXISTS address character varying(1024) NOT NULL DEFAULT '';
                ");
                logger.LogInformation("Ensured address column on organizations table.");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to ensure organization address column.");
            }
        }
    }
}
