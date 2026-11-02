using System;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Route("api/procurement-analytics")]
    [ApiController]
    public class ProcurementAnalyticsController : ControllerBase
    {
        private readonly IProcurementSpendAnalyticsService _analyticsService;

        public ProcurementAnalyticsController(IProcurementSpendAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        [HttpGet("executive-kpis")]
        public async Task<ActionResult<ProcurementKpiDashboardDto>> GetExecutiveKpis(CancellationToken cancellationToken = default)
        {
            var kpis = await _analyticsService.GetExecutiveKpisAsync(cancellationToken);
            return Ok(kpis);
        }

        [HttpGet("ppv-analysis")]
        public async Task<ActionResult<PurchasePriceVarianceReportDto>> GetPpvAnalysis(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            CancellationToken cancellationToken = default)
        {
            var start = fromDate ?? DateTime.UtcNow.AddMonths(-1);
            var end = toDate ?? DateTime.UtcNow;
            var report = await _analyticsService.CalculatePpvAsync(start, end, cancellationToken);
            return Ok(report);
        }

        [HttpGet("spend-pareto")]
        public async Task<ActionResult<SpendParetoMatrixDto>> GetSpendPareto(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            CancellationToken cancellationToken = default)
        {
            var start = fromDate ?? DateTime.UtcNow.AddMonths(-6);
            var end = toDate ?? DateTime.UtcNow;
            var matrix = await _analyticsService.GetSpendParetoMatrixAsync(start, end, cancellationToken);
            return Ok(matrix);
        }

        [HttpGet("supplier-otif")]
        public async Task<ActionResult<SupplierOtifScorecardDto>> GetSupplierOtif(
            [FromQuery] int? vendorId,
            [FromQuery] DateTime? fromDate,
            CancellationToken cancellationToken = default)
        {
            var scorecard = await _analyticsService.CalculateSupplierOtifAsync(vendorId, fromDate, cancellationToken);
            return Ok(scorecard);
        }

        [HttpPost("cache/purge")]
        public async Task<IActionResult> PurgeAnalyticsCache(CancellationToken cancellationToken = default)
        {
            await _analyticsService.InvalidateAnalyticsCacheAsync(cancellationToken);
            return Ok(new { message = "Procurement analytics cache successfully purged." });
        }
    }
}

