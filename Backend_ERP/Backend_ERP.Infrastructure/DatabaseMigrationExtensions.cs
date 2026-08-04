using ERP.Infrastructure.Data;
using ERP.Shared.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ERP.Infrastructure
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
            var db = scope.ServiceProvider.GetRequiredService<ERPDbContext>();
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
