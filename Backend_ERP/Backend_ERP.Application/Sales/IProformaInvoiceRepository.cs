using ERP.Domain.Sales;

namespace ERP.Application.Sales
{
    public interface IProformaInvoiceRepository
    {
        Task<bool> AnyAsync(CancellationToken cancellationToken = default);

        IQueryable<ProformaInvoice> Query();

        Task<ProformaInvoice?> GetByIdAsync(
            int id,
            bool includeDetails,
            bool asTracking,
            CancellationToken cancellationToken = default);

        Task AddAsync(ProformaInvoice entity, CancellationToken cancellationToken = default);

        void Remove(ProformaInvoice entity);

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        Task<string> ReserveNextPiNumberAsync(CancellationToken cancellationToken = default);

        Task<bool> PiNumberExistsAsync(string piNumber, int? excludeId, CancellationToken cancellationToken = default);

        Task<SalesOrder?> FindSalesOrderAsync(int salesOrderId, CancellationToken cancellationToken = default);

        Task<SalesOrder?> FindSalesOrderWithItemsAsync(int salesOrderId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SalesOrder>> ListSalesOrdersForLookupAsync(CancellationToken cancellationToken = default);
    }
}
