using System;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Common.Events;
using ERP.Application.Sales;
using ERP.Domain.Sales.Events;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Sales.Caching;
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
        private readonly ILogger<SalesOrderConfirmedEventHandler> _logger;

        public SalesOrderConfirmedEventHandler(
            ISalesTargetRealizationEngine engine,
            ILogger<SalesOrderConfirmedEventHandler> logger,
            IPerformanceRecalculationService? recalcService = null)
        {
            _engine = engine;
            _logger = logger;
            _recalcService = recalcService;
        }

        public static SalesOrderConfirmedEventHandler CreateForTest(
            ERPDbContext context,
            IPerformanceRecalculationService? recalcService = null,
            ILogger<SalesOrderConfirmedEventHandler>? logger = null)
        {
            var cacheManager = new TargetRedisCacheManager(null);
            var engine = new SalesTargetRealizationEngine(context, cacheManager, NullLogger<SalesTargetRealizationEngine>.Instance);
            return new SalesOrderConfirmedEventHandler(engine, logger ?? NullLogger<SalesOrderConfirmedEventHandler>.Instance, recalcService);
        }

        public async Task Handle(SalesOrderConfirmedDomainEvent notification, CancellationToken ct)
        {
            _logger.LogInformation("Domain event SalesOrderConfirmedDomainEvent received for SO #{OrderId}", notification.SalesOrder.Id);
            await _engine.RecordOrderBookingAsync(notification.SalesOrder.Id, ct);

            if (_recalcService != null)
            {
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
                    await _recalcService.RecalculateRepPerformanceAsync(spUserId.Value, fy, ct);
                }
            }
        }

        public async Task Handle(SalesOrderCancelledDomainEvent notification, CancellationToken ct)
        {
            _logger.LogWarning("Domain event SalesOrderCancelledDomainEvent received for SO #{OrderId}", notification.SalesOrderId);
            await _engine.ProcessOrderClawbackAsync(notification.SalesOrderId, "Sales Order Cancelled by Ops", ct);
        }
    }
}
