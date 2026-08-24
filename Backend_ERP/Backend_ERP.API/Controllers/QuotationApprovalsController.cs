using ERP.API.Security;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
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

        public QuotationApprovalsController(IQuotationApprovalService service)
        {
            _service = service;
        }

        // ─── Queries ──────────────────────────────────────────────────────────

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
            _ = userId; // reserved for CRM user integration
            return Ok(await _service.GetAllAsync(new QuotationApprovalListQueryDto
            {
                Search = search,
                Status = status,
                Priority = priority,
                ApprovalLevel = approvalLevel,
                SalesPersonUserId = salesPersonUserId,
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
            return Ok(await _service.GetStatisticsAsync(cancellationToken));
        }

        [HttpGet("permissions")]
        public async Task<ActionResult<IReadOnlyList<string>>> Permissions([FromQuery] int? userId)
        {
            _ = userId;
            return Ok(await _service.GetPermissionsAsync());
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
            return row is null ? NotFound() : Ok(row);
        }

        [HttpGet("{id:int}/history")]
        public async Task<ActionResult<IReadOnlyList<QuotationApprovalHistoryDto>>> History(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
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
                var created = await _service.CreateAsync(request, ResolveActingUser(userId), cancellationToken);
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

        private static string ResolveActingUser(int? userId) =>
            userId is int id and > 0 ? id.ToString() : "system";
    }
}
