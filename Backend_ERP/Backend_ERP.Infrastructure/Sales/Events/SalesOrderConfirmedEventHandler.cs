using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Common.Events;
using ERP.Application.Sales;
using ERP.Domain.Sales.Events;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Sales.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ERP.Infrastructure.Sales.Events
{
    public class SalesOrderConfirmedEventHandler : 
        INotificationHandler<SalesOrderConfirmedDomainEvent>,
        INotificationHandler<SalesOrderCancelledDomainEvent>
    {
        private readonly ISalesTargetRealizationEngine _engine;
        private readonly IPerformanceRecalculationService? _recalcService;
        private readonly ISalesIncentiveEngine? _incentiveEngine;
        private readonly ERPDbContext? _context;
        private readonly ILogger<SalesOrderConfirmedEventHandler> _logger;

        public SalesOrderConfirmedEventHandler(
            ISalesTargetRealizationEngine engine,
            ILogger<SalesOrderConfirmedEventHandler> logger,
            IPerformanceRecalculationService? recalcService = null,
            ISalesIncentiveEngine? incentiveEngine = null,
            ERPDbContext? context = null)
        {
            _engine = engine;
            _logger = logger;
            _recalcService = recalcService;
            _incentiveEngine = incentiveEngine;
            _context = context;
        }

        public static SalesOrderConfirmedEventHandler CreateForTest(
            ERPDbContext context,
            IPerformanceRecalculationService? recalcService = null,
            ILogger<SalesOrderConfirmedEventHandler>? logger = null)
        {
            var cacheManager = new TargetRedisCacheManager(null);
            var engine = new SalesTargetRealizationEngine(context, cacheManager, NullLogger<SalesTargetRealizationEngine>.Instance);
            var incentiveEngine = new SalesIncentiveEngine(context, NullLogger<SalesIncentiveEngine>.Instance);
            return new SalesOrderConfirmedEventHandler(
                engine,
                logger ?? NullLogger<SalesOrderConfirmedEventHandler>.Instance,
                recalcService,
                incentiveEngine,
                context);
        }

        public async Task Handle(SalesOrderConfirmedDomainEvent notification, CancellationToken ct)
        {
            _logger.LogInformation("Domain event SalesOrderConfirmedDomainEvent received for SO #{OrderId}", notification.SalesOrder.Id);
            await _engine.RecordOrderBookingAsync(notification.SalesOrder.Id, ct);

            var spUserId = notification.SalesOrder.SalesPersonUserId;
            if (!spUserId.HasValue && int.TryParse(notification.SalesOrder.SalesPerson, out var parsed))
            {
                spUserId = parsed;
            }

            if (spUserId.HasValue)
            {
                var fy = notification.SalesOrder.OrderDate != default
                    ? notification.SalesOrder.OrderDate.Year
                    : notification.SalesOrder.CreatedDate.Year;

                // Accrue Commission Ledger
                if (_incentiveEngine != null)
                {
                    try
                    {
                        decimal advanceClearedPct = 0m;
                        if (_context != null)
                        {
                            var totalClearedAdvances = await _context.AdvancePayments
                                .Where(a => a.SalesOrderId == notification.SalesOrder.Id && a.Status == "Cleared")
                                .SumAsync(a => (decimal?)a.AdvanceAmount, ct) ?? 0m;

                            if (notification.SalesOrder.TotalAmount > 0)
                            {
                                advanceClearedPct = Math.Round((totalClearedAdvances / notification.SalesOrder.TotalAmount) * 100m, 1);
                            }
                        }

                        await _incentiveEngine.AccrueOrderCommissionAsync(
                            spUserId.Value,
                            fy,
                            notification.SalesOrder.Id,
                            notification.SalesOrder.SalesOrderNumber ?? $"SO-{notification.SalesOrder.Id}",
                            notification.SalesOrder.TotalAmount,
                            22.5m, // Default gross margin
                            advanceClearedPct,
                            100.0m,
                            ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to accrue commission for Order #{OrderId}", notification.SalesOrder.Id);
                    }
                }

                // Recalculate Performance
                if (_recalcService != null)
                {
                    await _recalcService.RecalculateRepPerformanceAsync(spUserId.Value, fy, ct);
                }
            }
        }

        public async Task Handle(SalesOrderCancelledDomainEvent notification, CancellationToken ct)
        {
            _logger.LogWarning("Domain event SalesOrderCancelledDomainEvent received for SO #{OrderId}", notification.SalesOrderId);
            await _engine.ProcessOrderClawbackAsync(notification.SalesOrderId, "Sales Order Cancelled by Ops", ct);

            if (_incentiveEngine != null && _context != null)
            {
                var order = await _context.SalesOrders.FindAsync(new object[] { notification.SalesOrderId }, ct);
                if (order != null)
                {
                    var spUserId = order.SalesPersonUserId;
                    if (!spUserId.HasValue && int.TryParse(order.SalesPerson, out var parsed))
                    {
                        spUserId = parsed;
                    }

                    if (spUserId.HasValue)
                    {
                        var fy = order.OrderDate != default ? order.OrderDate.Year : order.CreatedDate.Year;

                        var existingLedger = await _context.SalesCommissionLedgers
                            .FirstOrDefaultAsync(l => l.SalesOrderId == notification.SalesOrderId && !l.IsClawback, ct);

                        if (existingLedger != null)
                        {
                            await _incentiveEngine.RecordClawbackAsync(
                                spUserId.Value,
                                fy,
                                notification.SalesOrderId,
                                order.SalesOrderNumber ?? $"SO-{notification.SalesOrderId}",
                                existingLedger.AccruedCommission,
                                "Sales Order Cancelled",
                                ct);
                        }

                        if (_recalcService != null)
                        {
                            await _recalcService.RecalculateRepPerformanceAsync(spUserId.Value, fy, ct);
                        }
                    }
                }
            }
        }
    }
}
