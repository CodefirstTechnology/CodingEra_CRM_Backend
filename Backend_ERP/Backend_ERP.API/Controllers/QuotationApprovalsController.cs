using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [ApiController]
    [Route("api/quotation-approvals")]
    public class QuotationApprovalsController : ControllerBase
    {
        private readonly IQuotationApprovalService _service;

        public QuotationApprovalsController(IQuotationApprovalService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<ActionResult<QuotationApprovalDto>> CreateAsync(
            [FromBody] QuotationApprovalCreateRequestDto request,
            CancellationToken cancellationToken)
        {
            var user = "SYSTEM"; // Mock user since CRM is not integrated
            var result = await _service.CreateAsync(request, user, cancellationToken);
            return CreatedAtAction(nameof(GetByIdAsync), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<QuotationApprovalDto>> UpdateAsync(
            int id,
            [FromBody] QuotationApprovalUpdateRequestDto request,
            CancellationToken cancellationToken)
        {
            var user = "SYSTEM";
            var result = await _service.UpdateAsync(id, request, user, cancellationToken);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAsync(int id, CancellationToken cancellationToken)
        {
            var user = "SYSTEM";
            var result = await _service.DeleteAsync(id, user, cancellationToken);
            if (!result)
                return NotFound();
            return NoContent();
        }

        [HttpPost("{id}/submit")]
        public async Task<ActionResult<QuotationApprovalDto>> SubmitAsync(int id, CancellationToken cancellationToken)
        {
            var user = "SYSTEM";
            var result = await _service.SubmitAsync(id, user, cancellationToken);
            return Ok(result);
        }

        [HttpPost("{id}/review")]
        public async Task<ActionResult<QuotationApprovalDto>> ReviewAsync(
            int id,
            [FromBody] QuotationApprovalDecisionRequestDto request,
            CancellationToken cancellationToken)
        {
            var user = "SYSTEM";
            var result = await _service.ReviewAsync(id, request, user, cancellationToken);
            return Ok(result);
        }

        [HttpPost("{id}/approve")]
        public async Task<ActionResult<QuotationApprovalDto>> ApproveAsync(
            int id,
            [FromBody] QuotationApprovalDecisionRequestDto request,
            CancellationToken cancellationToken)
        {
            var user = "SYSTEM";
            var result = await _service.ApproveAsync(id, request, user, cancellationToken);
            return Ok(result);
        }

        [HttpPost("{id}/reject")]
        public async Task<ActionResult<QuotationApprovalDto>> RejectAsync(
            int id,
            [FromBody] QuotationApprovalDecisionRequestDto request,
            CancellationToken cancellationToken)
        {
            var user = "SYSTEM";
            var result = await _service.RejectAsync(id, request, user, cancellationToken);
            return Ok(result);
        }

        [HttpPost("{id}/return")]
        public async Task<ActionResult<QuotationApprovalDto>> ReturnAsync(
            int id,
            [FromBody] QuotationApprovalDecisionRequestDto request,
            CancellationToken cancellationToken)
        {
            var user = "SYSTEM";
            var result = await _service.ReturnAsync(id, request, user, cancellationToken);
            return Ok(result);
        }

        [HttpPost("{id}/cancel")]
        public async Task<ActionResult<QuotationApprovalDto>> CancelAsync(
            int id,
            [FromBody] QuotationApprovalDecisionRequestDto request,
            CancellationToken cancellationToken)
        {
            var user = "SYSTEM";
            var result = await _service.CancelAsync(id, request, user, cancellationToken);
            return Ok(result);
        }

        [HttpPost("{id}/request-revision")]
        public async Task<ActionResult<QuotationApprovalDto>> RequestRevisionAsync(
            int id,
            [FromBody] QuotationApprovalDecisionRequestDto request,
            CancellationToken cancellationToken)
        {
            var user = "SYSTEM";
            var result = await _service.RequestRevisionAsync(id, request, user, cancellationToken);
            return Ok(result);
        }

        [HttpPost("{id}/reopen")]
        public async Task<ActionResult<QuotationApprovalDto>> ReopenAsync(
            int id,
            [FromBody] QuotationApprovalDecisionRequestDto request,
            CancellationToken cancellationToken)
        {
            var user = "SYSTEM";
            var result = await _service.ReopenAsync(id, request, user, cancellationToken);
            return Ok(result);
        }

        [HttpPost("{id}/comments")]
        public async Task<ActionResult<QuotationApprovalCommentDto>> AddCommentAsync(
            int id,
            [FromBody] QuotationApprovalCommentRequestDto request,
            CancellationToken cancellationToken)
        {
            var user = "SYSTEM";
            var result = await _service.AddCommentAsync(id, request, user, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<QuotationApprovalDto>> GetByIdAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var result = await _service.GetByIdAsync(id, cancellationToken);
            if (result is null)
                return NotFound();
            return Ok(result);
        }
        
        [HttpGet("statistics")]
        public async Task<ActionResult<QuotationApprovalStatisticsDto>> GetStatisticsAsync(CancellationToken cancellationToken)
        {
            var result = await _service.GetStatisticsAsync(cancellationToken);
            return Ok(result);
        }
    }
}
