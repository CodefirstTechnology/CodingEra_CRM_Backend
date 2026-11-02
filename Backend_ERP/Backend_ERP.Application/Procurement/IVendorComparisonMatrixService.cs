using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement.Dtos;

namespace ERP.Application.Procurement
{
    public interface IVendorComparisonMatrixService
    {
        Task<VendorComparisonDto> EvaluateRfqMatrixAsync(int rfqId, string actingUser, int tenantId = 1, CancellationToken cancellationToken = default);

        Task<VendorComparisonDto> AwardSingleAsync(int comparisonId, VendorAwardRequestDto request, string actingUser, int tenantId = 1, CancellationToken cancellationToken = default);

        Task<VendorComparisonDto> AwardSplitAsync(int comparisonId, VendorSplitAwardRequestDto request, string actingUser, int tenantId = 1, CancellationToken cancellationToken = default);

        Task<List<int>> GeneratePurchaseOrdersAsync(int comparisonId, string actingUser, int tenantId = 1, CancellationToken cancellationToken = default);
    }
}

