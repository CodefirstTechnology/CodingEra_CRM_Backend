using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales
{
    public interface IDiscountApprovalService
    {
        Task<IReadOnlyList<DiscountApprovalListItemDto>> GetAllAsync(
            DiscountApprovalListQueryDto? query,
            CancellationToken cancellationToken = default);

        Task<DiscountApprovalDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<DiscountApprovalDto> CreateAsync(
            DiscountApprovalCreateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<DiscountApprovalDto?> UpdateAsync(
            int id,
            DiscountApprovalUpdateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, string actingUser, CancellationToken cancellationToken = default);

        Task<DiscountApprovalDto?> ApproveAsync(
            int id,
            DiscountApprovalDecisionRequestDto? request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<DiscountApprovalDto?> RejectAsync(
            int id,
            DiscountApprovalDecisionRequestDto? request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<DiscountApprovalDto?> ReturnAsync(
            int id,
            DiscountApprovalDecisionRequestDto? request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<DiscountApprovalDto?> CancelAsync(
            int id,
            DiscountApprovalDecisionRequestDto? request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<DiscountApprovalDto?> ResubmitAsync(
            int id,
            DiscountApprovalDecisionRequestDto? request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<DiscountApprovalStatisticsDto> GetStatisticsAsync(
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<DiscountApprovalHistoryDto>?> GetHistoryAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<DiscountApprovalCommentDto>?> GetCommentsAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<DiscountApprovalCommentDto?> AddCommentAsync(
            int id,
            DiscountApprovalCommentRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<string>> GetPermissionsAsync();

        Task<IReadOnlyList<DiscountApprovalLookupDto>> LookupPriceListsAsync(
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<DiscountApprovalLookupDto>> LookupSalesOrdersAsync(
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<DiscountApprovalLookupDto>> LookupQuotationsAsync(
            CancellationToken cancellationToken = default);
    }
}
