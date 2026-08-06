using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales
{
    public interface IQuotationApprovalService
    {
        Task<QuotationApprovalDto> CreateAsync(QuotationApprovalCreateRequestDto request, string currentUser, CancellationToken cancellationToken = default);
        Task<QuotationApprovalDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<QuotationApprovalDto> UpdateAsync(int id, QuotationApprovalUpdateRequestDto request, string currentUser, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, string currentUser, CancellationToken cancellationToken = default);
        Task<QuotationApprovalDto> SubmitAsync(int id, string currentUser, CancellationToken cancellationToken = default);
        Task<QuotationApprovalDto> ReviewAsync(int id, QuotationApprovalDecisionRequestDto request, string currentUser, CancellationToken cancellationToken = default);
        Task<QuotationApprovalDto> ApproveAsync(int id, QuotationApprovalDecisionRequestDto request, string currentUser, CancellationToken cancellationToken = default);
        Task<QuotationApprovalDto> RejectAsync(int id, QuotationApprovalDecisionRequestDto request, string currentUser, CancellationToken cancellationToken = default);
        Task<QuotationApprovalDto> ReturnAsync(int id, QuotationApprovalDecisionRequestDto request, string currentUser, CancellationToken cancellationToken = default);
        Task<QuotationApprovalDto> CancelAsync(int id, QuotationApprovalDecisionRequestDto request, string currentUser, CancellationToken cancellationToken = default);
        Task<QuotationApprovalDto> RequestRevisionAsync(int id, QuotationApprovalDecisionRequestDto request, string currentUser, CancellationToken cancellationToken = default);
        Task<QuotationApprovalDto> ReopenAsync(int id, QuotationApprovalDecisionRequestDto request, string currentUser, CancellationToken cancellationToken = default);
        Task<QuotationApprovalCommentDto> AddCommentAsync(int id, QuotationApprovalCommentRequestDto request, string currentUser, CancellationToken cancellationToken = default);
        Task<QuotationApprovalStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default);
    }
}
