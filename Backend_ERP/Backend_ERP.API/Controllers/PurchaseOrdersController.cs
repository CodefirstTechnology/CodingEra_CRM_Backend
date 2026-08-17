using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Route("api/purchase-orders")]
    [ApiController]
    public class PurchaseOrdersController : ControllerBase
    {
        private readonly IPurchaseOrderService _poService;

        public PurchaseOrdersController(IPurchaseOrderService poService)
        {
            _poService = poService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<PurchaseOrderListItemDto>>> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] string? vendorName,
            [FromQuery] string? priority,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] int? userId = null,
            CancellationToken cancellationToken = default)
        {
            _ = userId;
            var result = await _poService.GetAllAsync(
                new PurchaseOrderListQueryDto
                {
                    Search = search,
                    Status = status,
                    DateFrom = dateFrom,
                    DateTo = dateTo,
                    VendorName = vendorName,
                    Priority = priority,
                    Page = page,
                    PageSize = pageSize
                },
                cancellationToken);

            return Ok(result);
        }

        [HttpGet("next-number")]
        public async Task<ActionResult<string>> GetNextNumber([FromQuery] int? userId, CancellationToken cancellationToken)
        {
            _ = userId;
            var number = await _poService.GetNextNumberAsync(cancellationToken);
            return Ok(number);
        }

        [HttpGet("approval-queue")]
        public async Task<ActionResult<List<PurchaseOrderApprovalQueueItemDto>>> GetApprovalQueue(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] string? priority,
            [FromQuery] int? userId = null,
            CancellationToken cancellationToken = default)
        {
            _ = userId;
            var result = await _poService.GetApprovalQueueAsync(
                new PurchaseOrderListQueryDto
                {
                    Search = search,
                    Status = status,
                    DateFrom = dateFrom,
                    DateTo = dateTo,
                    Priority = priority
                },
                cancellationToken);

            return Ok(result);
        }

        [HttpGet("approval-metrics")]
        public async Task<ActionResult<PurchaseOrderApprovalMetricsDto>> GetApprovalMetrics(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] string? priority,
            [FromQuery] int? userId = null,
            CancellationToken cancellationToken = default)
        {
            _ = userId;
            var result = await _poService.GetApprovalMetricsAsync(
                new PurchaseOrderListQueryDto
                {
                    Search = search,
                    Status = status,
                    DateFrom = dateFrom,
                    DateTo = dateTo,
                    Priority = priority
                },
                cancellationToken);

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<PurchaseOrderDto>> GetById(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var po = await _poService.GetByIdAsync(id, cancellationToken);
            if (po is null)
            {
                return NotFound();
            }

            return Ok(po);
        }

        [HttpPost]
        public async Task<ActionResult<PurchaseOrderDto>> Create(
            [FromBody] PurchaseOrderCreateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var created = await _poService.CreateAsync(
                request,
                ResolveActingUser(userId),
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = created.Id, userId }, created);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<PurchaseOrderDto>> Update(
            int id,
            [FromBody] PurchaseOrderUpdateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var updated = await _poService.UpdateAsync(
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

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var success = await _poService.DeleteAsync(
                id,
                ResolveActingUser(userId),
                cancellationToken);

            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpPost("{id:int}/status")]
        public async Task<ActionResult<PurchaseOrderDto>> UpdateStatus(
            int id,
            [FromBody] PurchaseOrderStatusUpdateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var updated = await _poService.UpdateStatusAsync(
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

        [HttpPost("{id:int}/cancel")]
        public async Task<ActionResult<PurchaseOrderDto>> Cancel(
            int id,
            [FromBody] PurchaseOrderApprovalActionRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var updated = await _poService.CancelAsync(
                id,
                request?.Remarks ?? "Purchase order cancelled.",
                ResolveActingUser(userId),
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            return Ok(updated);
        }

        [HttpGet("{id:int}/status-history")]
        public async Task<ActionResult<List<PurchaseOrderHistoryDto>>> GetStatusHistory(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var history = await _poService.GetStatusHistoryAsync(id, cancellationToken);
            return Ok(history);
        }

        [HttpPost("{id:int}/approve")]
        public async Task<ActionResult<PurchaseOrderDto>> Approve(
            int id,
            [FromBody] PurchaseOrderApprovalActionRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var updated = await _poService.ApproveAsync(
                id,
                request?.Remarks ?? "Purchase order approved.",
                ResolveActingUser(userId),
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            return Ok(updated);
        }

        [HttpPost("{id:int}/reject")]
        public async Task<ActionResult<PurchaseOrderDto>> Reject(
            int id,
            [FromBody] PurchaseOrderApprovalActionRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var updated = await _poService.RejectAsync(
                id,
                request?.Remarks ?? "Purchase order rejected.",
                ResolveActingUser(userId),
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            return Ok(updated);
        }

        [HttpPost("{id:int}/request-revision")]
        public async Task<ActionResult<PurchaseOrderDto>> RequestRevision(
            int id,
            [FromBody] PurchaseOrderApprovalActionRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var updated = await _poService.RequestRevisionAsync(
                id,
                request?.Remarks ?? "Revision requested.",
                ResolveActingUser(userId),
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            return Ok(updated);
        }

        [HttpPost("{id:int}/reopen")]
        public async Task<ActionResult<PurchaseOrderDto>> Reopen(
            int id,
            [FromBody] PurchaseOrderApprovalActionRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var updated = await _poService.ReopenAsync(
                id,
                request?.Remarks ?? "Purchase order reopened.",
                ResolveActingUser(userId),
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            return Ok(updated);
        }

        [HttpGet("{id:int}/approval-history")]
        public async Task<ActionResult<List<PurchaseOrderApprovalHistoryDto>>> GetApprovalHistory(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var history = await _poService.GetApprovalHistoryAsync(id, cancellationToken);
            return Ok(history);
        }

        private static string ResolveActingUser(int? userId) =>
            userId is > 0 ? userId.Value.ToString() : "system";
    }
}
