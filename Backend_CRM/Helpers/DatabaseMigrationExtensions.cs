using CRM.Configuration;
using CRM.DATA;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CRM.Helpers
{
    public static class DatabaseMigrationExtensions
    {
        public static async Task ApplyPendingMigrationsAsync(this WebApplication app)
        {
            var options = app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            if (!options.AutoMigrateOnStartup)
            {
                return;
            }

            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TaskDbcontext>();
            var logger = scope.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("DatabaseMigration");

            try
            {
                var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
                if (pending.Count > 0)
                {
                    logger.LogInformation(
                        "Applying {Count} pending migration(s): {Names}",
                        pending.Count,
                        string.Join(", ", pending));
                }

                await db.Database.MigrateAsync();
                logger.LogInformation("Database schema is up to date.");

                await LeadDealAmountSchemaEnsure.EnsureAsync(db, logger);
                await CompanyProfileFaviconSchemaEnsure.EnsureAsync(db, logger);
                await QuotationTermsSchemaEnsure.EnsureAsync(db, logger);
                await QuotationTemplateSchemaEnsure.EnsureAsync(db, logger);
                await LeadSyncIntervalSchemaEnsure.EnsureAsync(db, logger);
                await DealPipelineStageSeed.EnsureAsync(db, logger);
                await RbacSeed.EnsureAsync(db, logger);

                await LeadSyncSeed.EnsureAsync(db, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to apply database migrations.");
                if (app.Environment.IsDevelopment())
                {
                    throw;
                }
            }
        }
    }
}
