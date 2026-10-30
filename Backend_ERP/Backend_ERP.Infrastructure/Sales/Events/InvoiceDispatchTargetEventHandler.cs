using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Common.Events;
using ERP.Application.Sales;
using ERP.Domain.Sales.Events;
using Microsoft.Extensions.Logging;

namespace ERP.Infrastructure.Sales.Events
{
    public class InvoiceDispatchTargetEventHandler : 
        INotificationHandler<SalesInvoiceDispatchedDomainEvent>
    {
        private readonly ISalesTargetRealizationEngine _engine;
        private readonly ILogger<InvoiceDispatchTargetEventHandler> _logger;

        public InvoiceDispatchTargetEventHandler(
            ISalesTargetRealizationEngine engine,
            ILogger<InvoiceDispatchTargetEventHandler> logger)
        {
            _engine = engine;
            _logger = logger;
        }

        public async Task Handle(SalesInvoiceDispatchedDomainEvent notification, CancellationToken ct)
        {
            _logger.LogInformation("Processing Sales Invoice Dispatch Realization for Invoice #{InvoiceId}", notification.InvoiceId);
            await _engine.RecordInvoiceDispatchedAsync(notification.InvoiceId, ct);
        }
    }
}

