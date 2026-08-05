using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;

namespace ERP.Application.Sales
{
    public interface IDiscountApprovalRepository
    {
        Task<IReadOnlyList<DiscountApproval>> GetAllAsync(
            DiscountApprovalListQueryDto? query,
            CancellationToken cancellationToken = default);

        Task<DiscountApproval?> GetByIdAsync(
            int id,
            bool includeDetails,
            bool asTracking,
            CancellationToken cancellationToken = default);

        Task<DiscountApproval> CreateAsync(
            DiscountApproval entity,
            CancellationToken cancellationToken = default);

        Task<DiscountApproval?> UpdateAsync(
            DiscountApproval entity,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(
            DiscountApproval entity,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<DiscountApprovalHistory>?> GetHistoryAsync(
            int discountApprovalId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<DiscountApprovalComment>?> GetCommentsAsync(
            int discountApprovalId,
            CancellationToken cancellationToken = default);

        Task<DiscountApprovalComment?> AddCommentAsync(
            DiscountApproval entity,
            DiscountApprovalComment comment,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<DiscountApproval>> GetForStatisticsAsync(
            CancellationToken cancellationToken = default);

        Task<SalesOrder?> FindSalesOrderAsync(
            int salesOrderId,
            CancellationToken cancellationToken = default);

        Task<PriceList?> FindPriceListAsync(
            int priceListId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SalesOrder>> ListSalesOrdersForLookupAsync(
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<PriceList>> ListPriceListsForLookupAsync(
            CancellationToken cancellationToken = default);

        IQueryable<DiscountApproval> Query();
    }
}
