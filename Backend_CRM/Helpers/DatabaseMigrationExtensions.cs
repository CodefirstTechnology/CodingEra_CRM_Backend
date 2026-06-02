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

                await EnsureQuotationItemGridSchemaAsync(db, logger);
                await DealPipelineStageSeed.EnsureAsync(db, logger);
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

        private static async Task EnsureQuotationItemGridSchemaAsync(TaskDbcontext db, ILogger logger)
        {
            const string migrationId = "20260601190000_QuotationItemGridErp";

            await db.Database.ExecuteSqlRawAsync("""
                ALTER TABLE IF EXISTS quotation_line_items
                    ADD COLUMN IF NOT EXISTS item_name character varying(256) NOT NULL DEFAULT '';
                ALTER TABLE IF EXISTS quotation_line_items
                    ADD COLUMN IF NOT EXISTS weight numeric NOT NULL DEFAULT 0;
                ALTER TABLE IF EXISTS quotation_line_items
                    ADD COLUMN IF NOT EXISTS unit_weight numeric NOT NULL DEFAULT 0;
                ALTER TABLE IF EXISTS quotation_line_items
                    ADD COLUMN IF NOT EXISTS discount_percent numeric NOT NULL DEFAULT 0;
                ALTER TABLE IF EXISTS quotation_line_items
                    ADD COLUMN IF NOT EXISTS gst_percent numeric NOT NULL DEFAULT 0;
                ALTER TABLE IF EXISTS quotation_line_items
                    ADD COLUMN IF NOT EXISTS tax_amount numeric NOT NULL DEFAULT 0;
                ALTER TABLE IF EXISTS quotation_line_items
                    ADD COLUMN IF NOT EXISTS line_total numeric NOT NULL DEFAULT 0;

                ALTER TABLE IF EXISTS quotations
                    ADD COLUMN IF NOT EXISTS subtotal numeric NOT NULL DEFAULT 0;
                ALTER TABLE IF EXISTS quotations
                    ADD COLUMN IF NOT EXISTS tax_total numeric NOT NULL DEFAULT 0;
                ALTER TABLE IF EXISTS quotations
                    ADD COLUMN IF NOT EXISTS total_quantity numeric NOT NULL DEFAULT 0;
                ALTER TABLE IF EXISTS quotations
                    ADD COLUMN IF NOT EXISTS total_weight numeric NOT NULL DEFAULT 0;

                CREATE TABLE IF NOT EXISTS quotation_item_grid_defaults (
                    id integer PRIMARY KEY,
                    columns_json text NOT NULL DEFAULT '',
                    updated_at timestamp with time zone NOT NULL DEFAULT NOW(),
                    updated_by integer NULL
                );

                CREATE TABLE IF NOT EXISTS quotation_item_grid_user_preferences (
                    user_id integer PRIMARY KEY,
                    columns_json text NOT NULL DEFAULT '',
                    updated_at timestamp with time zone NOT NULL DEFAULT NOW()
                );
                """);

            var inserted = await db.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                SELECT {0}, {1}
                WHERE NOT EXISTS (
                    SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = {0}
                )
                """,
                migrationId,
                "8.0.0");
            if (inserted > 0)
            {
                logger.LogInformation("Applied idempotent schema patch: {MigrationId}", migrationId);
            }
        }
    }
}
