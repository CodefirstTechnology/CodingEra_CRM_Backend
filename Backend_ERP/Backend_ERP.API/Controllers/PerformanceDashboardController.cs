using ERP.API.Security;
using ERP.Application.Common.Security;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Enums;
using ERP.Shared.Security;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Route("api/performance-dashboard")]
    [ApiController]
    [RequirePermission(ErpPermissions.Performance.View)]
    public class PerformanceDashboardController : ControllerBase
    {
        private readonly IPerformanceService _service;
        private readonly ICurrentUser _currentUser;
        private readonly IErpAuthorizationService _authService;

        public PerformanceDashboardController(
            IPerformanceService service,
            ICurrentUser currentUser,
            IErpAuthorizationService authService)
        {
            _service = service;
            _currentUser = currentUser;
            _authService = authService;
        }

        [HttpGet("dashboard")]
        public Task<ActionResult<PerformanceDashboardDto>> Dashboard(
            [FromQuery] PerformanceFilterDto filter,
            CancellationToken cancellationToken)
        {
            ApplyOwnScopeFilter(filter);
            return Execute(() => _service.GetDashboardAsync(filter, cancellationToken));
        }

        [HttpGet("leaderboards")]
        [RequirePermission(ErpPermissions.Performance.LeaderboardView)]
        public Task<ActionResult<IReadOnlyList<PerformanceLeaderboardDto>>> Leaderboards(
            [FromQuery] PerformanceFilterDto filter,
            CancellationToken cancellationToken) =>
            Execute(() => _service.GetLeaderboardsAsync(filter, cancellationToken));

        [HttpGet("salespersons")]
        public Task<ActionResult<IReadOnlyList<PerformanceSalesPersonDto>>> Salespersons(
            [FromQuery] PerformanceFilterDto filter,
            CancellationToken cancellationToken)
        {
            ApplyOwnScopeFilter(filter);
            return Execute(() => _service.GetSalespersonsAsync(filter, cancellationToken));
        }

        [HttpGet("salespersons/{id:int}")]
        public async Task<ActionResult<PerformanceSalesPersonDto>> Salesperson(
            int id,
            [FromQuery] PerformanceFilterDto filter,
            CancellationToken cancellationToken)
        {
            try
            {
                if (_currentUser.Scope == AccessScope.Own && _currentUser.UserId.HasValue && id != _currentUser.UserId.Value)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not have access to another salesperson's performance." });
                }

                ApplyOwnScopeFilter(filter);
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
            CancellationToken cancellationToken)
        {
            if (_currentUser.Scope == AccessScope.Own && _currentUser.UserId.HasValue && id != _currentUser.UserId.Value)
            {
                return Task.FromResult<ActionResult<IReadOnlyList<PerformanceTimelineDto>>>(
                    StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not have access to another salesperson's performance." }));
            }

            ApplyOwnScopeFilter(filter);
            return Execute(() => _service.GetTimelineAsync(id, filter, cancellationToken));
        }

        [HttpGet("salespersons/{id:int}/monthly")]
        public Task<ActionResult<IReadOnlyList<PerformanceTrendDto>>> Monthly(
            int id,
            [FromQuery] PerformanceFilterDto filter,
            CancellationToken cancellationToken)
        {
            if (_currentUser.Scope == AccessScope.Own && _currentUser.UserId.HasValue && id != _currentUser.UserId.Value)
            {
                return Task.FromResult<ActionResult<IReadOnlyList<PerformanceTrendDto>>>(
                    StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not have access to another salesperson's performance." }));
            }

            ApplyOwnScopeFilter(filter);
            return Execute(() => _service.GetMonthlyAsync(id, filter, cancellationToken));
        }

        [HttpGet("salespersons/{id:int}/performance-history")]
        public Task<ActionResult<IReadOnlyList<PerformanceHistoryDto>>> PerformanceHistory(
            int id,
            CancellationToken cancellationToken)
        {
            if (_currentUser.Scope == AccessScope.Own && _currentUser.UserId.HasValue && id != _currentUser.UserId.Value)
            {
                return Task.FromResult<ActionResult<IReadOnlyList<PerformanceHistoryDto>>>(
                    StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not have access to another salesperson's performance." }));
            }

            return Execute(() => _service.GetPerformanceHistoryAsync(id, cancellationToken));
        }

        [HttpGet("salespersons/{id:int}/target-history")]
        public Task<ActionResult<IReadOnlyList<PerformanceHistoryDto>>> TargetHistory(
            int id,
            CancellationToken cancellationToken)
        {
            if (_currentUser.Scope == AccessScope.Own && _currentUser.UserId.HasValue && id != _currentUser.UserId.Value)
            {
                return Task.FromResult<ActionResult<IReadOnlyList<PerformanceHistoryDto>>>(
                    StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not have access to another salesperson's performance." }));
            }

            return Execute(() => _service.GetTargetHistoryAsync(id, cancellationToken));
        }

        [HttpGet("analytics")]
        public Task<ActionResult<PerformanceAnalyticsDto>> Analytics(
            [FromQuery] PerformanceFilterDto filter,
            CancellationToken cancellationToken)
        {
            ApplyOwnScopeFilter(filter);
            return Execute(() => _service.GetAnalyticsAsync(filter, cancellationToken));
        }

        [HttpGet("reports")]
        [HttpPost("reports")]
        public Task<ActionResult<PerformanceReportDto>> Reports(
            [FromQuery] PerformanceFilterDto? queryFilter,
            [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] PerformanceFilterDto? bodyFilter,
            CancellationToken cancellationToken)
        {
            var filter = bodyFilter ?? queryFilter ?? new PerformanceFilterDto();
            ApplyOwnScopeFilter(filter);
            return Execute(() => _service.GetReportsAsync(filter, cancellationToken));
        }

        [HttpPost("reports/export")]
        [RequirePermission(ErpPermissions.Performance.Export)]
        public Task<ActionResult<PerformanceExportDto>> Export(
            [FromBody] PerformanceExportRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken) =>
            Execute(() => _service.ExportAsync(
                request,
                _currentUser.UserId?.ToString() ?? (userId is > 0 ? userId.Value.ToString() : "system"),
                cancellationToken));

        [HttpGet("exports/history")]
        [RequirePermission(ErpPermissions.Performance.Export)]
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
        public ActionResult<IReadOnlyList<string>> Permissions() =>
            Ok(_currentUser.Permissions.ToList());

        private void ApplyOwnScopeFilter(PerformanceFilterDto filter)
        {
            if (_currentUser.Scope == AccessScope.Own && _currentUser.UserId.HasValue)
            {
                filter.SalesPersonUserId = _currentUser.UserId.Value;
            }
        }

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
