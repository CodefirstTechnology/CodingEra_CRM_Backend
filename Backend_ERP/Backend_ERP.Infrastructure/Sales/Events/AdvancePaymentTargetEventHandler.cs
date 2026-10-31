using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Common.Events;
using ERP.Application.Sales;
using ERP.Domain.Sales.Events;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Infrastructure.Sales.Events
{
    public class AdvancePaymentTargetEventHandler : 
        INotificationHandler<AdvancePaymentClearedDomainEvent>,
        INotificationHandler<AdvancePaymentRefundedDomainEvent>
    {
        private readonly ISalesTargetRealizationEngine _engine;
        private readonly ISalesIncentiveEngine? _incentiveEngine;
        private readonly ERPDbContext? _context;
        private readonly IPerformanceRecalculationService? _recalcService;
        private readonly ILogger<AdvancePaymentTargetEventHandler> _logger;

        public AdvancePaymentTargetEventHandler(
            ISalesTargetRealizationEngine engine,
            ILogger<AdvancePaymentTargetEventHandler> logger,
            ISalesIncentiveEngine? incentiveEngine = null,
            ERPDbContext? context = null,
            IPerformanceRecalculationService? recalcService = null)
        {
            _engine = engine;
            _logger = logger;
            _incentiveEngine = incentiveEngine;
            _context = context;
            _recalcService = recalcService;
        }

        public async Task Handle(AdvancePaymentClearedDomainEvent notification, CancellationToken ct)
        {
            _logger.LogInformation("Processing Advance Payment Clearance Realization for ID #{PaymentId}", notification.AdvancePaymentId);
            await _engine.RecordAdvanceCashAsync(notification.AdvancePaymentId, ct);

            if (_context != null)
            {
                var adv = await _context.AdvancePayments
                    .Include(p => p.SalesOrder)
                    .FirstOrDefaultAsync(p => p.Id == notification.AdvancePaymentId, ct);

                if (adv != null)
                {
                    if (adv.SalesOrderId.HasValue && _incentiveEngine != null)
                    {
                        var order = adv.SalesOrder ?? await _context.SalesOrders.FindAsync(new object[] { adv.SalesOrderId.Value }, ct);
                        if (order != null && order.TotalAmount > 0)
                        {
                            var totalCleared = await _context.AdvancePayments
                                .Where(a => a.SalesOrderId == adv.SalesOrderId.Value && a.Status == "Cleared")
                                .SumAsync(a => (decimal?)a.AdvanceAmount, ct) ?? 0m;

                            decimal clearedPct = Math.Round((totalCleared / order.TotalAmount) * 100m, 1);
                            await _incentiveEngine.ReleaseCashGatedCommissionsAsync(adv.SalesOrderId.Value, clearedPct, ct);
                        }
                    }

                    int? spUserId = adv.SalesOrder?.SalesPersonUserId;
                    if (spUserId.HasValue && _recalcService != null)
                    {
                        var fy = adv.PaymentDate != default ? adv.PaymentDate.Year : DateTime.UtcNow.Year;
                        await _recalcService.RecalculateRepPerformanceAsync(spUserId.Value, fy, ct);
                    }
                }
            }
        }

        public async Task Handle(AdvancePaymentRefundedDomainEvent notification, CancellationToken ct)
        {
            _logger.LogWarning("Processing Advance Payment Refund Clawback for ID #{PaymentId}", notification.AdvancePaymentId);
            await _engine.ProcessAdvanceClawbackAsync(notification.AdvancePaymentId, notification.Reason, ct);

            if (_context != null && _recalcService != null)
            {
                var adv = await _context.AdvancePayments
                    .Include(p => p.SalesOrder)
                    .FirstOrDefaultAsync(p => p.Id == notification.AdvancePaymentId, ct);

                int? spUserId = adv?.SalesOrder?.SalesPersonUserId;
                if (spUserId.HasValue)
                {
                    var fy = adv!.PaymentDate != default ? adv.PaymentDate.Year : DateTime.UtcNow.Year;
                    await _recalcService.RecalculateRepPerformanceAsync(spUserId.Value, fy, ct);
                }
            }
        }
    }
}
