using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ERP.Infrastructure.Sales
{
    public class ProformaInvoiceExpiryWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ProformaInvoiceExpiryWorker> _logger;

        public ProformaInvoiceExpiryWorker(IServiceScopeFactory scopeFactory, ILogger<ProformaInvoiceExpiryWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessExpiriesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing ProformaInvoiceExpiryWorker.");
                }

                // Run once every 6 hours
                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }
        }

        public async Task<int> ProcessExpiriesAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ERPDbContext>();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var expiredPis = await db.ProformaInvoices
                .Where(x => !x.IsDeleted && x.Status == ProformaInvoiceStatuses.Sent && x.ValidUntil < today && x.AdvanceReceivedAmount == 0)
                .ToListAsync(cancellationToken);

            foreach (var pi in expiredPis)
            {
                pi.Status = ProformaInvoiceStatuses.Expired;
                pi.UpdatedDate = DateTimeOffset.UtcNow;
                pi.UpdatedBy = "SYSTEM_EXPIRY_WORKER";

                db.ProformaInvoiceStatusHistories.Add(new ProformaInvoiceStatusHistory
                {
                    ProformaInvoiceId = pi.Id,
                    EntryKey = Guid.NewGuid().ToString("N"),
                    OldStatus = ProformaInvoiceStatuses.Sent,
                    NewStatus = ProformaInvoiceStatuses.Expired,
                    ChangedBy = "SYSTEM_EXPIRY_WORKER",
                    ChangedOn = DateTimeOffset.UtcNow,
                    Remarks = "Automatically expired past validity date with zero advance payments."
                });
            }

            if (expiredPis.Count > 0)
            {
                await db.SaveChangesAsync(cancellationToken);
            }

            return expiredPis.Count;
        }
    }
}
