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
        private readonly ISalesOrderAllocationService _allocationService;
        private readonly ICustomerCreditAssessmentService _creditService;
        private readonly ISalesOrderAdvanceGateService _advanceGateService;
        private readonly IMtoProductionTriggerService _mtoService;
        private readonly IProformaInvoiceService _proformaInvoices;
        private readonly IAdvancePaymentService _advancePayments;
        private readonly ERP.Application.Sales.Quotations.IStockDecrementService _stockDecrementService;
        private readonly ICurrentUser _currentUser;
        private readonly IErpAuthorizationService _authService;
        private readonly IErpWorkflowAuthorizationService _workflowAuthService;

        public SalesOrdersController(
            ISalesOrderService salesOrders,
            ISalesOrderAllocationService allocationService,
            ICustomerCreditAssessmentService creditService,
            ISalesOrderAdvanceGateService advanceGateService,
            IMtoProductionTriggerService mtoService,
            IProformaInvoiceService proformaInvoices,
            IAdvancePaymentService advancePayments,
            ERP.Application.Sales.Quotations.IStockDecrementService stockDecrementService,
            ICurrentUser currentUser,
            IErpAuthorizationService authService,
            IErpWorkflowAuthorizationService workflowAuthService)
        {
            _salesOrders = salesOrders;
            _allocationService = allocationService;
            _creditService = creditService;
            _advanceGateService = advanceGateService;
            _mtoService = mtoService;
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

                // Stage 3 Inventory Deduction: When Sales Order completes, automatically decrement physical finished goods stock
                if (string.Equals(target, SalesOrderStatuses.Completed, StringComparison.OrdinalIgnoreCase))
                {
                    await _stockDecrementService.DeductPhysicalStockForSalesOrderAsync(
                        id,
                        ResolveActingUser(userId),
                        cancellationToken);

                    updated = await _salesOrders.GetByIdAsync(id, cancellationToken);
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

        [HttpPost("{id:int}/approve")]
        [RequirePermission(ErpPermissions.SalesOrders.Confirm, ErpPermissions.SalesOrders.Approve)]
        public async Task<ActionResult<SalesOrderDto>> ApproveOrder(
            int id,
            [FromQuery] int? userId,
            CancellationToken ct)
        {
            try
            {
                var existing = await _salesOrders.GetByIdAsync(id, ct);
                if (existing is null) return NotFound();

                var actingUser = ResolveActingUser(userId);

                // 1. Dynamic Credit Assessment
                var creditCheck = await _creditService.AssessCustomerExposureAsync(
                    existing.CustomerId,
                    existing.GrandTotal,
                    ct);

                if (creditCheck.IsOnHold)
                {
                    await _salesOrders.SetCreditHoldAsync(id, creditCheck.Reason, actingUser, ct);
                    return StatusCode(StatusCodes.Status422UnprocessableEntity, new
                    {
                        message = creditCheck.Reason,
                        isOnHold = true,
                        exposure = creditCheck.TotalExposure,
                        creditLimit = creditCheck.ApprovedCreditLimit
                    });
                }

                // 2. Transition Status to Confirmed
                var updated = await _salesOrders.UpdateStatusAsync(id,
                    new SalesOrderStatusUpdateRequestDto
                    {
                        Status = SalesOrderStatuses.Confirmed,
                        Remarks = "Credit exposure verified. Order confirmed."
                    },
                    actingUser,
                    ct);

                // 3. Execute Soft Inventory Allocation
                await _allocationService.AllocateOrderInventoryAsync(id, actingUser, ct);

                return Ok(await _salesOrders.GetByIdAsync(id, ct) ?? updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("{id:int}/override-credit")]
        [RequirePermission(ErpPermissions.SalesOrders.CreditOverride, ErpPermissions.SalesOrders.Approve)]
        public async Task<ActionResult<SalesOrderDto>> OverrideCredit(
            int id,
            [FromBody] CreditOverrideRequestDto request,
            [FromQuery] int? userId,
            CancellationToken ct)
        {
            try
            {
                var existing = await _salesOrders.GetByIdAsync(id, ct);
                if (existing is null) return NotFound();

                var actingUser = ResolveActingUser(userId);
                var justification = string.IsNullOrWhiteSpace(request?.Justification)
                    ? "Managerial finance credit override"
                    : request.Justification.Trim();

                var updated = await _salesOrders.OverrideCreditHoldAsync(id, justification, actingUser, ct);
                if (updated is null) return NotFound();

                // Allocate soft stock after credit override
                await _allocationService.AllocateOrderInventoryAsync(id, actingUser, ct);

                return Ok(await _salesOrders.GetByIdAsync(id, ct) ?? updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("{id:int}/release-production")]
        [RequirePermission(ErpPermissions.SalesOrders.Process)]
        public async Task<ActionResult<SalesOrderDto>> ReleaseToProduction(
            int id,
            [FromQuery] int? userId,
            CancellationToken ct)
        {
            try
            {
                var existing = await _salesOrders.GetByIdAsync(id, ct);
                if (existing is null) return NotFound();

                var gateCheck = await _advanceGateService.CanReleaseToProductionAsync(id, ct);
                if (gateCheck.IsLocked)
                {
                    return StatusCode(StatusCodes.Status422UnprocessableEntity, new
                    {
                        message = gateCheck.LockReason,
                        required = gateCheck.RequiredAmount,
                        allocated = gateCheck.AllocatedAmount
                    });
                }

                var actingUser = ResolveActingUser(userId);

                // Auto-evaluate Net Deficit & spawn MTO production plans & work orders
                await _mtoService.EvaluateAndSpawnProductionAsync(id, actingUser, ct);

                var updated = await _salesOrders.SetProductionReleasedAsync(id, actingUser, ct);
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("{id:int}/release-dispatch")]
        [RequirePermission(ErpPermissions.SalesOrders.Process)]
        public async Task<ActionResult<SalesOrderDto>> ReleaseToDispatch(
            int id,
            [FromQuery] int? userId,
            CancellationToken ct)
        {
            try
            {
                var existing = await _salesOrders.GetByIdAsync(id, ct);
                if (existing is null) return NotFound();

                var gateCheck = await _advanceGateService.CanReleaseToDispatchAsync(id, ct);
                if (gateCheck.IsLocked)
                {
                    return StatusCode(StatusCodes.Status422UnprocessableEntity, new
                    {
                        message = gateCheck.LockReason,
                        required = gateCheck.RequiredAmount,
                        allocated = gateCheck.AllocatedAmount
                    });
                }

                var actingUser = ResolveActingUser(userId);
                var updated = await _salesOrders.SetDispatchReleasedAsync(id, actingUser, ct);
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpGet("{id:int}/allocations")]
        public async Task<ActionResult<IReadOnlyList<SalesOrderAllocationDto>>> GetAllocations(
            int id,
            CancellationToken ct)
        {
            var allocations = await _allocationService.GetAllocationsForOrderAsync(id, ct);
            return Ok(allocations);
        }

        [HttpPost("{id:int}/split-warehouse")]
        [RequirePermission(ErpPermissions.SalesOrders.Edit)]
        public async Task<ActionResult<AllocationResultDto>> SplitWarehouse(
            int id,
            [FromBody] SplitAllocationRequestDto request,
            [FromQuery] int? userId,
            CancellationToken ct)
        {
            try
            {
                var actingUser = ResolveActingUser(userId);
                var result = await _allocationService.SplitWarehouseAllocationAsync(id, request, actingUser, ct);
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
