using ERP.API.Security;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Shared.Security;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Route("api/discount-approvals")]
    [ApiController]
    [RequirePermission(ErpPermissions.DiscountApprovals.View)]
    public class DiscountApprovalsController : ControllerBase
    {
        private readonly IDiscountApprovalService _service;

        public DiscountApprovalsController(IDiscountApprovalService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<DiscountApprovalListItemDto>>> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? priority,
            [FromQuery] string? approvalLevel,
            [FromQuery] string? sourceType,
            [FromQuery] string? customerCategory,
            [FromQuery] int? salesPersonUserId,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.GetAllAsync(new DiscountApprovalListQueryDto
            {
                Search = search,
                Status = status,
                Priority = priority,
                ApprovalLevel = approvalLevel,
                SourceType = sourceType,
                CustomerCategory = customerCategory,
                SalesPersonUserId = salesPersonUserId,
                DateFrom = dateFrom,
                DateTo = dateTo
            }, cancellationToken));
        }

        [HttpGet("statistics")]
        public async Task<ActionResult<DiscountApprovalStatisticsDto>> Statistics(
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

        [HttpGet("lookups/price-lists")]
        public async Task<ActionResult<IReadOnlyList<DiscountApprovalLookupDto>>> LookupPriceLists(
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.LookupPriceListsAsync(cancellationToken));
        }

        [HttpGet("lookups/sales-orders")]
        public async Task<ActionResult<IReadOnlyList<DiscountApprovalLookupDto>>> LookupSalesOrders(
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.LookupSalesOrdersAsync(cancellationToken));
        }

        [HttpGet("lookups/quotations")]
        public async Task<ActionResult<IReadOnlyList<DiscountApprovalLookupDto>>> LookupQuotations(
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.LookupQuotationsAsync(cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<DiscountApprovalDto>> GetById(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var row = await _service.GetByIdAsync(id, cancellationToken);
            return row is null ? NotFound() : Ok(row);
        }

        [HttpGet("{id:int}/history")]
        public async Task<ActionResult<IReadOnlyList<DiscountApprovalHistoryDto>>> History(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var rows = await _service.GetHistoryAsync(id, cancellationToken);
            return rows is null ? NotFound() : Ok(rows);
        }

        [HttpGet("{id:int}/comments")]
        public async Task<ActionResult<IReadOnlyList<DiscountApprovalCommentDto>>> Comments(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var rows = await _service.GetCommentsAsync(id, cancellationToken);
            return rows is null ? NotFound() : Ok(rows);
        }

        [HttpPost("{id:int}/comments")]
        public async Task<ActionResult<DiscountApprovalCommentDto>> AddComment(
            int id,
            [FromBody] DiscountApprovalCommentRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var row = await _service.AddCommentAsync(
                    id, request, ResolveActingUser(userId), cancellationToken);
                return row is null ? NotFound() : Ok(row);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost]
        [RequirePermission(ErpPermissions.DiscountApprovals.Create)]
        public async Task<ActionResult<DiscountApprovalDto>> Create(
            [FromBody] DiscountApprovalCreateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var created = await _service.CreateAsync(
                    request, ResolveActingUser(userId), cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPut("{id:int}")]
        [RequirePermission(ErpPermissions.DiscountApprovals.Create)]
        public async Task<ActionResult<DiscountApprovalDto>> Update(
            int id,
            [FromBody] DiscountApprovalUpdateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var updated = await _service.UpdateAsync(
                    id, request, ResolveActingUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpDelete("{id:int}")]
        [RequirePermission(ErpPermissions.DiscountApprovals.Cancel)]
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

        [HttpPost("{id:int}/approve")]
        [RequirePermission(ErpPermissions.DiscountApprovals.Approve)]
        public async Task<ActionResult<DiscountApprovalDto>> Approve(
            int id,
            [FromBody] DiscountApprovalDecisionRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            return await Decision(id, request, userId, _service.ApproveAsync, cancellationToken);
        }

        [HttpPost("{id:int}/reject")]
        [RequirePermission(ErpPermissions.DiscountApprovals.Reject)]
        public async Task<ActionResult<DiscountApprovalDto>> Reject(
            int id,
            [FromBody] DiscountApprovalDecisionRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            return await Decision(id, request, userId, _service.RejectAsync, cancellationToken);
        }

        [HttpPost("{id:int}/return")]
        [RequirePermission(ErpPermissions.DiscountApprovals.Return)]
        public async Task<ActionResult<DiscountApprovalDto>> Return(
            int id,
            [FromBody] DiscountApprovalDecisionRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            return await Decision(id, request, userId, _service.ReturnAsync, cancellationToken);
        }

        [HttpPost("{id:int}/cancel")]
        [RequirePermission(ErpPermissions.DiscountApprovals.Cancel)]
        public async Task<ActionResult<DiscountApprovalDto>> Cancel(
            int id,
            [FromBody] DiscountApprovalDecisionRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            return await Decision(id, request, userId, _service.CancelAsync, cancellationToken);
        }

        [HttpPost("{id:int}/resubmit")]
        [RequirePermission(ErpPermissions.DiscountApprovals.Resubmit)]
        public async Task<ActionResult<DiscountApprovalDto>> Resubmit(
            int id,
            [FromBody] DiscountApprovalDecisionRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            return await Decision(id, request, userId, _service.ResubmitAsync, cancellationToken);
        }

        private async Task<ActionResult<DiscountApprovalDto>> Decision(
            int id,
            DiscountApprovalDecisionRequestDto? request,
            int? userId,
            Func<int, DiscountApprovalDecisionRequestDto?, string, CancellationToken,
                Task<DiscountApprovalDto?>> action,
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

        private static string ResolveActingUser(int? userId) =>
            userId is int id and > 0 ? id.ToString() : "system";
    }
}
