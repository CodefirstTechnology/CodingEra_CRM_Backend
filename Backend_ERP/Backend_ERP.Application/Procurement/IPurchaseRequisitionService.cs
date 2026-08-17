using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement.Dtos;
using ERP.Shared.Models;

namespace ERP.Application.Procurement
{
    public interface IPurchaseRequisitionService
    {
        Task<PagedResult<PurchaseRequisitionListItemDto>> GetAllAsync(PurchaseRequisitionListQueryDto query, CancellationToken cancellationToken = default);

        Task<PurchaseRequisitionDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<PurchaseRequisitionDto> CreateAsync(PurchaseRequisitionCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default);

        Task<PurchaseRequisitionDto?> UpdateAsync(int id, PurchaseRequisitionUpdateRequestDto request, string actingUser, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, string actingUser, CancellationToken cancellationToken = default);

        Task<PurchaseRequisitionDto?> SubmitAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default);

        Task<PurchaseRequisitionDto?> ApproveAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default);

        Task<PurchaseRequisitionDto?> RejectAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default);

        Task<string> GetNextPRNumberAsync(CancellationToken cancellationToken = default);
    }
}
