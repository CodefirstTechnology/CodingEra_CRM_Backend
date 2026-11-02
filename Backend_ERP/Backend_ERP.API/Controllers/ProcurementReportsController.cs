using System;
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
        private readonly IProcurementWorkflowSlaEngine _workflowSlaEngine;
        private readonly IAccountsPayableAgingService _agingService;

        public ProcurementReportsController(
            IProcurementReportsService reportsService,
            IProcurementWorkflowSlaEngine workflowSlaEngine,
            IAccountsPayableAgingService agingService)
        {
            _reportsService = reportsService;
            _workflowSlaEngine = workflowSlaEngine;
            _agingService = agingService;
        }

        [HttpGet("workflow-velocity")]
        public async Task<ActionResult<WorkflowVelocityReportDto>> GetWorkflowVelocity(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            CancellationToken cancellationToken = default)
        {
            var start = fromDate ?? DateTime.UtcNow.AddMonths(-1);
            var end = toDate ?? DateTime.UtcNow;
            var report = await _workflowSlaEngine.GetVelocityReportAsync(start, end, cancellationToken);
            return Ok(report);
        }

        [HttpGet("department-bottlenecks")]
        public async Task<ActionResult<List<DepartmentBottleneckScorecardDto>>> GetDepartmentBottlenecks(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            CancellationToken cancellationToken = default)
        {
            var result = await _workflowSlaEngine.GetDepartmentBottlenecksAsync(fromDate, toDate, cancellationToken);
            return Ok(result);
        }

        [HttpGet("msme-compliance")]
        public async Task<ActionResult<List<MsmeComplianceAlertDto>>> GetMsmeCompliance(CancellationToken cancellationToken = default)
        {
            var alerts = await _agingService.GetMsmeSection43BAlertsAsync(cancellationToken);
            return Ok(alerts);
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

        [HttpGet("vendor-performance")]
        public async Task<ActionResult<VendorPerformanceDashboardDto>> GetVendorPerformance(
            [FromQuery] string? search,
            [FromQuery] string? vendorName,
            [FromQuery] string? status,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            CancellationToken cancellationToken = default)
        {
            var query = BuildQuery(search, vendorName, status, dateFrom, dateTo, null, null, null, null);
            return Ok(await _reportsService.GetVendorPerformanceAsync(query, cancellationToken));
        }

        [HttpGet("export/csv")]
        public IActionResult ExportCsv(
            [FromQuery] string? search,
            [FromQuery] string? vendorName,
            [FromQuery] string? status,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            CancellationToken cancellationToken = default)
        {
            var query = BuildQuery(search, vendorName, status, dateFrom, dateTo, null, null, null, null);
            Response.Headers.Append("Content-Disposition", "attachment; filename=\"procurement_report.csv\"");
            return Ok(_reportsService.StreamPurchaseOrdersCsvAsync(query, cancellationToken));
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
