using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Accounting.Dtos;

namespace ERP.Application.Accounting
{
    public interface IReceiptService
    {
        Task<IReadOnlyList<ReceiptListItemDto>> GetReceiptsAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default);
        Task<ReceiptEntryDto?> GetReceiptByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<ReceiptEntryDto> CreateReceiptAsync(ReceiptCreateRequestDto dto, string user, CancellationToken cancellationToken = default);
        Task<ReceiptEntryDto> UpdateReceiptAsync(int id, ReceiptUpdateRequestDto dto, string user, CancellationToken cancellationToken = default);
        Task<bool> DeleteReceiptAsync(int id, CancellationToken cancellationToken = default);
        Task<ReceiptEntryDto> ApproveReceiptAsync(int id, StatusActionRequestDto? payload, string user, CancellationToken cancellationToken = default);
        Task<ReceiptEntryDto> PostReceiptAsync(int id, StatusActionRequestDto? payload, string user, CancellationToken cancellationToken = default);
        Task<ReceiptEntryDto> RecordReceivedAsync(int id, StatusActionRequestDto? payload, string user, CancellationToken cancellationToken = default);
        Task<ReceiptEntryDto> CancelReceiptAsync(int id, StatusActionRequestDto? payload, string user, CancellationToken cancellationToken = default);
        Task<ReceiptEntryDto> DuplicateReceiptAsync(int id, string user, CancellationToken cancellationToken = default);
        Task<ReceiptDashboardDto> GetReceiptDashboardAsync(CancellationToken cancellationToken = default);
    }
}
