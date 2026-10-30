using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.API.Security;
using ERP.Application.Common.Security;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Enums;
using ERP.Shared.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Route("api/sales-targets")]
    [ApiController]
    [RequirePermission(ErpPermissions.SalesTargets.View)]
    public class SalesTargetsController : ControllerBase
    {
        private readonly ISalesTargetService _service;
        private readonly ISalesTargetDashboardService _dashboardService;
        private readonly ISalesTargetReportService _reportService;
        private readonly ICurrentUser _currentUser;
        private readonly IErpAuthorizationService _authService;

        public SalesTargetsController(
            ISalesTargetService service,
            ISalesTargetDashboardService dashboardService,
            ISalesTargetReportService reportService,
            ICurrentUser currentUser,
            IErpAuthorizationService authService)
        {
            _service = service;
            _dashboardService = dashboardService;
            _reportService = reportService;
            _currentUser = currentUser;
            _authService = authService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<SalesTargetListItemDto>>> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? targetType,
            [FromQuery] string? targetCategory,
            [FromQuery] string? branch,
            [FromQuery] int? salesPersonUserId,
            [FromQuery] int? financialYear,
            [FromQuery] string? targetPeriod,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var effectiveSalesPersonUserId = (_currentUser.Scope == AccessScope.Own && _currentUser.UserId.HasValue)
                ? _currentUser.UserId.Value
                : salesPersonUserId;

            return Ok(await _service.GetAllAsync(new SalesTargetListQueryDto
            {
                Search = search,
                Status = status,
                TargetType = targetType,
                TargetCategory = targetCategory,
                Branch = branch,
                SalesPersonUserId = effectiveSalesPersonUserId,
                FinancialYear = financialYear,
                TargetPeriod = targetPeriod,
                DateFrom = dateFrom,
                DateTo = dateTo
            }, cancellationToken));
        }

        [HttpGet("tree")]
        public async Task<ActionResult<SalesTargetTreeDto>> GetTree(
            [FromQuery] int? rootTargetId,
            [FromQuery] int? fiscalYear,
            CancellationToken cancellationToken)
        {
            var fy = fiscalYear ?? DateTime.UtcNow.Year;
            return Ok(await _service.GetHierarchicalTreeAsync(rootTargetId, fy, cancellationToken));
        }

        [HttpGet("dashboard")]
        [RequirePermission(ErpPermissions.SalesTargets.DashboardView)]
        public async Task<ActionResult<SalesTargetDashboardDto>> Dashboard(
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.GetDashboardAsync(cancellationToken));
        }

        [HttpGet("dashboard/kpis")]
        [RequirePermission(ErpPermissions.SalesTargets.DashboardView)]
        public async Task<ActionResult<SalesTargetDashboardDto>> GetDashboardKpis(
            [FromQuery] int? fiscalYear,
            [FromQuery] string? period,
            [FromQuery] string? branch,
            [FromQuery] int? repUserId,
            CancellationToken cancellationToken)
        {
            var fy = fiscalYear ?? DateTime.UtcNow.Year;
            var effectiveRepUserId = (_currentUser.Scope == AccessScope.Own && _currentUser.UserId.HasValue)
                ? _currentUser.UserId.Value
                : repUserId;

            return Ok(await _dashboardService.GetDashboardKPIsAsync(fy, period, branch, effectiveRepUserId, cancellationToken));
        }

        [HttpGet("reports")]
        [HttpPost("reports")]
        public async Task<ActionResult<SalesTargetReportDto>> Reports(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? targetType,
            [FromQuery] string? targetCategory,
            [FromQuery] string? branch,
            [FromQuery] int? salesPersonUserId,
            [FromQuery] int? financialYear,
            [FromQuery] int? userId,
            [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] SalesTargetListQueryDto? bodyFilter,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var filter = bodyFilter ?? new SalesTargetListQueryDto
            {
                Search = search,
                Status = status,
                TargetType = targetType,
                TargetCategory = targetCategory,
                Branch = branch,
                SalesPersonUserId = salesPersonUserId,
                FinancialYear = financialYear
            };
            return Ok(await _service.GetReportsAsync(filter, cancellationToken));
        }

        [HttpPost("reports/variance")]
        [RequirePermission(ErpPermissions.SalesTargets.ReportsView)]
        public async Task<ActionResult<SalesTargetVarianceReportDto>> GetVarianceReport(
            [FromBody] SalesTargetReportFilterDto filter,
            CancellationToken cancellationToken)
        {
            return Ok(await _reportService.GetVarianceReportAsync(filter, cancellationToken));
        }

        [HttpGet("reports/leaderboard")]
        [RequirePermission(ErpPermissions.SalesTargets.ReportsView)]
        public async Task<ActionResult<SalesTargetRepLeaderboardDto>> GetLeaderboardReport(
            [FromQuery] int? fiscalYear,
            [FromQuery] string? period,
            CancellationToken cancellationToken)
        {
            var fy = fiscalYear ?? DateTime.UtcNow.Year;
            return Ok(await _reportService.GetRepLeaderboardAuditAsync(fy, period, cancellationToken));
        }

        [HttpGet("reports/category-matrix")]
        [RequirePermission(ErpPermissions.SalesTargets.ReportsView)]
        public async Task<ActionResult<SalesTargetCategoryMatrixDto>> GetCategoryMatrixReport(
            [FromQuery] int? fiscalYear,
            [FromQuery] string? period,
            CancellationToken cancellationToken)
        {
            var fy = fiscalYear ?? DateTime.UtcNow.Year;
            return Ok(await _reportService.GetCategoryContributionMatrixAsync(fy, period, cancellationToken));
        }

        [HttpPost("reports/export")]
        [RequirePermission(ErpPermissions.SalesTargets.Create)]
        public async Task<ActionResult<SalesTargetExportMetadataDto>> ExportReports(
            [FromBody] SalesTargetExportRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.ExportReportsAsync(request, cancellationToken));
        }

        [HttpPost("copy-previous")]
        [RequirePermission(ErpPermissions.SalesTargets.Create)]
        public async Task<ActionResult<SalesTargetDto>> CopyPrevious(
            [FromBody] SalesTargetCopyPreviousRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var created = await _service.CopyPreviousAsync(request, ResolveActingUser(userId), cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpGet("permissions")]
        public async Task<ActionResult<IReadOnlyList<string>>> GetPermissions()
        {
            return Ok(await _service.GetPermissionsAsync());
        }

        [HttpGet("lookups/salespersons")]
        public async Task<ActionResult<IReadOnlyList<SalesTargetLookupDto>>> LookupSalespersons(
            CancellationToken cancellationToken)
        {
            return Ok(await _service.LookupSalespersonsAsync(cancellationToken));
        }

        [HttpGet("lookups/teams")]
        public async Task<ActionResult<IReadOnlyList<SalesTargetLookupDto>>> LookupTeams(
            CancellationToken cancellationToken)
        {
            return Ok(await _service.LookupTeamsAsync(cancellationToken));
        }

        [HttpGet("lookups/branches")]
        public async Task<ActionResult<IReadOnlyList<SalesTargetLookupDto>>> LookupBranches(
            CancellationToken cancellationToken)
        {
            return Ok(await _service.LookupBranchesAsync(cancellationToken));
        }

        [HttpGet("lookups/regional-managers")]
        public async Task<ActionResult<IReadOnlyList<SalesTargetLookupDto>>> LookupRegionalManagers(
            CancellationToken cancellationToken)
        {
            return Ok(await _service.LookupRegionalManagersAsync(cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<SalesTargetDto>> GetById(int id, CancellationToken cancellationToken)
        {
            var target = await _service.GetByIdAsync(id, cancellationToken);
            if (target is null)
            {
                return NotFound();
            }

            if (_currentUser.Scope == AccessScope.Own &&
                _currentUser.UserId.HasValue &&
                target.SalesPersonUserId.HasValue &&
                target.SalesPersonUserId.Value != _currentUser.UserId.Value)
            {
                return Forbid();
            }

            return Ok(target);
        }

        [HttpPost]
        [RequirePermission(ErpPermissions.SalesTargets.Create)]
        public async Task<ActionResult<SalesTargetDto>> Create(
            [FromBody] SalesTargetCreateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var created = await _service.CreateAsync(request, ResolveActingUser(userId), cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("{id:int}/allocate-child")]
        [RequirePermission(ErpPermissions.SalesTargets.Create)]
        public async Task<ActionResult<SalesTargetDto>> AllocateChild(
            int id,
            [FromBody] SalesTargetCreateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var created = await _service.AllocateChildTargetAsync(id, request, ResolveActingUser(userId), cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("{id:int}/revise")]
        [RequirePermission(ErpPermissions.SalesTargets.Edit)]
        public async Task<ActionResult<SalesTargetDto>> ReviseTarget(
            int id,
            [FromBody] SalesTargetRevisionRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var revised = await _service.ReviseTargetAsync(id, request, ResolveActingUser(userId), cancellationToken);
                return Ok(revised);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("{id:int}/submit-approval")]
        [RequirePermission(ErpPermissions.SalesTargets.Edit)]
        public async Task<ActionResult<SalesTargetDto>> SubmitApproval(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var submitted = await _service.SubmitForApprovalAsync(id, ResolveActingUser(userId), cancellationToken);
                return Ok(submitted);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("{id:int}/approve")]
        [RequirePermission(ErpPermissions.SalesTargets.Edit)]
        public async Task<ActionResult<SalesTargetDto>> Approve(
            int id,
            [FromBody] SalesTargetApprovalActionDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var approved = await _service.ApproveTargetAsync(id, request, ResolveActingUser(userId), cancellationToken);
                return Ok(approved);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("{id:int}/reject")]
        [RequirePermission(ErpPermissions.SalesTargets.Edit)]
        public async Task<ActionResult<SalesTargetDto>> Reject(
            int id,
            [FromBody] SalesTargetApprovalActionDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var rejected = await _service.RejectTargetAsync(id, request, ResolveActingUser(userId), cancellationToken);
                return Ok(rejected);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPut("{id:int}")]
        [RequirePermission(ErpPermissions.SalesTargets.Edit)]
        public async Task<ActionResult<SalesTargetDto>> Update(
            int id,
            [FromBody] SalesTargetUpdateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var updated = await _service.UpdateAsync(id, request, ResolveActingUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpDelete("{id:int}")]
        [RequirePermission(ErpPermissions.SalesTargets.Delete)]
        public async Task<IActionResult> Delete(int id, [FromQuery] int? userId, CancellationToken cancellationToken)
        {
            var deleted = await _service.DeleteAsync(id, ResolveActingUser(userId), cancellationToken);
            return deleted ? NoContent() : NotFound();
        }

        [HttpPost("{id:int}/duplicate")]
        [RequirePermission(ErpPermissions.SalesTargets.Create)]
        public async Task<ActionResult<SalesTargetDto>> Duplicate(
            int id,
            [FromBody] SalesTargetDuplicateRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var dup = await _service.DuplicateAsync(id, request, ResolveActingUser(userId), cancellationToken);
                return dup is null ? NotFound() : CreatedAtAction(nameof(GetById), new { id = dup.Id }, dup);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("{id:int}/activate")]
        [RequirePermission(ErpPermissions.SalesTargets.Edit)]
        public Task<ActionResult<SalesTargetDto>> Activate(
            int id,
            [FromBody] SalesTargetRemarksRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken) =>
            Workflow(id, request, userId, _service.ActivateAsync, cancellationToken);

        [HttpPost("{id:int}/deactivate")]
        [RequirePermission(ErpPermissions.SalesTargets.Edit)]
        public Task<ActionResult<SalesTargetDto>> Deactivate(
            int id,
            [FromBody] SalesTargetRemarksRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken) =>
            Workflow(id, request, userId, _service.DeactivateAsync, cancellationToken);

        [HttpPost("{id:int}/status")]
        [RequirePermission(ErpPermissions.SalesTargets.Edit)]
        public async Task<ActionResult<SalesTargetDto>> UpdateStatus(
            int id,
            [FromBody] SalesTargetStatusUpdateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var updated = await _service.UpdateStatusAsync(id, request, ResolveActingUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("{id:int}/progress")]
        [RequirePermission(ErpPermissions.SalesTargets.Edit)]
        public async Task<ActionResult<SalesTargetDto>> UpdateProgress(
            int id,
            [FromBody] SalesTargetProgressUpdateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var updated = await _service.UpdateProgressAsync(id, request, ResolveActingUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpGet("{id:int}/progress")]
        public async Task<ActionResult<IReadOnlyList<SalesTargetProgressDto>>> GetProgress(
            int id,
            CancellationToken cancellationToken)
        {
            var progress = await _service.GetProgressAsync(id, cancellationToken);
            return progress is null ? NotFound() : Ok(progress);
        }

        [HttpGet("{id:int}/history")]
        public async Task<ActionResult<IReadOnlyList<SalesTargetHistoryDto>>> GetHistory(
            int id,
            CancellationToken cancellationToken)
        {
            var history = await _service.GetHistoryAsync(id, cancellationToken);
            return history is null ? NotFound() : Ok(history);
        }

        [HttpPost("{id:int}/prorate")]
        [RequirePermission(ErpPermissions.SalesTargets.Edit)]
        public async Task<ActionResult<SalesTargetDto>> ProrateTarget(
            int id,
            [FromBody] ProrateTargetRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var actingUser = ResolveActingUser(userId);
            var updated = await _service.ProrateTargetAsync(id, request, actingUser, cancellationToken);
            if (updated == null)
            {
                return BadRequest("Target is locked or not found.");
            }
            return Ok(updated);
        }

        private async Task<ActionResult<SalesTargetDto>> Workflow(
            int id,
            SalesTargetRemarksRequestDto? request,
            int? userId,
            Func<int, SalesTargetRemarksRequestDto?, string, CancellationToken, Task<SalesTargetDto?>> action,
            CancellationToken cancellationToken)
        {
            try
            {
                var updated = await action(id, request, ResolveActingUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        private string ResolveActingUser(int? userId)
        {
            if (_currentUser.IsAuthenticated && _currentUser.UserId.HasValue)
            {
                return _currentUser.UserId.Value.ToString();
            }
            return userId is int id and > 0 ? id.ToString() : "system";
        }
    }
}
