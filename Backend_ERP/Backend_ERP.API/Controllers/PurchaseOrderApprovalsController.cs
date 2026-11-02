using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Route("api/purchase-order-approvals")]
    [ApiController]
    public class PurchaseOrderApprovalsController : ControllerBase
    {
        private readonly IPurchaseOrderService _poService;
        private readonly IPurchaseOrderApprovalEngine _approvalEngine;

        public PurchaseOrderApprovalsController(
            IPurchaseOrderService poService,
            IPurchaseOrderApprovalEngine approvalEngine)
        {
            _poService = poService;
            _approvalEngine = approvalEngine;
        }

        [HttpGet("queue")]
        public async Task<ActionResult<List<PurchaseOrderApprovalQueueItemDto>>> GetQueue(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] string? priority,
            [FromQuery] int? userId = null,
            CancellationToken cancellationToken = default)
        {
            _ = userId;
            var queue = await _poService.GetApprovalQueueAsync(
                new PurchaseOrderListQueryDto
                {
                    Search = search,
                    Status = status,
                    DateFrom = dateFrom,
                    DateTo = dateTo,
                    Priority = priority
                },
                cancellationToken);

            return Ok(queue);
        }

        [HttpGet("metrics")]
        public async Task<ActionResult<PurchaseOrderApprovalMetricsDto>> GetMetrics(
            [FromQuery] int? userId = null,
            CancellationToken cancellationToken = default)
        {
            _ = userId;
            var metrics = await _poService.GetApprovalMetricsAsync(new PurchaseOrderListQueryDto(), cancellationToken);
            return Ok(metrics);
        }

        [HttpPost("{id:int}/action")]
        public async Task<ActionResult<POApprovalSignOffResultDto>> Action(
            int id,
            [FromBody] POApprovalSignOffRequestDto request,
            CancellationToken cancellationToken)
        {
            var result = await _approvalEngine.ProcessApprovalActionAsync(id, request, cancellationToken);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost("{id:int}/approve")]
        public async Task<ActionResult<POApprovalSignOffResultDto>> Approve(
            int id,
            [FromBody] PurchaseOrderApprovalActionRequestDto? request,
            [FromQuery] string? approverUserId = "system",
            [FromQuery] string? approverName = "Authorized Approver",
            [FromQuery] string? approverRole = "Approver",
            CancellationToken cancellationToken = default)
        {
            var signOffRequest = new POApprovalSignOffRequestDto
            {
                Decision = PurchaseOrderApprovalDecision.Approved,
                Comments = request?.Remarks ?? "Approved via Purchase Order Approvals Hub.",
                ApproverUserId = approverUserId ?? "system",
                ApproverName = approverName ?? "Authorized Approver",
                ApproverRole = approverRole ?? "Approver"
            };

            var result = await _approvalEngine.ProcessApprovalActionAsync(id, signOffRequest, cancellationToken);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost("{id:int}/reject")]
        public async Task<ActionResult<POApprovalSignOffResultDto>> Reject(
            int id,
            [FromBody] PurchaseOrderApprovalActionRequestDto? request,
            [FromQuery] string? approverUserId = "system",
            [FromQuery] string? approverName = "Authorized Approver",
            [FromQuery] string? approverRole = "Approver",
            CancellationToken cancellationToken = default)
        {
            var signOffRequest = new POApprovalSignOffRequestDto
            {
                Decision = PurchaseOrderApprovalDecision.Rejected,
                Comments = request?.Remarks ?? "Rejected via Purchase Order Approvals Hub.",
                ApproverUserId = approverUserId ?? "system",
                ApproverName = approverName ?? "Authorized Approver",
                ApproverRole = approverRole ?? "Approver"
            };

            var result = await _approvalEngine.ProcessApprovalActionAsync(id, signOffRequest, cancellationToken);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost("{id:int}/return")]
        public async Task<ActionResult<POApprovalSignOffResultDto>> ReturnToDraft(
            int id,
            [FromBody] PurchaseOrderApprovalActionRequestDto? request,
            [FromQuery] string? approverUserId = "system",
            [FromQuery] string? approverName = "Authorized Approver",
            [FromQuery] string? approverRole = "Approver",
            CancellationToken cancellationToken = default)
        {
            var signOffRequest = new POApprovalSignOffRequestDto
            {
                Decision = PurchaseOrderApprovalDecision.Returned,
                Comments = request?.Remarks ?? "Returned for revision via Purchase Order Approvals Hub.",
                ApproverUserId = approverUserId ?? "system",
                ApproverName = approverName ?? "Authorized Approver",
                ApproverRole = approverRole ?? "Approver"
            };

            var result = await _approvalEngine.ProcessApprovalActionAsync(id, signOffRequest, cancellationToken);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}

