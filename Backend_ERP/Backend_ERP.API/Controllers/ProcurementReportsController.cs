using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Route("api/procurement-reports")]
    [ApiController]
    public class ProcurementReportsController : ControllerBase
    {
        private readonly IProcurementReportsService _reportsService;

        public ProcurementReportsController(IProcurementReportsService reportsService)
        {
            _reportsService = reportsService;
        }

        [HttpGet("kpis")]
        public async Task<ActionResult<ProcurementReportKpisDto>> GetKpis(
            [FromQuery] string? search,
            [FromQuery] string? vendorName,
            [FromQuery] string? status,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            [FromQuery] string? purchaseOrderNumber,
            [FromQuery] string? goodsReceiptNumber,
            [FromQuery] string? createdBy,
            [FromQuery] string? approvalStatus,
            [FromQuery] int? userId,
            CancellationToken cancellationToken = default)
        {
            _ = userId;
            var query = BuildQuery(search, vendorName, status, dateFrom, dateTo, purchaseOrderNumber, goodsReceiptNumber, createdBy, approvalStatus);
            return Ok(await _reportsService.GetKpisAsync(query, cancellationToken));
        }

        [HttpGet("charts")]
        public async Task<ActionResult<ProcurementChartsDto>> GetCharts(
            [FromQuery] string? search,
            [FromQuery] string? vendorName,
            [FromQuery] string? status,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            [FromQuery] string? purchaseOrderNumber,
            [FromQuery] string? goodsReceiptNumber,
            [FromQuery] string? createdBy,
            [FromQuery] string? approvalStatus,
            [FromQuery] int? userId,
            CancellationToken cancellationToken = default)
        {
            _ = userId;
            var query = BuildQuery(search, vendorName, status, dateFrom, dateTo, purchaseOrderNumber, goodsReceiptNumber, createdBy, approvalStatus);
            return Ok(await _reportsService.GetChartsAsync(query, cancellationToken));
        }

        [HttpGet("purchase-orders")]
        public async Task<ActionResult<List<PurchaseOrderReportRowDto>>> GetPurchaseOrderReport(
            [FromQuery] string? search,
            [FromQuery] string? vendorName,
            [FromQuery] string? status,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            [FromQuery] string? purchaseOrderNumber,
            [FromQuery] string? createdBy,
            [FromQuery] string? approvalStatus,
            [FromQuery] int? userId,
            CancellationToken cancellationToken = default)
        {
            _ = userId;
            var query = BuildQuery(search, vendorName, status, dateFrom, dateTo, purchaseOrderNumber, null, createdBy, approvalStatus);
            return Ok(await _reportsService.GetPurchaseOrderReportAsync(query, cancellationToken));
        }

        [HttpGet("goods-receipts")]
        public async Task<ActionResult<List<GoodsReceiptReportRowDto>>> GetGoodsReceiptReport(
            [FromQuery] string? search,
            [FromQuery] string? vendorName,
            [FromQuery] string? status,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            [FromQuery] string? goodsReceiptNumber,
            [FromQuery] string? createdBy,
            [FromQuery] int? userId,
            CancellationToken cancellationToken = default)
        {
            _ = userId;
            var query = BuildQuery(search, vendorName, status, dateFrom, dateTo, null, goodsReceiptNumber, createdBy, null);
            return Ok(await _reportsService.GetGoodsReceiptReportAsync(query, cancellationToken));
        }

        [HttpGet("vendors")]
        public async Task<ActionResult<List<VendorPurchaseSummaryRowDto>>> GetVendorSummary(
            [FromQuery] string? search,
            [FromQuery] string? vendorName,
            [FromQuery] string? status,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            [FromQuery] int? userId,
            CancellationToken cancellationToken = default)
        {
            _ = userId;
            var query = BuildQuery(search, vendorName, status, dateFrom, dateTo, null, null, null, null);
            return Ok(await _reportsService.GetVendorSummaryAsync(query, cancellationToken));
        }

        [HttpGet("status-summary")]
        public async Task<ActionResult<List<StatusSummaryRowDto>>> GetStatusSummary(
            [FromQuery] string? search,
            [FromQuery] string? vendorName,
            [FromQuery] string? status,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            [FromQuery] int? userId,
            CancellationToken cancellationToken = default)
        {
            _ = userId;
            var query = BuildQuery(search, vendorName, status, dateFrom, dateTo, null, null, null, null);
            return Ok(await _reportsService.GetStatusSummaryAsync(query, cancellationToken));
        }

        [HttpGet("monthly-trend")]
        public async Task<ActionResult<List<MonthlyTrendRowDto>>> GetMonthlyTrend(
            [FromQuery] string? search,
            [FromQuery] string? vendorName,
            [FromQuery] string? status,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            [FromQuery] int? userId,
            CancellationToken cancellationToken = default)
        {
            _ = userId;
            var query = BuildQuery(search, vendorName, status, dateFrom, dateTo, null, null, null, null);
            return Ok(await _reportsService.GetMonthlyTrendAsync(query, cancellationToken));
        }

        [HttpGet("top-vendors")]
        public async Task<ActionResult<List<TopVendorRowDto>>> GetTopVendors(
            [FromQuery] string? search,
            [FromQuery] string? vendorName,
            [FromQuery] string? status,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            [FromQuery] int? limit,
            [FromQuery] int? userId,
            CancellationToken cancellationToken = default)
        {
            _ = userId;
            var query = BuildQuery(search, vendorName, status, dateFrom, dateTo, null, null, null, null);
            return Ok(await _reportsService.GetTopVendorsAsync(query, limit, cancellationToken));
        }

        private static ProcurementReportFilterQueryDto BuildQuery(
            string? search, string? vendorName, string? status, string? dateFrom, string? dateTo,
            string? purchaseOrderNumber, string? goodsReceiptNumber, string? createdBy, string? approvalStatus)
        {
            return new ProcurementReportFilterQueryDto
            {
                Search = search,
                VendorName = vendorName,
                Status = status,
                DateFrom = dateFrom,
                DateTo = dateTo,
                PurchaseOrderNumber = purchaseOrderNumber,
                GoodsReceiptNumber = goodsReceiptNumber,
                CreatedBy = createdBy,
                ApprovalStatus = approvalStatus
            };
        }
    }
}
