using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backend_ERP.Domain.Entities;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Backend_ERP.Infrastructure.BackgroundServices
{
    public class OutboxProcessorBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OutboxProcessorBackgroundService> _logger;
        private bool _isSchemaEnsured = false;

        public OutboxProcessorBackgroundService(IServiceProvider serviceProvider, ILogger<OutboxProcessorBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<ERPDbContext>();

                    // Ensure Schema & Outbox Table Exist Dynamically in PostgreSQL
                    if (!_isSchemaEnsured)
                    {
                        await dbContext.Database.ExecuteSqlRawAsync(@"
                            CREATE TABLE IF NOT EXISTS outbox_messages (
                                outbox_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                                event_type VARCHAR(250) NOT NULL,
                                payload_json TEXT NOT NULL,
                                created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                processed_at TIMESTAMP WITH TIME ZONE NULL,
                                error_log TEXT NULL,
                                retry_count INT NOT NULL DEFAULT 0
                            );
                            ALTER TABLE stock_transactions ADD COLUMN IF NOT EXISTS ""IdempotencyKey"" UUID DEFAULT gen_random_uuid();
                            ALTER TABLE stock_transactions ADD COLUMN IF NOT EXISTS ""SecondaryQty"" NUMERIC(18,4) NULL;
                            ALTER TABLE raw_materials ADD COLUMN IF NOT EXISTS ""CatchWeightTolerancePercent"" NUMERIC(5,2) DEFAULT 3.00;
                        ", stoppingToken);

                        _isSchemaEnsured = true;
                    }

                    var pendingMessages = await dbContext.Set<OutboxMessageEntity>()
                        .Where(x => x.ProcessedAt == null && x.RetryCount < 5)
                        .OrderBy(x => x.CreatedAt)
                        .Take(20)
                        .ToListAsync(stoppingToken);

                    foreach (var message in pendingMessages)
                    {
                        try
                        {
                            // Process outbox event dispatching
                            message.ProcessedAt = DateTime.UtcNow;
                        }
                        catch (Exception ex)
                        {
                            message.RetryCount++;
                            message.ErrorLog = ex.Message;
                        }
                    }

                    if (pendingMessages.Count > 0)
                    {
                        await dbContext.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing transactional outbox messages.");
                }

                await Task.Delay(5000, stoppingToken); // Poll every 5 seconds
            }
        }
    }
}
