using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Common.Events;
using ERP.Application.Sales;
using ERP.Domain.Sales.Events;
using Microsoft.Extensions.Logging;

namespace ERP.Infrastructure.Sales.Events
{
    public class AdvancePaymentTargetEventHandler : 
        INotificationHandler<AdvancePaymentClearedDomainEvent>,
        INotificationHandler<AdvancePaymentRefundedDomainEvent>
    {
        private readonly ISalesTargetRealizationEngine _engine;
        private readonly ILogger<AdvancePaymentTargetEventHandler> _logger;

        public AdvancePaymentTargetEventHandler(
            ISalesTargetRealizationEngine engine,
            ILogger<AdvancePaymentTargetEventHandler> logger)
        {
            _engine = engine;
            _logger = logger;
        }

        public async Task Handle(AdvancePaymentClearedDomainEvent notification, CancellationToken ct)
        {
            _logger.LogInformation("Processing Advance Payment Clearance Realization for ID #{PaymentId}", notification.AdvancePaymentId);
            await _engine.RecordAdvanceCashAsync(notification.AdvancePaymentId, ct);
        }

        public async Task Handle(AdvancePaymentRefundedDomainEvent notification, CancellationToken ct)
        {
            _logger.LogWarning("Processing Advance Payment Refund Clawback for ID #{PaymentId}", notification.AdvancePaymentId);
            await _engine.ProcessAdvanceClawbackAsync(notification.AdvancePaymentId, notification.Reason, ct);
        }
    }
}

