using ERP.API.Security;
using ERP.Application.Common.Security;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Enums;
using ERP.Shared.Security;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Route("api/sales-targets")]
    [ApiController]
    [RequirePermission(ErpPermissions.SalesTargets.View)]
    public class SalesTargetsController : ControllerBase
    {
        private readonly ISalesTargetService _service;
        private readonly ICurrentUser _currentUser;
        private readonly IErpAuthorizationService _authService;

        public SalesTargetsController(
            ISalesTargetService service,
            ICurrentUser currentUser,
            IErpAuthorizationService authService)
        {
            _service = service;
            _currentUser = currentUser;
            _authService = authService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<SalesTargetListItemDto>>> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? targetType,
            [FromQuery] string? targetCategory,
            [FromQuery] int? salesPersonUserId,
            [FromQuery] int? financialYear,
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
                SalesPersonUserId = effectiveSalesPersonUserId,
                FinancialYear = financialYear,
                DateFrom = dateFrom,
                DateTo = dateTo
            }, cancellationToken));
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

        [HttpGet("reports")]
        [HttpPost("reports")]
        public async Task<ActionResult<SalesTargetReportDto>> Reports(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? targetCategory,
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
                TargetCategory = targetCategory,
                FinancialYear = financialYear
            };
            return Ok(await _service.GetReportsAsync(filter, cancellationToken));
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
                return CreatedAtAction(nameof(GetById), new { id = created.Id, userId }, created);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpGet("permissions")]
        public async Task<ActionResult<IReadOnlyList<string>>> Permissions([FromQuery] int? userId)
        {
            _ = userId;
            return Ok(_currentUser.Permissions.ToList());
        }

        [HttpGet("lookups/salespersons")]
        public async Task<ActionResult<IReadOnlyList<SalesTargetLookupDto>>> LookupSalespersons(
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.LookupSalespersonsAsync(cancellationToken));
        }

        [HttpGet("lookups/teams")]
        public async Task<ActionResult<IReadOnlyList<SalesTargetLookupDto>>> LookupTeams(
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.LookupTeamsAsync(cancellationToken));
        }

        [HttpGet("lookups/branches")]
        public async Task<ActionResult<IReadOnlyList<SalesTargetLookupDto>>> LookupBranches(
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.LookupBranchesAsync(cancellationToken));
        }

        [HttpGet("lookups/regional-managers")]
        public async Task<ActionResult<IReadOnlyList<SalesTargetLookupDto>>> LookupRegionalManagers(
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.LookupRegionalManagersAsync(cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<SalesTargetDto>> GetById(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var row = await _service.GetByIdAsync(id, cancellationToken);
            if (row is null) return NotFound();

            if (!_authService.CanAccessRecord(row.SalesPersonUserId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not have access to this sales target." });
            }

            return Ok(row);
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
                return CreatedAtAction(nameof(GetById), new { id = created.Id, userId }, created);
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
        public async Task<IActionResult> Delete(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var ok = await _service.DeleteAsync(id, ResolveActingUser(userId), cancellationToken);
                return ok ? NoContent() : NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("{id:int}/duplicate")]
        [RequirePermission(ErpPermissions.SalesTargets.Duplicate)]
        public async Task<ActionResult<SalesTargetDto>> Duplicate(
            int id,
            [FromBody] SalesTargetDuplicateRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var created = await _service.DuplicateAsync(
                    id, request, ResolveActingUser(userId), cancellationToken);
                return created is null ? NotFound() : Ok(created);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("{id:int}/activate")]
        [RequirePermission(ErpPermissions.SalesTargets.Edit)]
        public async Task<ActionResult<SalesTargetDto>> Activate(
            int id,
            [FromBody] SalesTargetRemarksRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            return await Workflow(id, request, userId, _service.ActivateAsync, cancellationToken);
        }

        [HttpPost("{id:int}/deactivate")]
        [RequirePermission(ErpPermissions.SalesTargets.Edit)]
        public async Task<ActionResult<SalesTargetDto>> Deactivate(
            int id,
            [FromBody] SalesTargetRemarksRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            return await Workflow(id, request, userId, _service.DeactivateAsync, cancellationToken);
        }

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
                var updated = await _service.UpdateStatusAsync(
                    id, request, ResolveActingUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("{id:int}/progress")]
        public async Task<ActionResult<SalesTargetDto>> UpdateProgress(
            int id,
            [FromBody] SalesTargetProgressUpdateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var updated = await _service.UpdateProgressAsync(
                    id, request, ResolveActingUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpGet("{id:int}/progress")]
        public async Task<ActionResult<IReadOnlyList<SalesTargetProgressDto>>> Progress(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var rows = await _service.GetProgressAsync(id, cancellationToken);
            return rows is null ? NotFound() : Ok(rows);
        }

        [HttpGet("{id:int}/history")]
        public async Task<ActionResult<IReadOnlyList<SalesTargetHistoryDto>>> History(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var rows = await _service.GetHistoryAsync(id, cancellationToken);
            return rows is null ? NotFound() : Ok(rows);
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
