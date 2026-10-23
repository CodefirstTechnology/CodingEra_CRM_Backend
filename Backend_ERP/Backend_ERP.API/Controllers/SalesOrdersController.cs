using ERP.API.Security;
using ERP.Application.Common.Security;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Enums;
using ERP.Domain.Sales;
using ERP.Shared.Security;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Route("api/sales-orders")]
    [ApiController]
    [RequirePermission(ErpPermissions.SalesOrders.View)]
    public class SalesOrdersController : ControllerBase
    {
        private readonly ISalesOrderService _salesOrders;
        private readonly IProformaInvoiceService _proformaInvoices;
        private readonly IAdvancePaymentService _advancePayments;
        private readonly ERP.Application.Sales.Quotations.IStockDecrementService _stockDecrementService;
        private readonly ICurrentUser _currentUser;
        private readonly IErpAuthorizationService _authService;
        private readonly IErpWorkflowAuthorizationService _workflowAuthService;

        public SalesOrdersController(
            ISalesOrderService salesOrders,
            IProformaInvoiceService proformaInvoices,
            IAdvancePaymentService advancePayments,
            ERP.Application.Sales.Quotations.IStockDecrementService stockDecrementService,
            ICurrentUser currentUser,
            IErpAuthorizationService authService,
            IErpWorkflowAuthorizationService workflowAuthService)
        {
            _salesOrders = salesOrders;
            _proformaInvoices = proformaInvoices;
            _advancePayments = advancePayments;
            _stockDecrementService = stockDecrementService;
            _currentUser = currentUser;
            _authService = authService;
            _workflowAuthService = workflowAuthService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<SalesOrderListItemDto>>> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var rows = await _salesOrders.GetAllAsync(
                new SalesOrderListQueryDto
                {
                    Search = search,
                    Status = status,
                    DateFrom = dateFrom,
                    DateTo = dateTo
                },
                cancellationToken);

            if (_currentUser.Scope == AccessScope.Own)
            {
                rows = rows.Where(x => _authService.CanAccessRecord(x.CreatedBy)).ToList();
            }

            return Ok(rows);
        }

        [HttpGet("permissions")]
        public async Task<ActionResult<IReadOnlyList<string>>> Permissions([FromQuery] int? userId)
        {
            _ = userId;
            return Ok(_currentUser.Permissions.ToList());
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<SalesOrderDto>> GetById(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var row = await _salesOrders.GetByIdAsync(id, cancellationToken);
            if (row is null)
            {
                return NotFound();
            }

            if (!_authService.CanAccessRecord(row.CreatedBy) && !(_currentUser.FullName != null && string.Equals(row.SalesPerson, _currentUser.FullName, StringComparison.OrdinalIgnoreCase)))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not have access to this sales order." });
            }

            return Ok(row);
        }

        [HttpPost]
        [RequirePermission(ErpPermissions.SalesOrders.Create)]
        public async Task<ActionResult<SalesOrderDto>> Create(
            [FromBody] SalesOrderCreateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                if (_currentUser.Scope == AccessScope.Own)
                {
                    if (_currentUser.FullName != null) request.SalesPerson = _currentUser.FullName;
                }

                var created = await _salesOrders.CreateAsync(
                    request,
                    ResolveActingUser(userId),
                    cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.Id, userId }, created);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPut("{id:int}")]
        [RequirePermission(ErpPermissions.SalesOrders.Edit)]
        public async Task<ActionResult<SalesOrderDto>> Update(
            int id,
            [FromBody] SalesOrderUpdateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var existing = await _salesOrders.GetByIdAsync(id, cancellationToken);
                if (existing is null) return NotFound();

                if (!_workflowAuthService.CanEditSalesOrder(existing.Status, existing.CreatedBy))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot modify this sales order in its current workflow state." });
                }

                var updated = await _salesOrders.UpdateAsync(
                    id,
                    request,
                    ResolveActingUser(userId),
                    cancellationToken);
                if (updated is null)
                {
                    return NotFound();
                }

                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("{id:int}/status")]
        [RequirePermission(
            ErpPermissions.SalesOrders.Confirm,
            ErpPermissions.SalesOrders.Submit,
            ErpPermissions.SalesOrders.Process,
            ErpPermissions.SalesOrders.Complete,
            ErpPermissions.SalesOrders.Cancel,
            ErpPermissions.SalesOrders.Edit)]
        public async Task<ActionResult<SalesOrderDto>> UpdateStatus(
            int id,
            [FromBody] SalesOrderStatusUpdateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var existing = await _salesOrders.GetByIdAsync(id, cancellationToken);
                if (existing is null) return NotFound();

                var target = SalesOrderStatusRules.Normalize(request?.Status ?? string.Empty);
                if (target is null)
                {
                    return ValidationProblem(detail: $"Unknown status '{request?.Status}'.", statusCode: StatusCodes.Status400BadRequest);
                }

                if (!SalesOrderStatusRules.CanTransition(existing.Status, target))
                {
                    return ValidationProblem(detail: $"Cannot transition sales order from '{existing.Status}' to '{target}'.", statusCode: StatusCodes.Status400BadRequest);
                }

                bool authorized = target switch
                {
                    SalesOrderStatuses.Submitted => _workflowAuthService.CanSubmitSalesOrder(existing.Status, existing.CreatedBy),
                    SalesOrderStatuses.Confirmed => _workflowAuthService.CanConfirmSalesOrder(existing.Status, existing.CreatedBy),
                    SalesOrderStatuses.Processing => _workflowAuthService.CanProcessSalesOrder(existing.Status, existing.CreatedBy),
                    SalesOrderStatuses.PartiallyDelivered => _workflowAuthService.CanProcessSalesOrder(existing.Status, existing.CreatedBy) || _authService.Authorize(ErpPermissions.SalesOrders.Process),
                    SalesOrderStatuses.Completed => _workflowAuthService.CanCompleteSalesOrder(existing.Status, existing.CreatedBy),
                    SalesOrderStatuses.Cancelled => _workflowAuthService.CanCancelSalesOrder(existing.Status, existing.CreatedBy),
                    _ => false
                };

                if (!authorized && _currentUser.IsAdmin)
                {
                    authorized = true;
                }

                if (!authorized)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = $"You cannot change the status of this sales order to '{target}' in its current workflow state." });
                }

                var updated = await _salesOrders.UpdateStatusAsync(
                    id,
                    request!,
                    ResolveActingUser(userId),
                    cancellationToken);
                if (updated is null)
                {
                    return NotFound();
                }

                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("{id:int}/cancel")]
        [RequirePermission(ErpPermissions.SalesOrders.Cancel)]
        public async Task<ActionResult<SalesOrderDto>> Cancel(
            int id,
            [FromBody] SalesOrderCancelRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var existing = await _salesOrders.GetByIdAsync(id, cancellationToken);
                if (existing is null) return NotFound();

                if (!_workflowAuthService.CanCancelSalesOrder(existing.Status, existing.CreatedBy))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot cancel this sales order in its current workflow state." });
                }

                var updated = await _salesOrders.CancelAsync(
                    id,
                    request ?? new SalesOrderCancelRequestDto(),
                    ResolveActingUser(userId),
                    cancellationToken);
                if (updated is null)
                {
                    return NotFound();
                }

                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("convert-quotation/{quotationApprovalId:int}")]
        [RequirePermission(ErpPermissions.SalesOrders.Create)]
        public async Task<ActionResult<SalesOrderDto>> ConvertQuotation(
            int quotationApprovalId,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var created = await _salesOrders.ConvertQuotationAsync(
                    quotationApprovalId,
                    ResolveActingUser(userId),
                    cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.Id, userId }, created);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("{id:int}/proforma-invoice")]
        [RequirePermission(ErpPermissions.ProformaInvoices.Create)]
        public async Task<ActionResult<ProformaInvoiceDto>> GenerateProforma(
            int id,
            [FromBody] GenerateProformaInvoiceFromSoRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var existing = await _salesOrders.GetByIdAsync(id, cancellationToken);
                if (existing is null) return NotFound();

                if (!_workflowAuthService.CanCreateProformaFromSalesOrder(existing.Status, existing.CreatedBy))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot generate a proforma invoice for another user's sales order." });
                }

                var created = await _proformaInvoices.GenerateFromSalesOrderAsync(
                    id,
                    ResolveActingUser(userId),
                    request?.BillingType ?? "Full",
                    request?.MilestonePercentage,
                    cancellationToken);
                return Ok(created);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("{id:int}/advance-payments/apply")]
        [RequirePermission(ErpPermissions.AdvancePayments.Apply)]
        public async Task<ActionResult<AdvancePaymentDto>> ApplyAdvancePayment(
            int id,
            [FromBody] AdvancePaymentApplyRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var existing = await _salesOrders.GetByIdAsync(id, cancellationToken);
                if (existing is null) return NotFound();

                if (!_authService.CanAccessRecord(existing.CreatedBy))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot apply advance payment to another user's sales order." });
                }

                request.SalesOrderId = id;
                var updated = await _advancePayments.ApplyAsync(
                    request.AdvancePaymentId > 0 ? request.AdvancePaymentId : id,
                    request,
                    ResolveActingUser(userId),
                    cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("{id:int}/pdf")]
        [RequirePermission(ErpPermissions.SalesOrders.GeneratePdf)]
        public async Task<ActionResult<SalesOrderPdfResultDto>> GeneratePdf(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var existing = await _salesOrders.GetByIdAsync(id, cancellationToken);
            if (existing is null) return NotFound();

            if (!_authService.CanAccessRecord(existing.CreatedBy))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot generate PDF for another user's sales order." });
            }

            var result = await _salesOrders.GeneratePdfAsync(id, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpPost("{id:int}/email")]
        [RequirePermission(ErpPermissions.SalesOrders.SendEmail)]
        public async Task<ActionResult<SalesOrderEmailResultDto>> SendEmail(
            int id,
            [FromBody] SalesOrderEmailRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var existing = await _salesOrders.GetByIdAsync(id, cancellationToken);
                if (existing is null) return NotFound();

                if (!_authService.CanAccessRecord(existing.CreatedBy))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot send email for another user's sales order." });
                }

                var result = await _salesOrders.SendEmailAsync(
                    id,
                    request,
                    ResolveActingUser(userId),
                    cancellationToken);
                return result is null ? NotFound() : Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpGet("{id:int}/status-history")]
        [RequirePermission(ErpPermissions.SalesOrders.AuditView)]
        public async Task<ActionResult<IReadOnlyList<SalesOrderStatusHistoryDto>>> GetStatusHistory(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var existing = await _salesOrders.GetByIdAsync(id, cancellationToken);
            if (existing is null) return NotFound();

            if (!_authService.CanAccessRecord(existing.CreatedBy))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot view history for another user's sales order." });
            }

            var history = await _salesOrders.GetStatusHistoryAsync(id, cancellationToken);
            if (history is null)
            {
                return NotFound();
            }

            return Ok(history);
        }

        /// <summary>
        /// Stage 3 Dispatch/Completion: Decrements physical stock from finished_goods, releases reserved stock,
        /// and records double-entry stock transactions under row-level pessimistic locks.
        /// </summary>
        [HttpPost("{id:int}/complete")]
        [RequirePermission(ErpPermissions.SalesOrders.Complete)]
        public async Task<ActionResult<SalesOrderDto>> CompleteOrder(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var existing = await _salesOrders.GetByIdAsync(id, cancellationToken);
                if (existing is null) return NotFound();

                var actingUser = ResolveActingUser(userId);
                var result = await _stockDecrementService.DeductPhysicalStockForSalesOrderAsync(id, actingUser, cancellationToken);
                return Ok(result);
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
