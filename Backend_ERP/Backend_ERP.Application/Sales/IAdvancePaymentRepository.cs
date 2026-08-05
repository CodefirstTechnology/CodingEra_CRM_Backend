using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;

namespace ERP.Application.Sales
{
    public interface IAdvancePaymentRepository
    {
        Task<IReadOnlyList<AdvancePayment>> GetAllAsync(
            AdvancePaymentListQueryDto? query,
            CancellationToken cancellationToken = default);

        Task<AdvancePayment?> GetByIdAsync(
            int id,
            bool includeDetails,
            bool asTracking,
            CancellationToken cancellationToken = default);

        Task<AdvancePayment> CreateAsync(
            AdvancePayment entity,
            CancellationToken cancellationToken = default);

        Task<AdvancePayment?> UpdateAsync(
            AdvancePayment entity,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(
            AdvancePayment entity,
            CancellationToken cancellationToken = default);

        Task<AdvancePayment?> ApplyAsync(
            AdvancePayment entity,
            AdvancePaymentApplication application,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AdvancePaymentTimeline>?> GetTimelineAsync(
            int advancePaymentId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AdvancePayment>> GetLedgerAsync(
            AdvancePaymentListQueryDto? query,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AdvancePayment>> GetDashboardAsync(
            CancellationToken cancellationToken = default);

        Task<bool> PaymentNumberExistsAsync(
            string paymentNumber,
            int? excludeId,
            CancellationToken cancellationToken = default);

        Task<bool> ApplicationExistsAsync(
            int advancePaymentId,
            int salesOrderId,
            CancellationToken cancellationToken = default);

        Task<SalesOrder?> FindSalesOrderAsync(
            int salesOrderId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SalesOrder>> ListSalesOrdersForLookupAsync(
            CancellationToken cancellationToken = default);

        IQueryable<AdvancePayment> Query();
    }
}
