using ERP.API.Security;
using ERP.Application.Common.Security;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Enums;
using ERP.Shared.Security;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Route("api/proforma-invoices")]
    [ApiController]
    [RequirePermission(ErpPermissions.ProformaInvoices.View)]
    public class ProformaInvoicesController : ControllerBase
    {
        private readonly IProformaInvoiceService _service;
        private readonly ICurrentUser _currentUser;
        private readonly IErpAuthorizationService _authService;
        private readonly IErpWorkflowAuthorizationService _workflowAuthService;

        public ProformaInvoicesController(
            IProformaInvoiceService service,
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
        public async Task<ActionResult<IReadOnlyList<ProformaInvoiceListItemDto>>> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? customer,
            [FromQuery] string? salesPerson,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var rows = await _service.GetAllAsync(new ProformaInvoiceListQueryDto
            {
                Search = search,
                Status = status,
                Customer = customer,
                SalesPerson = salesPerson,
                DateFrom = dateFrom,
                DateTo = dateTo
            }, cancellationToken);

            if (_currentUser.Scope == AccessScope.Own)
            {
                rows = rows.Where(x => _authService.CanAccessRecord(x.SalesPerson)
                    || (_currentUser.UserId.HasValue && string.Equals(x.SalesPerson, _currentUser.UserId.Value.ToString(), StringComparison.OrdinalIgnoreCase))
                    || (_currentUser.FullName != null && string.Equals(x.SalesPerson, _currentUser.FullName, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            return Ok(rows);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProformaInvoiceDto>> GetById(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var row = await _service.GetByIdAsync(id, cancellationToken);
            if (row is null) return NotFound();

            if (!CanAccessPi(row))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not have access to this proforma invoice." });
            }

            return Ok(row);
        }

        [HttpPost]
        [RequirePermission(ErpPermissions.ProformaInvoices.Create)]
        public async Task<ActionResult<ProformaInvoiceDto>> Create(
            [FromBody] ProformaInvoiceCreateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                if (_currentUser.Scope == AccessScope.Own && _currentUser.UserId.HasValue)
                {
                    request.SalesPersonId = _currentUser.UserId.Value.ToString();
                    if (_currentUser.FullName != null)
                    {
                        request.SalesPerson = _currentUser.FullName;
                    }
                }

                var created = await _service.CreateAsync(request, ResolveActingUser(userId), cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.Id, userId }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        [RequirePermission(ErpPermissions.ProformaInvoices.Edit)]
        public async Task<ActionResult<ProformaInvoiceDto>> Update(
            int id,
            [FromBody] ProformaInvoiceUpdateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var existing = await _service.GetByIdAsync(id, cancellationToken);
                if (existing is null) return NotFound();

                if (!CanAccessPi(existing) || !_workflowAuthService.CanEditProformaInvoice(existing.Status, existing.SalesPersonId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot modify this proforma invoice in its current workflow state." });
                }

                var updated = await _service.UpdateAsync(id, request, ResolveActingUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        [RequirePermission(ErpPermissions.ProformaInvoices.Edit)]
        public async Task<IActionResult> Delete(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var existing = await _service.GetByIdAsync(id, cancellationToken);
                if (existing is null) return NotFound();

                if (!CanAccessPi(existing))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot delete another user's proforma invoice." });
                }

                var ok = await _service.DeleteAsync(id, ResolveActingUser(userId), cancellationToken);
                return ok ? NoContent() : NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/duplicate")]
        [RequirePermission(ErpPermissions.ProformaInvoices.Create)]
        public async Task<ActionResult<ProformaInvoiceDto>> Duplicate(
            int id,
            [FromBody] ProformaInvoiceCreateRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var existing = await _service.GetByIdAsync(id, cancellationToken);
                if (existing is null) return NotFound();

                if (!CanAccessPi(existing))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot duplicate another user's proforma invoice." });
                }

                var created = await _service.DuplicateAsync(id, ResolveActingUser(userId), cancellationToken);
                return created is null ? NotFound() : Ok(created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/status")]
        [RequirePermission(ErpPermissions.ProformaInvoices.Edit)]
        public async Task<ActionResult<ProformaInvoiceDto>> UpdateStatus(
            int id,
            [FromBody] ProformaInvoiceStatusUpdateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var existing = await _service.GetByIdAsync(id, cancellationToken);
                if (existing is null) return NotFound();

                if (!CanAccessPi(existing))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot modify another user's proforma invoice." });
                }

                var updated = await _service.UpdateStatusAsync(id, request, ResolveActingUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/approval")]
        [RequirePermission(ErpPermissions.ProformaInvoices.Approve)]
        public async Task<ActionResult<ProformaInvoiceDto>> Approval(
            int id,
            [FromBody] ProformaInvoiceApprovalRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var existing = await _service.GetByIdAsync(id, cancellationToken);
                if (existing is null) return NotFound();

                if (!_workflowAuthService.CanApproveProformaInvoice(existing.Status, existing.SalesPersonId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot approve this proforma invoice in its current workflow state." });
                }

                var updated = await _service.ApplyApprovalAsync(id, request, ResolveActingUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/convert")]
        [RequirePermission(ErpPermissions.ProformaInvoices.Convert)]
        public async Task<ActionResult<ProformaInvoiceDto>> Convert(
            int id,
            [FromBody] ProformaInvoiceConvertRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var existing = await _service.GetByIdAsync(id, cancellationToken);
                if (existing is null) return NotFound();

                if (!_workflowAuthService.CanConvertProformaInvoice(existing.Status, existing.Conversion != null))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot convert this proforma invoice in its current workflow state." });
                }

                var updated = await _service.ConvertAsync(
                    id,
                    request ?? new ProformaInvoiceConvertRequestDto(),
                    ResolveActingUser(userId),
                    cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("from-sales-order/{salesOrderId:int}")]
        [RequirePermission(ErpPermissions.ProformaInvoices.Create)]
        public async Task<ActionResult<ProformaInvoiceDto>> GenerateFromSalesOrder(
            int salesOrderId,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var created = await _service.GenerateFromSalesOrderAsync(
                    salesOrderId,
                    ResolveActingUser(userId),
                    cancellationToken);
                return Ok(created);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpGet("{id:int}/status-history")]
        public async Task<ActionResult<IReadOnlyList<ProformaInvoiceStatusHistoryDto>>> StatusHistory(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var existing = await _service.GetByIdAsync(id, cancellationToken);
            if (existing is null) return NotFound();

            if (!CanAccessPi(existing))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot view history for another user's proforma invoice." });
            }

            var rows = await _service.GetStatusHistoryAsync(id, cancellationToken);
            return rows is null ? NotFound() : Ok(rows);
        }

        [HttpGet("{id:int}/approval-history")]
        public async Task<ActionResult<IReadOnlyList<ProformaInvoiceApprovalHistoryDto>>> ApprovalHistory(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var existing = await _service.GetByIdAsync(id, cancellationToken);
            if (existing is null) return NotFound();

            if (!CanAccessPi(existing))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot view history for another user's proforma invoice." });
            }

            var rows = await _service.GetApprovalHistoryAsync(id, cancellationToken);
            return rows is null ? NotFound() : Ok(rows);
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<ProformaDashboardDto>> Dashboard(
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.GetDashboardAsync(cancellationToken));
        }

        [HttpGet("reports")]
        public async Task<ActionResult<ProformaReportResultDto>> Reports(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.GetReportsAsync(new ProformaInvoiceListQueryDto
            {
                Search = search,
                Status = status,
                DateFrom = dateFrom,
                DateTo = dateTo
            }, cancellationToken));
        }

        [HttpPost("reports/export")]
        [RequirePermission(ErpPermissions.ProformaInvoices.Approve)]
        public async Task<ActionResult<ProformaExportMetadataDto>> ExportReports(
            [FromBody] ProformaExportRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.ExportReportsAsync(request, cancellationToken));
        }

        [HttpPost("{id:int}/pdf")]
        [RequirePermission(ErpPermissions.ProformaInvoices.GeneratePdf)]
        public async Task<ActionResult<ProformaPdfResultDto>> Pdf(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var existing = await _service.GetByIdAsync(id, cancellationToken);
            if (existing is null) return NotFound();

            if (!CanAccessPi(existing))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot generate PDF for another user's proforma invoice." });
            }

            var result = await _service.GeneratePdfAsync(id, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpPost("{id:int}/email")]
        [RequirePermission(ErpPermissions.ProformaInvoices.Email)]
        public async Task<ActionResult<ProformaEmailResultDto>> Email(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var existing = await _service.GetByIdAsync(id, cancellationToken);
            if (existing is null) return NotFound();

            if (!CanAccessPi(existing))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot send email for another user's proforma invoice." });
            }

            var result = await _service.SendEmailAsync(id, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpGet("permissions")]
        public async Task<ActionResult<IReadOnlyList<string>>> Permissions([FromQuery] int? userId)
        {
            _ = userId;
            return Ok(_currentUser.Permissions.ToList());
        }

        [HttpGet("lookups/customers")]
        public async Task<ActionResult<IReadOnlyList<ProformaLookupCustomerDto>>> LookupCustomers(
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.LookupCustomersAsync(cancellationToken));
        }

        [HttpGet("lookups/sales-orders")]
        public async Task<ActionResult<IReadOnlyList<ProformaLookupSalesOrderDto>>> LookupSalesOrders(
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.LookupSalesOrdersAsync(cancellationToken));
        }

        [HttpGet("lookups/quotations")]
        public async Task<ActionResult<IReadOnlyList<ProformaLookupQuotationDto>>> LookupQuotations(
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.LookupQuotationsAsync(cancellationToken));
        }

        private bool CanAccessPi(ProformaInvoiceDto row)
        {
            if (int.TryParse(row.SalesPersonId, out var id) && _authService.CanAccessRecord(id)) return true;
            if (_authService.CanAccessRecord(row.CreatedBy)) return true;
            if (_authService.CanAccessRecord(row.SalesPerson)) return true;
            return false;
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
