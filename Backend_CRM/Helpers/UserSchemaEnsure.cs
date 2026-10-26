using CRM.DATA;
using Microsoft.EntityFrameworkCore;

namespace CRM.Helpers
{
    /// <summary>Idempotent ensure for user online/active timestamps.</summary>
    public static class UserSchemaEnsure
    {
        public static async Task EnsureAsync(TaskDbcontext db, ILogger logger, CancellationToken cancellationToken = default)
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync(
                    """
                    ALTER TABLE users ADD COLUMN IF NOT EXISTS last_active_at timestamp with time zone;
                    ALTER TABLE users ADD COLUMN IF NOT EXISTS first_login_at timestamp with time zone;
                    ALTER TABLE users ADD COLUMN IF NOT EXISTS is_online boolean NOT NULL DEFAULT false;
                    """,
                    cancellationToken);

                logger.LogInformation("Users table active/login timestamps schema verified.");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Users table active/login schema ensure skipped or failed.");
            }
        }
    }
}
