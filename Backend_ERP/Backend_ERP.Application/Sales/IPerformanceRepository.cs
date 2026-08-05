using ERP.Domain.Sales;

namespace ERP.Application.Sales
{
    public interface IPerformanceRepository
    {
        IQueryable<SalesOrder> SalesOrders();
        IQueryable<ProformaInvoice> ProformaInvoices();
        IQueryable<AdvancePayment> AdvancePayments();
        IQueryable<SalesTarget> SalesTargets();
        IQueryable<PerformanceSnapshot> Snapshots();
        IQueryable<PerformanceHistory> History();
        IQueryable<PerformanceExportHistory> ExportHistory();
        Task AddExportAsync(PerformanceExportHistory export, CancellationToken cancellationToken = default);
    }
}
