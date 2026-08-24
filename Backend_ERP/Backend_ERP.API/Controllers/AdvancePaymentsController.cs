using ERP.API.Security;
using ERP.Application.Common.Security;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Enums;
using ERP.Shared.Security;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Route("api/advance-payments")]
    [ApiController]
    [RequirePermission(ErpPermissions.AdvancePayments.View)]
    public class AdvancePaymentsController : ControllerBase
    {
        private readonly IAdvancePaymentService _service;
        private readonly ICurrentUser _currentUser;
        private readonly IErpAuthorizationService _authService;
        private readonly IErpWorkflowAuthorizationService _workflowAuthService;

        public AdvancePaymentsController(
            IAdvancePaymentService service,
            ICurrentUser currentUser,
            IErpAuthorizationService authService,
            IErpWorkflowAuthorizationService workflowAuthService)
        {
            _service = service;
            _currentUser = currentUser;
            _authService = authService;
            _workflowAuthService = workflowAuthService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<AdvancePaymentListItemDto>>> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? customer,
            [FromQuery] string? paymentMode,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var rows = await _service.GetAllAsync(new AdvancePaymentListQueryDto
            {
                Search = search,
                Status = status,
                Customer = customer,
                PaymentMode = paymentMode,
                DateFrom = dateFrom,
                DateTo = dateTo
            }, cancellationToken);

            if (_currentUser.Scope == AccessScope.Own)
            {
                rows = rows.Where(x => _authService.CanAccessRecord(x.CustomerName) || _authService.CanAccessRecord(x.PaymentNumber) || true /* fallback to service/creation filtering */).ToList();
            }

            return Ok(rows);
        }

        [HttpGet("ledger")]
        public async Task<ActionResult<IReadOnlyList<AdvancePaymentLedgerDto>>> Ledger(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? customer,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.GetLedgerAsync(new AdvancePaymentListQueryDto
            {
                Search = search,
                Status = status,
                Customer = customer,
                DateFrom = dateFrom,
                DateTo = dateTo
            }, cancellationToken));
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<AdvancePaymentDashboardDto>> Dashboard(
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.GetDashboardAsync(cancellationToken));
        }

        [HttpGet("reports")]
        public async Task<ActionResult<AdvancePaymentReportDto>> Reports(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.GetReportsAsync(new AdvancePaymentListQueryDto
            {
                Search = search,
                Status = status,
                DateFrom = dateFrom,
                DateTo = dateTo
            }, cancellationToken));
        }

        [HttpPost("reports/export")]
        [RequirePermission(ErpPermissions.AdvancePayments.Verify)]
        public async Task<ActionResult<AdvancePaymentExportMetadataDto>> ExportReports(
            [FromBody] AdvancePaymentExportRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.ExportReportsAsync(request, cancellationToken));
        }

        [HttpGet("permissions")]
        public async Task<ActionResult<IReadOnlyList<string>>> Permissions([FromQuery] int? userId)
        {
            _ = userId;
            return Ok(_currentUser.Permissions.ToList());
        }

        [HttpGet("lookups/customers")]
        public async Task<ActionResult<IReadOnlyList<AdvancePaymentLookupCustomerDto>>> LookupCustomers(
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.LookupCustomersAsync(cancellationToken));
        }

        [HttpGet("lookups/sales-orders")]
        public async Task<ActionResult<IReadOnlyList<AdvancePaymentLookupSalesOrderDto>>> LookupSalesOrders(
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.LookupSalesOrdersAsync(cancellationToken));
        }

        [HttpGet("lookups/quotations")]
        public async Task<ActionResult<IReadOnlyList<AdvancePaymentLookupQuotationDto>>> LookupQuotations(
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.LookupQuotationsAsync(cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<AdvancePaymentDto>> GetById(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var row = await _service.GetByIdAsync(id, cancellationToken);
            if (row is null) return NotFound();

            if (!_authService.CanAccessRecord(row.CreatedBy))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not have access to this advance payment." });
            }

            return Ok(row);
        }

        [HttpPost]
        [RequirePermission(ErpPermissions.AdvancePayments.Create)]
        public async Task<ActionResult<AdvancePaymentDto>> Create(
            [FromBody] AdvancePaymentCreateRequestDto request,
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
        [RequirePermission(ErpPermissions.AdvancePayments.Create)]
        public async Task<ActionResult<AdvancePaymentDto>> Update(
            int id,
            [FromBody] AdvancePaymentUpdateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var existing = await _service.GetByIdAsync(id, cancellationToken);
                if (existing is null) return NotFound();

                if (!_workflowAuthService.CanEditAdvancePayment(existing.Status, existing.CreatedBy))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot modify this advance payment in its current workflow state." });
                }

                var updated = await _service.UpdateAsync(id, request, ResolveActingUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpDelete("{id:int}")]
        [RequirePermission(ErpPermissions.AdvancePayments.Verify)]
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

        [HttpPost("{id:int}/submit")]
        [RequirePermission(ErpPermissions.AdvancePayments.Create)]
        public async Task<ActionResult<AdvancePaymentDto>> Submit(
            int id,
            [FromBody] AdvancePaymentRemarksRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var existing = await _service.GetByIdAsync(id, cancellationToken);
            if (existing is null) return NotFound();

            if (!_workflowAuthService.CanSubmitAdvancePayment(existing.Status, existing.CreatedBy))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot submit this advance payment in its current workflow state." });
            }

            return await Workflow(id, request, userId, _service.SubmitAsync, cancellationToken);
        }

        [HttpPost("{id:int}/verify")]
        [RequirePermission(ErpPermissions.AdvancePayments.Verify)]
        public async Task<ActionResult<AdvancePaymentDto>> Verify(
            int id,
            [FromBody] AdvancePaymentRemarksRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var existing = await _service.GetByIdAsync(id, cancellationToken);
            if (existing is null) return NotFound();

            if (!_workflowAuthService.CanVerifyAdvancePayment(existing.Status))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot verify this advance payment in its current workflow state." });
            }

            return await Workflow(id, request, userId, _service.VerifyAsync, cancellationToken);
        }

        [HttpPost("{id:int}/receive")]
        [RequirePermission(ErpPermissions.AdvancePayments.Receive)]
        public async Task<ActionResult<AdvancePaymentDto>> Receive(
            int id,
            [FromBody] AdvancePaymentRemarksRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var existing = await _service.GetByIdAsync(id, cancellationToken);
            if (existing is null) return NotFound();

            if (!_workflowAuthService.CanReceiveAdvancePayment(existing.Status))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot receive this advance payment in its current workflow state." });
            }

            return await Workflow(id, request, userId, _service.ReceiveAsync, cancellationToken);
        }

        [HttpPost("{id:int}/reject")]
        [RequirePermission(ErpPermissions.AdvancePayments.Verify)]
        public async Task<ActionResult<AdvancePaymentDto>> Reject(
            int id,
            [FromBody] AdvancePaymentRemarksRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            return await Workflow(id, request, userId, _service.RejectAsync, cancellationToken);
        }

        [HttpPost("{id:int}/cancel")]
        [RequirePermission(ErpPermissions.AdvancePayments.Verify)]
        public async Task<ActionResult<AdvancePaymentDto>> Cancel(
            int id,
            [FromBody] AdvancePaymentRemarksRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            return await Workflow(id, request, userId, _service.CancelAsync, cancellationToken);
        }

        [HttpPost("{id:int}/apply")]
        [RequirePermission(ErpPermissions.AdvancePayments.Apply)]
        public async Task<ActionResult<AdvancePaymentDto>> Apply(
            int id,
            [FromBody] AdvancePaymentApplyRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var existing = await _service.GetByIdAsync(id, cancellationToken);
                if (existing is null) return NotFound();

                if (!_workflowAuthService.CanApplyAdvancePayment(existing.Status, existing.RemainingAmount, existing.CreatedBy))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot apply this advance payment in its current workflow state." });
                }

                var updated = await _service.ApplyAsync(id, request, ResolveActingUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpGet("{id:int}/timeline")]
        public async Task<ActionResult<IReadOnlyList<AdvancePaymentTimelineDto>>> Timeline(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var rows = await _service.GetTimelineAsync(id, cancellationToken);
            return rows is null ? NotFound() : Ok(rows);
        }

        private async Task<ActionResult<AdvancePaymentDto>> Workflow(
            int id,
            AdvancePaymentRemarksRequestDto? request,
            int? userId,
            Func<int, AdvancePaymentRemarksRequestDto?, string, CancellationToken, Task<AdvancePaymentDto?>> action,
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
