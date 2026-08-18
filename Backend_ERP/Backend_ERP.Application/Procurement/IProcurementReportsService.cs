using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement.Dtos;

namespace ERP.Application.Procurement
{
    public interface IProcurementReportsService
    {
        Task<ProcurementReportKpisDto> GetKpisAsync(ProcurementReportFilterQueryDto query, CancellationToken cancellationToken = default);
        Task<ProcurementChartsDto> GetChartsAsync(ProcurementReportFilterQueryDto query, CancellationToken cancellationToken = default);
        Task<List<PurchaseOrderReportRowDto>> GetPurchaseOrderReportAsync(ProcurementReportFilterQueryDto query, CancellationToken cancellationToken = default);
        Task<List<GoodsReceiptReportRowDto>> GetGoodsReceiptReportAsync(ProcurementReportFilterQueryDto query, CancellationToken cancellationToken = default);
        Task<List<VendorPurchaseSummaryRowDto>> GetVendorSummaryAsync(ProcurementReportFilterQueryDto query, CancellationToken cancellationToken = default);
        Task<List<StatusSummaryRowDto>> GetStatusSummaryAsync(ProcurementReportFilterQueryDto query, CancellationToken cancellationToken = default);
        Task<List<MonthlyTrendRowDto>> GetMonthlyTrendAsync(ProcurementReportFilterQueryDto query, CancellationToken cancellationToken = default);
        Task<List<TopVendorRowDto>> GetTopVendorsAsync(ProcurementReportFilterQueryDto query, int? limit, CancellationToken cancellationToken = default);
    }
}
