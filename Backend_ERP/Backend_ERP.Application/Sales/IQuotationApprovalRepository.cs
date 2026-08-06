using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;

namespace ERP.Application.Sales
{
    public interface IQuotationApprovalRepository
    {
        IQueryable<QuotationApproval> Query();
        Task<IReadOnlyList<QuotationApproval>> GetAllAsync(QuotationApprovalListQueryDto? query, CancellationToken cancellationToken = default);
        Task<QuotationApproval?> GetByIdAsync(int id, bool includeDetails, bool asTracking, CancellationToken cancellationToken = default);
        Task<QuotationApproval> CreateAsync(QuotationApproval entity, CancellationToken cancellationToken = default);
        Task<QuotationApproval?> UpdateAsync(QuotationApproval entity, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(QuotationApproval entity, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<QuotationApprovalHistory>?> GetHistoryAsync(int quotationApprovalId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<QuotationApprovalComment>?> GetCommentsAsync(int quotationApprovalId, CancellationToken cancellationToken = default);
        Task<QuotationApprovalComment?> AddCommentAsync(QuotationApproval entity, QuotationApprovalComment comment, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<QuotationApproval>> GetForStatisticsAsync(CancellationToken cancellationToken = default);
        Task<SalesOrder?> FindSalesOrderAsync(int salesOrderId, CancellationToken cancellationToken = default);
    }
}
