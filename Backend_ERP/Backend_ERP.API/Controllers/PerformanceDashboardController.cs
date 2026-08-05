using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Route("api/performance-dashboard")]
    [ApiController]
    public class PerformanceDashboardController : ControllerBase
    {
        private readonly IPerformanceService _service;

        public PerformanceDashboardController(IPerformanceService service)
        {
            _service = service;
        }

        [HttpGet("dashboard")]
        public Task<ActionResult<PerformanceDashboardDto>> Dashboard(
            [FromQuery] PerformanceFilterDto filter,
            CancellationToken cancellationToken) =>
            Execute(() => _service.GetDashboardAsync(filter, cancellationToken));

        [HttpGet("leaderboards")]
        public Task<ActionResult<IReadOnlyList<PerformanceLeaderboardDto>>> Leaderboards(
            [FromQuery] PerformanceFilterDto filter,
            CancellationToken cancellationToken) =>
            Execute(() => _service.GetLeaderboardsAsync(filter, cancellationToken));

        [HttpGet("salespersons")]
        public Task<ActionResult<IReadOnlyList<PerformanceSalesPersonDto>>> Salespersons(
            [FromQuery] PerformanceFilterDto filter,
            CancellationToken cancellationToken) =>
            Execute(() => _service.GetSalespersonsAsync(filter, cancellationToken));

        [HttpGet("salespersons/{id:int}")]
        public async Task<ActionResult<PerformanceSalesPersonDto>> Salesperson(
            int id,
            [FromQuery] PerformanceFilterDto filter,
            CancellationToken cancellationToken)
        {
            try
            {
                var row = await _service.GetSalespersonAsync(id, filter, cancellationToken);
                return row is null ? NotFound() : Ok(row);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpGet("salespersons/{id:int}/timeline")]
        public Task<ActionResult<IReadOnlyList<PerformanceTimelineDto>>> Timeline(
            int id,
            [FromQuery] PerformanceFilterDto filter,
            CancellationToken cancellationToken) =>
            Execute(() => _service.GetTimelineAsync(id, filter, cancellationToken));

        [HttpGet("salespersons/{id:int}/monthly")]
        public Task<ActionResult<IReadOnlyList<PerformanceTrendDto>>> Monthly(
            int id,
            [FromQuery] PerformanceFilterDto filter,
            CancellationToken cancellationToken) =>
            Execute(() => _service.GetMonthlyAsync(id, filter, cancellationToken));

        [HttpGet("salespersons/{id:int}/performance-history")]
        public Task<ActionResult<IReadOnlyList<PerformanceHistoryDto>>> PerformanceHistory(
            int id,
            CancellationToken cancellationToken) =>
            Execute(() => _service.GetPerformanceHistoryAsync(id, cancellationToken));

        [HttpGet("salespersons/{id:int}/target-history")]
        public Task<ActionResult<IReadOnlyList<PerformanceHistoryDto>>> TargetHistory(
            int id,
            CancellationToken cancellationToken) =>
            Execute(() => _service.GetTargetHistoryAsync(id, cancellationToken));

        [HttpGet("analytics")]
        public Task<ActionResult<PerformanceAnalyticsDto>> Analytics(
            [FromQuery] PerformanceFilterDto filter,
            CancellationToken cancellationToken) =>
            Execute(() => _service.GetAnalyticsAsync(filter, cancellationToken));

        [HttpGet("reports")]
        public Task<ActionResult<PerformanceReportDto>> Reports(
            [FromQuery] PerformanceFilterDto filter,
            CancellationToken cancellationToken) =>
            Execute(() => _service.GetReportsAsync(filter, cancellationToken));

        [HttpPost("reports/export")]
        public Task<ActionResult<PerformanceExportDto>> Export(
            [FromBody] PerformanceExportRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken) =>
            Execute(() => _service.ExportAsync(
                request,
                userId is > 0 ? userId.Value.ToString() : "1",
                cancellationToken));

        [HttpGet("exports/history")]
        public async Task<ActionResult<IReadOnlyList<PerformanceExportDto>>> ExportHistory(
            CancellationToken cancellationToken) =>
            Ok(await _service.GetExportHistoryAsync(cancellationToken));

        [HttpGet("lookups/salespersons")]
        public async Task<ActionResult<IReadOnlyList<PerformanceLookupDto>>> LookupSalespersons(
            CancellationToken cancellationToken) =>
            Ok(await _service.LookupSalespersonsAsync(cancellationToken));

        [HttpGet("lookups/teams")]
        public async Task<ActionResult<IReadOnlyList<PerformanceLookupDto>>> LookupTeams(
            CancellationToken cancellationToken) =>
            Ok(await _service.LookupTeamsAsync(cancellationToken));

        [HttpGet("lookups/branches")]
        public async Task<ActionResult<IReadOnlyList<PerformanceLookupDto>>> LookupBranches(
            CancellationToken cancellationToken) =>
            Ok(await _service.LookupBranchesAsync(cancellationToken));

        [HttpGet("lookups/regional-managers")]
        public async Task<ActionResult<IReadOnlyList<PerformanceLookupDto>>> LookupRegionalManagers(
            CancellationToken cancellationToken) =>
            Ok(await _service.LookupRegionalManagersAsync(cancellationToken));

        [HttpGet("permissions")]
        public async Task<ActionResult<IReadOnlyList<string>>> Permissions() =>
            Ok(await _service.GetPermissionsAsync());

        private async Task<ActionResult<T>> Execute<T>(Func<Task<T>> action)
        {
            try
            {
                return Ok(await action());
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }
    }
}
