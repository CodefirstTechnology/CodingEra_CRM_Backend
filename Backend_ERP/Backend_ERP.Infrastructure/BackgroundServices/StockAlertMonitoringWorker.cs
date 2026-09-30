using System;
using System.Linq;
using System.Text.Json;
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
    public class StockAlertMonitoringWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<StockAlertMonitoringWorker> _logger;
        private readonly TimeSpan _pollingInterval = TimeSpan.FromMinutes(2);

        public StockAlertMonitoringWorker(IServiceScopeFactory scopeFactory, ILogger<StockAlertMonitoringWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Stock Alert Monitoring Worker initialized.");

            int backoffFactor = 1;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessStockAlertsAsync(stoppingToken);
                    backoffFactor = 1; // Reset backoff on successful execution
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during stock alert evaluation cycle. Backing off...");
                    backoffFactor = Math.Min(backoffFactor * 2, 16); // Exponential backoff up to ~32 min max
                }

                int delaySeconds = (int)(_pollingInterval.TotalSeconds * backoffFactor);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
            }

            _logger.LogInformation("Stock Alert Monitoring Worker is shutting down.");
        }

        private async Task ProcessStockAlertsAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ERPDbContext>();

            var thresholds = await dbContext.StockAlertThresholds
                .Where(t => t.IsActive)
                .AsNoTracking()
                .ToListAsync(ct);

            if (thresholds.Count == 0) return;

            foreach (var threshold in thresholds)
            {
                if (ct.IsCancellationRequested) break;

                // Calculate current stock position from stock transactions
                decimal currentQty = await dbContext.StockTransactions
                    .Where(st => st.MaterialId == threshold.ItemId && !st.IsDeleted)
                    .SumAsync(st => st.Quantity, ct);

                // Mock/Calculate OnOrderQty and ReservedQty for position calculation
                decimal onOrderQty = 0m;
                decimal reservedQty = 0m;
                decimal effectiveQty = currentQty + onOrderQty - reservedQty;

                string? triggeredType = null;
                string priority = "Warning";

                if (effectiveQty <= threshold.MinStockLevel)
                {
                    triggeredType = "MinStock";
                    priority = "Critical";
                }
                else if (effectiveQty <= threshold.ReorderPoint)
                {
                    triggeredType = "ReorderPoint";
                    priority = "Warning";
                }
                else if (threshold.MaxStockLevel > 0 && effectiveQty > threshold.MaxStockLevel)
                {
                    triggeredType = "MaxStockOverflow";
                    priority = "Normal";
                }

                if (triggeredType != null)
                {
                    // Check if active alert already exists to prevent duplicate alert spam
                    bool activeAlertExists = await dbContext.StockAlertLogs
                        .AnyAsync(a => a.ThresholdId == threshold.ThresholdId && a.AlertType == triggeredType && a.Status == "Active", ct);

                    if (!activeAlertExists)
                    {
                        var alertLog = new StockAlertLogEntity
                        {
                            ThresholdId = threshold.ThresholdId,
                            ItemId = threshold.ItemId,
                            WarehouseId = threshold.WarehouseId,
                            AlertType = triggeredType,
                            AlertPriority = priority,
                            CurrentQty = currentQty,
                            AvailableQty = effectiveQty,
                            OnOrderQty = onOrderQty,
                            Status = "Active",
                            CreatedAt = DateTime.UtcNow
                        };

                        dbContext.StockAlertLogs.Add(alertLog);

                        // Persist Outbox Event
                        var alertEvent = JsonSerializer.Serialize(new
                        {
                            alertLog.AlertId,
                            threshold.ItemId,
                            threshold.WarehouseId,
                            triggeredType,
                            priority,
                            currentQty,
                            effectiveQty
                        });

                        dbContext.OutboxMessages.Add(new OutboxMessageEntity
                        {
                            EventType = "StockAlertTriggeredEvent",
                            PayloadJson = alertEvent
                        });

                        await dbContext.SaveChangesAsync(ct);
                        _logger.LogWarning("Triggered {AlertType} Alert for Item {ItemId} at WH {WarehouseId}. Effective Qty: {Qty}.", triggeredType, threshold.ItemId, threshold.WarehouseId, effectiveQty);
                    }
                }
            }
        }
    }
}
