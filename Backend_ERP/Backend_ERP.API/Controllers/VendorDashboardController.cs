using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Route("api/vendors/dashboard")]
    [ApiController]
    public class VendorDashboardController : ControllerBase
    {
        private readonly IVendorDashboardService _dashboardService;

        public VendorDashboardController(IVendorDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("kpis")]
        public async Task<ActionResult<VendorDashboardKpiDto>> GetKpis(
            [FromQuery] string tenantId = "default",
            [FromQuery] bool forceRefresh = false,
            CancellationToken cancellationToken = default)
        {
            var kpis = await _dashboardService.GetDashboardKpisAsync(tenantId, forceRefresh, cancellationToken);
            return Ok(kpis);
        }

        [HttpGet("charts")]
        public async Task<ActionResult<VendorDashboardChartsDto>> GetCharts(
            [FromQuery] string tenantId = "default",
            [FromQuery] bool forceRefresh = false,
            CancellationToken cancellationToken = default)
        {
            var charts = await _dashboardService.GetDashboardChartsAsync(tenantId, forceRefresh, cancellationToken);
            return Ok(charts);
        }

        [HttpGet]
        public async Task<IActionResult> GetDefault(
            [FromQuery] string tenantId = "default",
            [FromQuery] bool forceRefresh = false,
            CancellationToken cancellationToken = default)
        {
            var kpis = await _dashboardService.GetDashboardKpisAsync(tenantId, forceRefresh, cancellationToken);
            var charts = await _dashboardService.GetDashboardChartsAsync(tenantId, forceRefresh, cancellationToken);

            var vendorTypeDist = new System.Collections.Generic.List<VendorDistributionItemDto>();
            foreach (var c in charts.SpendByCategory)
            {
                vendorTypeDist.Add(new VendorDistributionItemDto
                {
                    Label = c.CategoryName,
                    Count = (int)c.SpendPercentage
                });
            }

            var countryDist = new System.Collections.Generic.List<VendorDistributionItemDto>
            {
                new() { Label = "India", Count = kpis.TotalVendors }
            };

            return Ok(new
            {
                totalVendors = kpis.TotalVendors,
                activeVendors = kpis.ActiveVendors,
                blockedVendors = kpis.OnHoldCount,
                pendingApproval = kpis.PendingKycCount,
                preferredVendors = (int)(kpis.ActiveVendors * 0.4),
                averageRating = (double)kpis.AverageQualityRating,
                countryDistribution = countryDist,
                vendorTypeDistribution = vendorTypeDist,
                fleetOtifPercentage = kpis.FleetOtifPercentage,
                averageQualityRating = kpis.AverageQualityRating,
                totalOpenCommitments = kpis.TotalOpenCommitments,
                msmeDueRiskCount = kpis.MsmeDueRiskCount,
                criticalSingleSourcedItems = kpis.CriticalSingleSourcedItems
            });
        }
    }
}

