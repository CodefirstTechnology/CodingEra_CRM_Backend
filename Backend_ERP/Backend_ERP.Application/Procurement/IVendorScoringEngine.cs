using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement.Dtos;

namespace ERP.Application.Procurement
{
    public interface IVendorScoringEngine
    {
        Task<VendorPerformanceScoreDto> IngestLotInspectionAndScoreAsync(
            int vendorId,
            VendorLotInspectionIngestionDto dto,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<VendorPerformanceSummaryDto?> GetPerformanceSummaryAsync(
            int vendorId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<VendorPerformanceScoreDto>> GetPerformanceHistoryAsync(
            int vendorId,
            int? limitMonths = 12,
            CancellationToken cancellationToken = default);
    }
}

