using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Accounting.Dtos;

namespace ERP.Application.Accounting
{
    public interface IGstService
    {
        Task<IReadOnlyList<GstTransactionListItemDto>> GetGstTransactionsAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default);
        Task<GstTransactionDto?> GetGstTransactionByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<GstTransactionDto> CreateGstTransactionAsync(GstTransactionDto dto, string user, CancellationToken cancellationToken = default);
        Task<GstTransactionDto> VerifyGstTransactionAsync(int id, StatusActionRequestDto? payload, string user, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<GstReturnDto>> GetGstReturnsAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default);
        Task<GstReturnDto> VerifyGstReturnAsync(int id, StatusActionRequestDto? payload, string user, CancellationToken cancellationToken = default);
        Task<GstReturnDto> FileGstReturnAsync(int id, StatusActionRequestDto? payload, string user, CancellationToken cancellationToken = default);
        Task<GstReturnDto> CloseGstReturnAsync(int id, StatusActionRequestDto? payload, string user, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<GstSummaryDto>> GetGstSummaryAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default);
        Task<GstDashboardDto> GetGstDashboardAsync(CancellationToken cancellationToken = default);
    }
}
