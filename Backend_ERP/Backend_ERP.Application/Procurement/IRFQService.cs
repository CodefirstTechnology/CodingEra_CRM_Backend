using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement.Dtos;
using ERP.Shared.Models;

namespace ERP.Application.Procurement
{
    public interface IRFQService
    {
        Task<PagedResult<RFQListItemDto>> GetAllAsync(RFQListQueryDto query, CancellationToken cancellationToken = default);

        Task<RFQDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<RFQDto> CreateAsync(RFQCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default);

        Task<RFQDto?> CreateFromPRAsync(int purchaseRequisitionId, string actingUser, CancellationToken cancellationToken = default);

        Task<RFQDto?> UpdateAsync(int id, RFQUpdateRequestDto request, string actingUser, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, string actingUser, CancellationToken cancellationToken = default);

        Task<RFQDto?> SendAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default);

        Task<RFQDto?> CloseAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default);

        Task<RFQDto?> CancelAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default);

        Task<string> GetNextRFQNumberAsync(CancellationToken cancellationToken = default);
    }
}
