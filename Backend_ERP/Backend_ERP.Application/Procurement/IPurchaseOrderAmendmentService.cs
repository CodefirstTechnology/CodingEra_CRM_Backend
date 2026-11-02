using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;

namespace ERP.Application.Procurement
{
    public interface IPurchaseOrderAmendmentService
    {
        Task<PurchaseOrder> AmendPurchaseOrderAsync(int predecessorPoId, POAmendmentRequestDto request, string actingUser, CancellationToken cancellationToken = default);

        Task<List<PurchaseOrderRevisionSummaryDto>> GetRevisionHistoryAsync(int poId, CancellationToken cancellationToken = default);

        Task<List<PurchaseOrderAmendmentDeltaDto>> GetAmendmentAuditTrailAsync(int poId, CancellationToken cancellationToken = default);
    }
}

