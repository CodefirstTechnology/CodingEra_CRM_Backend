using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Route("api/vendors/reports")]
    [ApiController]
    public class VendorReportsController : ControllerBase
    {
        private readonly IVendorReportService _reportService;

        public VendorReportsController(IVendorReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpPost]
        public async Task<ActionResult<VendorReportResultDto>> GenerateReport(
            [FromBody] VendorReportFilterFrontendRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var result = await _reportService.GetVendorReportResultAsync(request, cancellationToken);
            return Ok(result);
        }

        [HttpGet]
        public async Task<ActionResult<VendorReportResultDto>> GetReportGet(
            [FromQuery] string? reportType,
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? country,
            [FromQuery] string? state,
            [FromQuery] string? vendorType,
            [FromQuery] string? tag,
            CancellationToken cancellationToken = default)
        {
            var request = new VendorReportFilterFrontendRequestDto
            {
                ReportType = reportType,
                Search = search,
                Status = status,
                Country = country,
                State = state,
                VendorType = vendorType,
                Tag = tag
            };
            var result = await _reportService.GetVendorReportResultAsync(request, cancellationToken);
            return Ok(result);
        }

        [HttpGet("lead-time")]
        public async Task<ActionResult<IReadOnlyList<LeadTimeVarianceReportRowDto>>> GetLeadTimeVariance(
            [FromQuery] int? vendorId,
            [FromQuery] string? itemCategory,
            [FromQuery] DateOnly? fromDate,
            [FromQuery] DateOnly? toDate,
            CancellationToken cancellationToken = default)
        {
            var filter = new VendorReportFilterDto
            {
                VendorId = vendorId,
                ItemCategory = itemCategory,
                FromDate = fromDate,
                ToDate = toDate
            };

            var rows = await _reportService.GetLeadTimeVarianceReportAsync(filter, cancellationToken);
            return Ok(rows);
        }

        [HttpGet("concentration-hhi")]
        public async Task<ActionResult<CategorySpendConcentrationReportDto>> GetSpendConcentration(
            [FromQuery] int? vendorId,
            [FromQuery] string? itemCategory,
            CancellationToken cancellationToken = default)
        {
            var filter = new VendorReportFilterDto
            {
                VendorId = vendorId,
                ItemCategory = itemCategory
            };

            var report = await _reportService.GetCategorySpendConcentrationReportAsync(filter, cancellationToken);
            return Ok(report);
        }

        [HttpGet("msme-compliance")]
        public async Task<ActionResult<IReadOnlyList<MsmePaymentDueAuditRowDto>>> GetMsmeCompliance(
            [FromQuery] int? vendorId,
            CancellationToken cancellationToken = default)
        {
            var filter = new VendorReportFilterDto { VendorId = vendorId };
            var rows = await _reportService.GetMsmePaymentDueAuditReportAsync(filter, cancellationToken);
            return Ok(rows);
        }

        [HttpGet("qc-pareto")]
        public async Task<ActionResult<QcDefectParetoReportDto>> GetQcDefectPareto(
            [FromQuery] int? vendorId,
            [FromQuery] string? itemCategory,
            CancellationToken cancellationToken = default)
        {
            var filter = new VendorReportFilterDto
            {
                VendorId = vendorId,
                ItemCategory = itemCategory
            };

            var report = await _reportService.GetQcDefectParetoReportAsync(filter, cancellationToken);
            return Ok(report);
        }

        [HttpGet("export-csv")]
        public async Task<IActionResult> ExportCsv(
            [FromQuery] string reportType,
            [FromQuery] int? vendorId,
            [FromQuery] string? itemCategory,
            [FromQuery] DateOnly? fromDate,
            [FromQuery] DateOnly? toDate,
            CancellationToken cancellationToken = default)
        {
            var filter = new VendorReportFilterDto
            {
                VendorId = vendorId,
                ItemCategory = itemCategory,
                FromDate = fromDate,
                ToDate = toDate
            };

            var csvBytes = await _reportService.ExportReportCsvAsync(reportType, filter, cancellationToken);
            var fileName = $"vendor_{reportType.ToLowerInvariant()}_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

            return File(csvBytes, "text/csv", fileName);
        }
    }
}

