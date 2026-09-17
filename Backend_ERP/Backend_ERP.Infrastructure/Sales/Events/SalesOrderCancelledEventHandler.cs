using ERP.Application.Common.Events;
using ERP.Application.Sales;
using ERP.Domain.Sales;
using ERP.Domain.Sales.Events;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales.Events
{
    public class SalesOrderCancelledEventHandler : INotificationHandler<SalesOrderCancelledDomainEvent>
    {
        private readonly ERPDbContext _context;
        private readonly IPerformanceRecalculationService? _recalcService;

        public SalesOrderCancelledEventHandler(ERPDbContext context, IPerformanceRecalculationService? recalcService = null)
        {
            _context = context;
            _recalcService = recalcService;
        }

        public async Task Handle(SalesOrderCancelledDomainEvent notification, CancellationToken ct)
        {
            var orderId = notification.SalesOrderId;

            var order = await _context.SalesOrders.FirstOrDefaultAsync(so => so.Id == orderId, ct);

            var existingRealizations = await _context.SalesTargetRealizations
                .Include(r => r.Target)
                    .ThenInclude(t => t.ProgressHistory)
                .Where(r => r.SalesOrderId == orderId && !r.IsReversal)
                .ToListAsync(ct);

            foreach (var original in existingRealizations)
            {
                var target = original.Target;

                _context.SalesTargetRealizations.Add(new SalesTargetRealization
                {
                    SalesTargetId = target.Id,
                    SalesOrderId = orderId,
                    TransactionType = "SalesOrderReversal",
                    RealizedAmount = -original.RealizedAmount,
                    IsReversal = true,
                    AppliedOn = DateTimeOffset.UtcNow,
                    AppliedBy = "SYSTEM_SO_CANCELLATION"
                });

                target.AchievedValue = Math.Max(0m, target.AchievedValue - original.RealizedAmount);
                SalesTargetRules.RecalculateProgress(target);

                target.ProgressHistory.Add(new SalesTargetProgressHistory
                {
                    SalesTargetId = target.Id,
                    OldAchievedValue = target.AchievedValue + original.RealizedAmount,
                    NewAchievedValue = target.AchievedValue,
                    AchievementPercentage = target.AchievementPercentage,
                    Action = "SystemSync",
                    Remarks = $"Reversal from Cancelled Sales Order #{orderId}",
                    UpdatedBy = "SYSTEM",
                    UpdatedOn = DateTimeOffset.UtcNow
                });
            }

            await _context.SaveChangesAsync(ct);

            foreach (var original in existingRealizations)
            {
                if (original.Target.ParentTargetId.HasValue)
                {
                    await PropagateRollUpAsync(original.Target.ParentTargetId.Value, ct);
                }
            }

            if (order != null && order.SalesPersonUserId.HasValue && _recalcService != null)
            {
                await _recalcService.RecalculateRepPerformanceAsync(order.SalesPersonUserId.Value, DateTime.UtcNow.Year, ct);
            }
        }

        private async Task PropagateRollUpAsync(int parentTargetId, CancellationToken ct)
        {
            var parent = await _context.SalesTargets
                .Include(t => t.ChildTargets)
                .FirstOrDefaultAsync(t => t.Id == parentTargetId && !t.IsDeleted, ct);

            if (parent == null || !parent.IsAutoAggregated) return;

            var activeChildren = parent.ChildTargets
                .Where(c => !c.IsDeleted && c.Status != SalesTargetStatuses.Cancelled)
                .ToList();

            parent.TargetValue = activeChildren.Sum(c => c.TargetValue);
            parent.AchievedValue = activeChildren.Sum(c => c.AchievedValue);
            SalesTargetRules.RecalculateProgress(parent);
            parent.CalculatedCommissionAmount = SalesTargetAnalyticsService.CalculateCommission(parent.TargetValue, parent.AchievedValue);

            await _context.SaveChangesAsync(ct);

            if (parent.ParentTargetId.HasValue)
            {
                await PropagateRollUpAsync(parent.ParentTargetId.Value, ct);
            }
        }
    }
}
