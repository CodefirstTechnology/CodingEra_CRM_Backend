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

        public SalesOrderCancelledEventHandler(ERPDbContext context)
        {
            _context = context;
        }

        public async Task Handle(SalesOrderCancelledDomainEvent notification, CancellationToken ct)
        {
            var orderId = notification.SalesOrderId;

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
        }
    }
}
