using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.API.Security;
using ERP.Application.Common.Security;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Enums;
using ERP.Infrastructure.Data;
using ERP.Shared.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.API.Controllers
{
    [Route("api/performance-dashboard")]
    [ApiController]
    [RequirePermission(ErpPermissions.Performance.View)]
    public class PerformanceDashboardController : ControllerBase
    {
        private readonly IPerformanceService _service;
        private readonly IPerformanceHubService _hubService;
        private readonly IPerformanceReportService _reportService;
        private readonly ICurrentUser _currentUser;
        private readonly IErpAuthorizationService _authService;
        private readonly ERPDbContext _context;

        public PerformanceDashboardController(
            IPerformanceService service,
            IPerformanceHubService hubService,
            IPerformanceReportService reportService,
            ICurrentUser currentUser,
            IErpAuthorizationService authService,
            ERPDbContext context)
        {
            _service = service;
            _hubService = hubService;
            _reportService = reportService;
            _currentUser = currentUser;
            _authService = authService;
            _context = context;
        }

        // =========================================================================
        // SUBMODULE 7 ENTERPRISE HUBS: COCKPIT, LEADERBOARD, SCHEMES & COMMISSIONS
        // =========================================================================

        [HttpGet("overview/{userId:int?}")]
        public async Task<ActionResult<RepOverviewCockpitDto>> GetOverview(
            int? userId,
            [FromQuery] int financialYear = 2026,
            CancellationToken ct = default)
        {
            int targetUserId = userId ?? _currentUser.UserId ?? 1;

            if (_currentUser.Scope == AccessScope.Own && _currentUser.UserId.HasValue && targetUserId != _currentUser.UserId.Value)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not have access to another salesperson's performance profile." });
            }

            var overview = await _hubService.GetRepOverviewAsync(targetUserId, financialYear, ct);
            return Ok(overview);
        }

        [HttpGet("leaderboard")]
        [RequirePermission(ErpPermissions.Performance.LeaderboardView)]
        public async Task<ActionResult<LeaderboardPodiumDto>> GetLeaderboard(
            [FromQuery] LeaderboardFilterDto filter,
            CancellationToken ct = default)
        {
            var result = await _hubService.GetLeaderboardAsync(filter, ct);
            return Ok(result);
        }

        [HttpGet("schemes")]
        public async Task<ActionResult<List<IncentiveSchemeDto>>> GetSchemes(
            [FromQuery] int financialYear = 2026,
            CancellationToken ct = default)
        {
            var schemes = await _hubService.GetIncentiveSchemesAsync(financialYear, ct);
            return Ok(schemes);
        }

        [HttpPost("schemes")]
        public async Task<ActionResult<IncentiveSchemeDto>> CreateScheme(
            [FromBody] CreateIncentiveSchemeDto request,
            CancellationToken ct = default)
        {
            int creatorId = _currentUser.UserId ?? 1;
            var created = await _hubService.CreateIncentiveSchemeAsync(request, creatorId, ct);
            return Ok(created);
        }

        [HttpPost("schemes/{id:int}/toggle-active")]
        public async Task<ActionResult<bool>> ToggleScheme(
            int id,
            [FromQuery] bool isActive,
            CancellationToken ct = default)
        {
            var success = await _hubService.ToggleSchemeActiveAsync(id, isActive, ct);
            return Ok(success);
        }

        [HttpGet("commissions/{userId:int}/ledger")]
        public async Task<ActionResult<List<SalesCommissionLedgerDto>>> GetCommissionLedger(
            int userId,
            [FromQuery] int financialYear = 2026,
            CancellationToken ct = default)
        {
            if (_currentUser.Scope == AccessScope.Own && _currentUser.UserId.HasValue && userId != _currentUser.UserId.Value)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not have access to another salesperson's commission ledgers." });
            }

            var ledgers = await _hubService.GetRepCommissionLedgerAsync(userId, financialYear, ct);
            return Ok(ledgers);
        }

        [HttpPost("commissions/{ledgerId:int}/approve")]
        public async Task<ActionResult<bool>> ApproveCommission(
            int ledgerId,
            [FromBody] CommissionApprovalRequestDto request,
            CancellationToken ct = default)
        {
            request.LedgerId = ledgerId;
            int approverId = _currentUser.UserId ?? 1;
            var success = await _hubService.ApproveCommissionEntryAsync(request, approverId, ct);
            return Ok(success);
        }

        [HttpPost("commissions/disburse")]
        public async Task<ActionResult<bool>> DisburseCommission(
            [FromBody] CommissionDisbursementRequestDto request,
            CancellationToken ct = default)
        {
            int disburserId = _currentUser.UserId ?? 1;
            var success = await _hubService.DisburseCommissionPayoutAsync(request, disburserId, ct);
            return Ok(success);
        }

        // =========================================================================
        // SUBMODULE 7 ANALYTICAL EXECUTIVE REPORTS
        // =========================================================================

        [HttpGet("reports/commission-statement")]
        [RequirePermission(ErpPermissions.Performance.ReportView)]
        public async Task<ActionResult<ExecutiveCommissionPayoutReportDto>> GetCommissionStatement(
            [FromQuery] int financialYear = 2026,
            [FromQuery] string? branch = null,
            [FromQuery] string? salesTeam = null,
            CancellationToken ct = default)
        {
            var report = await _reportService.GetExecutiveCommissionPayoutStatementAsync(financialYear, branch, salesTeam, ct);
            return Ok(report);
        }

        [HttpGet("reports/velocity-matrix")]
        [RequirePermission(ErpPermissions.Performance.ReportView)]
        public async Task<ActionResult<RepCohortVelocityMatrixReportDto>> GetVelocityMatrix(
            [FromQuery] int financialYear = 2026,
            [FromQuery] string? branch = null,
            [FromQuery] string? salesTeam = null,
            CancellationToken ct = default)
        {
            var report = await _reportService.GetRepCohortVelocityMatrixAsync(financialYear, branch, salesTeam, ct);
            return Ok(report);
        }

        [HttpGet("reports/margin-erosion")]
        [RequirePermission(ErpPermissions.Performance.ReportView)]
        public async Task<ActionResult<DiscountMarginErosionReportDto>> GetMarginErosion(
            [FromQuery] int financialYear = 2026,
            [FromQuery] string? branch = null,
            [FromQuery] string? salesTeam = null,
            CancellationToken ct = default)
        {
            var report = await _reportService.GetDiscountMarginErosionReportAsync(financialYear, branch, salesTeam, ct);
            return Ok(report);
        }

        [HttpGet("reports/pip-audit")]
        [RequirePermission(ErpPermissions.Performance.ReportView)]
        public async Task<ActionResult<PipEarlyWarningAuditReportDto>> GetPipAudit(
            [FromQuery] int financialYear = 2026,
            [FromQuery] string? branch = null,
            [FromQuery] string? salesTeam = null,
            CancellationToken ct = default)
        {
            var report = await _reportService.GetPipEarlyWarningAuditAsync(financialYear, branch, salesTeam, ct);
            return Ok(report);
        }

        [HttpGet("reports/export-csv")]
        [RequirePermission(ErpPermissions.Performance.Export)]
        public async Task<IActionResult> ExportCsv(
            [FromQuery] string reportType = "commission",
            [FromQuery] int financialYear = 2026,
            [FromQuery] string? branch = null,
            [FromQuery] string? salesTeam = null,
            CancellationToken ct = default)
        {
            var csvBytes = await _reportService.ExportReportToCsvAsync(reportType, financialYear, branch, salesTeam, ct);
            string fileName = $"performance-{reportType}-fy{financialYear}-{DateTime.UtcNow:yyyyMMdd}.csv";
            return File(csvBytes, "text/csv", fileName);
        }

        // =========================================================================
        // LEGACY DASHBOARD, REPORTS & LOOKUP ENDPOINTS (100% BACKWARD COMPATIBLE)
        // =========================================================================

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

        [HttpGet("reports/incentives")]
        [RequirePermission(ErpPermissions.Performance.ReportView)]
        public async Task<ActionResult<IEnumerable<SalesIncentiveStatementRowDto>>> GetIncentiveStatement(
            [FromQuery] int financialYear = 2026,
            CancellationToken ct = default)
        {
            var targets = await _context.SalesTargets
                .Include(t => t.ChildTargets)
                .Where(t => t.FinancialYear == financialYear && !t.IsDeleted && t.Status != "Cancelled" && t.SalesPersonUserId.HasValue)
                .ToListAsync(ct);

            var reps = await _context.SalespersonPerformances
                .Where(p => p.FinancialYear == financialYear)
                .ToDictionaryAsync(p => p.SalesPersonUserId, ct);

            var rows = targets.Select(t =>
            {
                reps.TryGetValue(t.SalesPersonUserId!.Value, out var rep);
                decimal baseIncentive = t.AchievementPercentage >= 100m ? (t.TargetValue * 0.015m) : (t.AchievementPercentage >= 80m ? t.AchievedValue * 0.005m : 0m);
                decimal kicker = t.AchievementPercentage >= 120m ? ((t.AchievedValue - t.TargetValue) * 0.025m) : 0m;

                return new SalesIncentiveStatementRowDto(
                    t.SalesPersonUserId!.Value,
                    rep?.SalesPersonName ?? $"User {t.SalesPersonUserId}",
                    rep?.Branch ?? t.Branch,
                    rep?.SalesTeam ?? t.SalesTeam,
                    t.TargetValue,
                    t.AchievedValue,
                    t.AchievementPercentage,
                    Math.Round(baseIncentive, 2, MidpointRounding.AwayFromZero),
                    Math.Round(kicker, 2, MidpointRounding.AwayFromZero),
                    t.CalculatedCommissionAmount
                );
            }).OrderByDescending(x => x.TotalCommissionPayable).ToList();

            return Ok(rows);
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
