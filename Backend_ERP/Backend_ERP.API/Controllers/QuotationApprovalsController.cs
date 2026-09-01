using ERP.API.Security;
using ERP.Application.Common.Security;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Enums;
using ERP.Shared.Security;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [ApiController]
    [Route("api/quotation-approvals")]
    [RequirePermission(ErpPermissions.Quotations.View)]
    public class QuotationApprovalsController : ControllerBase
    {
        private readonly IQuotationApprovalService _service;
        private readonly ICurrentUser _currentUser;
        private readonly IErpAuthorizationService _authService;
        private readonly IErpWorkflowAuthorizationService _workflowAuthService;
        private readonly IQuotationApprovalNumberingService _numberingService;
        private readonly IStoreInventoryService _inventoryService;

        public QuotationApprovalsController(
            IQuotationApprovalService service,
            ICurrentUser currentUser,
            IErpAuthorizationService authService,
            IErpWorkflowAuthorizationService workflowAuthService,
            IQuotationApprovalNumberingService numberingService,
            IStoreInventoryService inventoryService)
        {
            _service = service;
            _currentUser = currentUser;
            _authService = authService;
            _workflowAuthService = workflowAuthService;
            _numberingService = numberingService;
            _inventoryService = inventoryService;
        }

        // ─── Queries ──────────────────────────────────────────────────────────

        [HttpGet("next-number")]
        public async Task<ActionResult<string>> GetNextNumber(CancellationToken cancellationToken)
        {
            var number = await _numberingService.GenerateNextApprovalNumberAsync(cancellationToken);
            return Ok(number);
        }

        /// <summary>Returns finished goods as quotation item lookup options (product picker).</summary>
        [HttpGet("lookups/finished-goods")]
        public async Task<ActionResult<IReadOnlyList<QuotationFinishedGoodLookupDto>>> LookupFinishedGoods(
            [FromQuery] string? search,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            var items = await _inventoryService.GetFinishedGoodsAsync(
                new StoreListQueryDto { Search = search, Page = 1, PageSize = pageSize },
                cancellationToken);

            var result = items.Select(x => new QuotationFinishedGoodLookupDto
            {
                Id = x.Id,
                ProductCode = x.ProductCode,
                ProductName = x.ProductName,
                AvailableQuantity = x.AvailableQuantity,
                Unit = "Nos",
                UnitCost = x.CurrentValue > 0 && x.FinishedQuantity > 0
                    ? Math.Round(x.CurrentValue / x.FinishedQuantity, 2)
                    : 0m,
                WarehouseName = x.WarehouseName,
                BatchNumber = x.BatchNumber,
            }).ToList();

            return Ok(result);
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<QuotationApprovalListItemDto>>> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? priority,
            [FromQuery] string? approvalLevel,
            [FromQuery] int? salesPersonUserId,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var effectiveSalesPersonUserId = (_currentUser.Scope == AccessScope.Own && _currentUser.UserId.HasValue)
                ? _currentUser.UserId.Value
                : salesPersonUserId;

            return Ok(await _service.GetAllAsync(new QuotationApprovalListQueryDto
            {
                Search = search,
                Status = status,
                Priority = priority,
                ApprovalLevel = approvalLevel,
                SalesPersonUserId = effectiveSalesPersonUserId,
                DateFrom = dateFrom,
                DateTo = dateTo
            }, cancellationToken));
        }

        [HttpGet("statistics")]
        public async Task<ActionResult<QuotationApprovalStatisticsDto>> Statistics(
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var stats = await _service.GetStatisticsAsync(cancellationToken);
            if (_currentUser.Scope == AccessScope.Own && _currentUser.UserId.HasValue)
            {
                var ownRows = await _service.GetAllAsync(new QuotationApprovalListQueryDto
                {
                    SalesPersonUserId = _currentUser.UserId.Value
                }, cancellationToken);

                return Ok(new QuotationApprovalStatisticsDto
                {
                    TotalCount = ownRows.Count,
                    PendingCount = ownRows.Count(x => x.Status == "Submitted" || x.Status == "Draft"),
                    UnderReviewCount = ownRows.Count(x => x.Status == "UnderReview"),
                    ApprovedCount = ownRows.Count(x => x.Status == "Approved"),
                    RejectedCount = ownRows.Count(x => x.Status == "Rejected"),
                    ReturnedCount = ownRows.Count(x => x.Status == "Returned"),
                    CancelledCount = ownRows.Count(x => x.Status == "Cancelled"),
                    TotalAmount = ownRows.Sum(x => x.TotalAmount),
                    Recent = ownRows.Take(10).ToList()
                });
            }

            return Ok(stats);
        }

        [HttpGet("permissions")]
        public async Task<ActionResult<IReadOnlyList<string>>> Permissions([FromQuery] int? userId)
        {
            _ = userId;
            return Ok(_currentUser.Permissions.ToList());
        }

        [HttpGet("lookups/sales-orders")]
        public async Task<ActionResult<IReadOnlyList<QuotationApprovalLookupDto>>> LookupSalesOrders(
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.LookupSalesOrdersAsync(cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<QuotationApprovalDto>> GetById(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var row = await _service.GetByIdAsync(id, cancellationToken);
            if (row is null) return NotFound();

            if (!_authService.CanAccessRecord(row.SalesPersonUserId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not have access to this quotation." });
            }

            return Ok(row);
        }

        [HttpGet("{id:int}/history")]
        public async Task<ActionResult<IReadOnlyList<QuotationApprovalHistoryDto>>> History(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var row = await _service.GetByIdAsync(id, cancellationToken);
            if (row is null) return NotFound();

            if (!_authService.CanAccessRecord(row.SalesPersonUserId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not have access to this quotation." });
            }

            var rows = await _service.GetHistoryAsync(id, cancellationToken);
            return rows is null ? NotFound() : Ok(rows);
        }

        [HttpGet("{id:int}/comments")]
        public async Task<ActionResult<IReadOnlyList<QuotationApprovalCommentDto>>> Comments(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var row = await _service.GetByIdAsync(id, cancellationToken);
            if (row is null) return NotFound();

            if (!_authService.CanAccessRecord(row.SalesPersonUserId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not have access to this quotation." });
            }

            var rows = await _service.GetCommentsAsync(id, cancellationToken);
            return rows is null ? NotFound() : Ok(rows);
        }

        // ─── Write ────────────────────────────────────────────────────────────

        [HttpPost]
        [RequirePermission(ErpPermissions.Quotations.Create)]
        public async Task<ActionResult<QuotationApprovalDto>> Create(
            [FromBody] QuotationApprovalCreateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                if (_currentUser.Scope == AccessScope.Own && _currentUser.UserId.HasValue)
                {
                    request.SalesPersonUserId = _currentUser.UserId.Value;
                }

                var actingUser = ResolveActingUser(userId);
                var created = await _service.CreateAsync(request, actingUser, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPut("{id:int}")]
        [RequirePermission(ErpPermissions.Quotations.Edit)]
        public async Task<ActionResult<QuotationApprovalDto>> Update(
            int id,
            [FromBody] QuotationApprovalUpdateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var existing = await _service.GetByIdAsync(id, cancellationToken);
                if (existing is null) return NotFound();

                if (!_workflowAuthService.CanEditQuotation(existing.Status, existing.SalesPersonUserId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot modify this quotation in its current workflow state." });
                }

                var updated = await _service.UpdateAsync(id, request, ResolveActingUser(userId), cancellationToken);
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpDelete("{id:int}")]
        [RequirePermission(ErpPermissions.Quotations.Delete)]
        public async Task<IActionResult> Delete(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var existing = await _service.GetByIdAsync(id, cancellationToken);
                if (existing is null) return NotFound();

                if (!_workflowAuthService.CanDeleteQuotation(existing.Status, existing.SalesPersonUserId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot delete this quotation in its current workflow state." });
                }

                var ok = await _service.DeleteAsync(id, ResolveActingUser(userId), cancellationToken);
                return ok ? NoContent() : NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("{id:int}/comments")]
        public async Task<ActionResult<QuotationApprovalCommentDto>> AddComment(
            int id,
            [FromBody] QuotationApprovalCommentRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var existing = await _service.GetByIdAsync(id, cancellationToken);
                if (existing is null) return NotFound();

                if (!_authService.CanAccessRecord(existing.SalesPersonUserId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot comment on another user's quotation." });
                }

                var row = await _service.AddCommentAsync(id, request, ResolveActingUser(userId), cancellationToken);
                return Ok(row);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        // ─── Workflow transitions ─────────────────────────────────────────────

        [HttpPost("{id:int}/submit")]
        [RequirePermission(ErpPermissions.Quotations.Submit)]
        public async Task<ActionResult<QuotationApprovalDto>> Submit(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var existing = await _service.GetByIdAsync(id, cancellationToken);
                if (existing is null) return NotFound();

                if (!_workflowAuthService.CanSubmitQuotation(existing.Status, existing.SalesPersonUserId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot submit this quotation in its current workflow state." });
                }

                var result = await _service.SubmitAsync(id, ResolveActingUser(userId), cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("{id:int}/review")]
        [RequirePermission(ErpPermissions.Quotations.Approve)]
        public async Task<ActionResult<QuotationApprovalDto>> Review(
            int id,
            [FromBody] QuotationApprovalDecisionRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var existing = await _service.GetByIdAsync(id, cancellationToken);
            if (existing is null) return NotFound();

            if (!_workflowAuthService.CanApproveQuotation(existing.Status, existing.SalesPersonUserId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot review this quotation in its current workflow state." });
            }

            return await Decision(id, request, userId, _service.ReviewAsync, cancellationToken);
        }

        [HttpPost("{id:int}/approve")]
        [RequirePermission(ErpPermissions.Quotations.Approve)]
        public async Task<ActionResult<QuotationApprovalDto>> Approve(
            int id,
            [FromBody] QuotationApprovalDecisionRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var existing = await _service.GetByIdAsync(id, cancellationToken);
            if (existing is null) return NotFound();

            if (!_workflowAuthService.CanApproveQuotation(existing.Status, existing.SalesPersonUserId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot approve this quotation in its current workflow state." });
            }

            return await Decision(id, request, userId, _service.ApproveAsync, cancellationToken);
        }

        [HttpPost("{id:int}/reject")]
        [RequirePermission(ErpPermissions.Quotations.Reject)]
        public async Task<ActionResult<QuotationApprovalDto>> Reject(
            int id,
            [FromBody] QuotationApprovalDecisionRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var existing = await _service.GetByIdAsync(id, cancellationToken);
            if (existing is null) return NotFound();

            if (!_workflowAuthService.CanRejectQuotation(existing.Status, existing.SalesPersonUserId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot reject this quotation in its current workflow state." });
            }

            return await Decision(id, request, userId, _service.RejectAsync, cancellationToken);
        }

        [HttpPost("{id:int}/return")]
        [RequirePermission(ErpPermissions.Quotations.Return)]
        public async Task<ActionResult<QuotationApprovalDto>> Return(
            int id,
            [FromBody] QuotationApprovalDecisionRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var existing = await _service.GetByIdAsync(id, cancellationToken);
            if (existing is null) return NotFound();

            if (!_workflowAuthService.CanReturnQuotation(existing.Status, existing.SalesPersonUserId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot return this quotation in its current workflow state." });
            }

            return await Decision(id, request, userId, _service.ReturnAsync, cancellationToken);
        }

        [HttpPost("{id:int}/cancel")]
        [RequirePermission(ErpPermissions.Quotations.Delete)]
        public async Task<ActionResult<QuotationApprovalDto>> Cancel(
            int id,
            [FromBody] QuotationApprovalDecisionRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            return await Decision(id, request, userId, _service.CancelAsync, cancellationToken);
        }

        [HttpPost("{id:int}/request-revision")]
        [RequirePermission(ErpPermissions.Quotations.Return)]
        public async Task<ActionResult<QuotationApprovalDto>> RequestRevision(
            int id,
            [FromBody] QuotationApprovalDecisionRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            return await Decision(id, request, userId, _service.RequestRevisionAsync, cancellationToken);
        }

        [HttpPost("{id:int}/reopen")]
        [RequirePermission(ErpPermissions.Quotations.Reopen)]
        public async Task<ActionResult<QuotationApprovalDto>> Reopen(
            int id,
            [FromBody] QuotationApprovalDecisionRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var existing = await _service.GetByIdAsync(id, cancellationToken);
            if (existing is null) return NotFound();

            if (!_workflowAuthService.CanReopenQuotation(existing.Status, existing.SalesPersonUserId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot reopen this quotation in its current workflow state." });
            }

            return await Decision(id, request, userId, _service.ReopenAsync, cancellationToken);
        }

        [HttpPost("{id:int}/convert")]
        [RequirePermission(ErpPermissions.Quotations.Convert)]
        public async Task<ActionResult<SalesOrderDto>> ConvertToSalesOrder(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var existing = await _service.GetByIdAsync(id, cancellationToken);
                if (existing is null) return NotFound();

                if (!_workflowAuthService.CanConvertQuotation(existing.Status, existing.SalesPersonUserId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot convert this quotation to a sales order in its current workflow state." });
                }

                var created = await _service.ConvertToSalesOrderAsync(id, ResolveActingUser(userId), cancellationToken);
                return Ok(created);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }


        // ─── Helpers ──────────────────────────────────────────────────────────

        private async Task<ActionResult<QuotationApprovalDto>> Decision(
            int id,
            QuotationApprovalDecisionRequestDto? request,
            int? userId,
            Func<int, QuotationApprovalDecisionRequestDto, string, CancellationToken, Task<QuotationApprovalDto>> action,
            CancellationToken cancellationToken)
        {
            try
            {
                var updated = await action(id, request ?? new(), ResolveActingUser(userId), cancellationToken);
                return Ok(updated);
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
