using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ERP.Infrastructure.Sales.Workers
{
    public class SalesTargetEvaluationWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public SalesTargetEvaluationWorker(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ERPDbContext>();
                    var today = DateOnly.FromDateTime(DateTime.UtcNow);

                    var pastTargets = await db.SalesTargets
                        .Include(t => t.StatusHistory)
                        .Where(t => !t.IsDeleted && t.Status == SalesTargetStatuses.Active && t.EndDate < today)
                        .ToListAsync(ct);

                    foreach (var target in pastTargets)
                    {
                        target.Status = SalesTargetStatuses.Closed;
                        target.IsLocked = true;
                        target.UpdatedDate = DateTimeOffset.UtcNow;
                        target.UpdatedBy = "TARGET_EVALUATION_WORKER";

                        target.StatusHistory.Add(new SalesTargetStatusHistory
                        {
                            SalesTargetId = target.Id,
                            OldStatus = SalesTargetStatuses.Active,
                            NewStatus = SalesTargetStatuses.Closed,
                            Remarks = "Closed automatically as target period ended.",
                            ChangedBy = "SYSTEM",
                            ChangedOn = DateTimeOffset.UtcNow
                        });
                    }

                    if (pastTargets.Count > 0)
                    {
                        await db.SaveChangesAsync(ct);
                    }
                }
                catch when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch
                {
                    // Swallowing transient errors in worker to prevent crash
                }

                await Task.Delay(TimeSpan.FromHours(12), ct);
            }
        }
    }
}
