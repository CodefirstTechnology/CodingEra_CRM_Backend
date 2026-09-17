using ERP.Application.Common.Events;
using ERP.Application.Sales;
using ERP.Domain.Sales;
using ERP.Domain.Sales.Events;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales.Events
{
    public class SalesOrderConfirmedEventHandler : INotificationHandler<SalesOrderConfirmedDomainEvent>
    {
        private readonly ERPDbContext _context;

        public SalesOrderConfirmedEventHandler(ERPDbContext context)
        {
            _context = context;
        }

        public async Task Handle(SalesOrderConfirmedDomainEvent notification, CancellationToken ct)
        {
            var order = notification.SalesOrder;
            var orderDate = order.OrderDate != default
                ? order.OrderDate
                : DateOnly.FromDateTime(order.CreatedDate.UtcDateTime);

            var spUserId = order.SalesPersonUserId
                ?? (int.TryParse(order.SalesPerson, out var parsedId) ? parsedId : (int?)null);

            // Find active targets for the rep covering this date
            var query = _context.SalesTargets
                .Include(t => t.Realizations)
                .Include(t => t.ProgressHistory)
                .Where(t => !t.IsDeleted && t.Status == SalesTargetStatuses.Active)
                .Where(t => t.TargetCategory == SalesTargetCategories.Revenue)
                .Where(t => t.StartDate <= orderDate && t.EndDate >= orderDate);

            if (spUserId.HasValue)
            {
                query = query.Where(t => t.SalesPersonUserId == spUserId.Value);
            }
            else
            {
                query = query.Where(t => t.SalesTeam == order.SalesPerson);
            }

            var targets = await query.ToListAsync(ct);

            foreach (var target in targets)
            {
                // Prevent double realization
                bool alreadyApplied = target.Realizations.Any(r => r.SalesOrderId == order.Id && !r.IsReversal);
                if (alreadyApplied) continue;

                target.Realizations.Add(new SalesTargetRealization
                {
                    SalesTargetId = target.Id,
                    SalesOrderId = order.Id,
                    TransactionType = "SalesOrder",
                    RealizedAmount = order.GrandTotal,
                    IsReversal = false,
                    AppliedOn = DateTimeOffset.UtcNow,
                    AppliedBy = "SYSTEM_SO_CONFIRMATION"
                });

                target.AchievedValue += order.GrandTotal;
                SalesTargetRules.RecalculateProgress(target);

                target.ProgressHistory.Add(new SalesTargetProgressHistory
                {
                    SalesTargetId = target.Id,
                    OldAchievedValue = target.AchievedValue - order.GrandTotal,
                    NewAchievedValue = target.AchievedValue,
                    AchievementPercentage = target.AchievementPercentage,
                    Action = "SystemSync",
                    Remarks = $"Auto-realized from Confirmed Sales Order: {order.SalesOrderNumber}",
                    UpdatedBy = "SYSTEM",
                    UpdatedOn = DateTimeOffset.UtcNow
                });
            }

            await _context.SaveChangesAsync(ct);
        }
    }
}
