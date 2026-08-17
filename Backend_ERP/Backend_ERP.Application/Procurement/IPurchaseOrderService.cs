using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement.Dtos;
using ERP.Shared.Models;

namespace ERP.Application.Procurement
{
    public interface IPurchaseOrderService
    {
        Task<PagedResult<PurchaseOrderListItemDto>> GetAllAsync(PurchaseOrderListQueryDto query, CancellationToken cancellationToken = default);

        Task<PurchaseOrderDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<PurchaseOrderDto> CreateAsync(PurchaseOrderCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default);

        Task<PurchaseOrderDto?> UpdateAsync(int id, PurchaseOrderUpdateRequestDto request, string actingUser, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, string actingUser, CancellationToken cancellationToken = default);

        Task<PurchaseOrderDto?> UpdateStatusAsync(int id, PurchaseOrderStatusUpdateRequestDto request, string actingUser, CancellationToken cancellationToken = default);

        Task<PurchaseOrderDto?> CancelAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default);

        Task<List<PurchaseOrderHistoryDto>> GetStatusHistoryAsync(int id, CancellationToken cancellationToken = default);

        Task<List<PurchaseOrderApprovalQueueItemDto>> GetApprovalQueueAsync(PurchaseOrderListQueryDto query, CancellationToken cancellationToken = default);

        Task<PurchaseOrderApprovalMetricsDto> GetApprovalMetricsAsync(PurchaseOrderListQueryDto query, CancellationToken cancellationToken = default);

        Task<PurchaseOrderDto?> ApproveAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default);

        Task<PurchaseOrderDto?> RejectAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default);

        Task<PurchaseOrderDto?> RequestRevisionAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default);

        Task<PurchaseOrderDto?> ReopenAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default);

        Task<List<PurchaseOrderApprovalHistoryDto>> GetApprovalHistoryAsync(int id, CancellationToken cancellationToken = default);

        Task<string> GetNextNumberAsync(CancellationToken cancellationToken = default);
    }
}
