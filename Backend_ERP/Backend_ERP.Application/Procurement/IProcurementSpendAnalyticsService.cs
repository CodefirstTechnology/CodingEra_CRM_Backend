using System;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement.Dtos;

namespace ERP.Application.Procurement
{
    public interface IProcurementSpendAnalyticsService
    {
        Task<PurchasePriceVarianceReportDto> CalculatePpvAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);
        Task<SpendParetoMatrixDto> GetSpendParetoMatrixAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);
        Task<SupplierOtifScorecardDto> CalculateSupplierOtifAsync(int? vendorId = null, DateTime? fromDate = null, CancellationToken ct = default);
        Task<ProcurementKpiDashboardDto> GetExecutiveKpisAsync(CancellationToken ct = default);
        Task InvalidateAnalyticsCacheAsync(CancellationToken ct = default);
    }
}

