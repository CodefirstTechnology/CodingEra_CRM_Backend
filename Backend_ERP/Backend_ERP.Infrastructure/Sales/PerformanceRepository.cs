using ERP.Application.Sales;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;

namespace ERP.Infrastructure.Sales
{
    public class PerformanceRepository : IPerformanceRepository
    {
        private readonly ERPDbContext _db;

        public PerformanceRepository(ERPDbContext db)
        {
            _db = db;
        }

        public IQueryable<SalesOrder> SalesOrders() => _db.SalesOrders;
        public IQueryable<ProformaInvoice> ProformaInvoices() => _db.ProformaInvoices.Where(x => !x.IsDeleted);
        public IQueryable<AdvancePayment> AdvancePayments() => _db.AdvancePayments.Where(x => !x.IsDeleted);
        public IQueryable<SalesTarget> SalesTargets() => _db.SalesTargets.Where(x => !x.IsDeleted);
        public IQueryable<PerformanceSnapshot> Snapshots() => _db.PerformanceSnapshots;
        public IQueryable<PerformanceHistory> History() => _db.PerformanceHistory;
        public IQueryable<PerformanceExportHistory> ExportHistory() => _db.PerformanceExportHistory;

        public async Task AddExportAsync(
            PerformanceExportHistory export,
            CancellationToken cancellationToken = default)
        {
            await _db.PerformanceExportHistory.AddAsync(export, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
