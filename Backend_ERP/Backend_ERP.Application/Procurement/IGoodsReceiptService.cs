using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement.Dtos;
using ERP.Shared.Models;

namespace ERP.Application.Procurement
{
    public interface IGoodsReceiptService
    {
        Task<PagedResult<GoodsReceiptListItemDto>> GetAllAsync(GoodsReceiptListQueryDto query, CancellationToken cancellationToken = default);

        Task<GoodsReceiptDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<PagedResult<GoodsReceiptListItemDto>> GetByPurchaseOrderAsync(int purchaseOrderId, CancellationToken cancellationToken = default);

        Task<GoodsReceiptDto> CreateAsync(GoodsReceiptCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default);

        Task<GoodsReceiptDto?> UpdateAsync(int id, GoodsReceiptUpdateRequestDto request, string actingUser, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, string actingUser, CancellationToken cancellationToken = default);

        Task<GoodsReceiptDto?> UpdateStatusAsync(int id, GoodsReceiptStatusUpdateRequestDto request, string actingUser, CancellationToken cancellationToken = default);

        Task<GoodsReceiptDto?> CancelAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default);

        Task<List<GoodsReceiptHistoryDto>> GetStatusHistoryAsync(int id, CancellationToken cancellationToken = default);

        Task<List<GoodsReceiptItemDto>> BuildDraftItemsForPurchaseOrderAsync(int purchaseOrderId, int? excludeGrnId = null, CancellationToken cancellationToken = default);

        Task<string> GetNextNumberAsync(CancellationToken cancellationToken = default);
    }
}
