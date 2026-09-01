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
    [Route("api/goods-receipts")]
    [ApiController]
    public class GoodsReceiptsController : ControllerBase
    {
        private readonly IGoodsReceiptService _grService;

        public GoodsReceiptsController(IGoodsReceiptService grService)
        {
            _grService = grService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<GoodsReceiptListItemDto>>> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] int? purchaseOrderId,
            [FromQuery] string? vendorName,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 1000,
            [FromQuery] int? userId = null,
            CancellationToken cancellationToken = default)
        {
            _ = userId;
            var result = await _grService.GetAllAsync(
                new GoodsReceiptListQueryDto
                {
                    Search = search,
                    Status = status,
                    DateFrom = dateFrom,
                    DateTo = dateTo,
                    PurchaseOrderId = purchaseOrderId,
                    VendorName = vendorName,
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
            var number = await _grService.GetNextNumberAsync(cancellationToken);
            return Ok(number);
        }

        [HttpGet("by-purchase-order/{purchaseOrderId:int}")]
        public async Task<ActionResult<PagedResult<GoodsReceiptListItemDto>>> GetByPurchaseOrder(
            int purchaseOrderId,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var result = await _grService.GetByPurchaseOrderAsync(purchaseOrderId, cancellationToken);
            return Ok(result);
        }

        [HttpGet("build-items/{purchaseOrderId:int}")]
        public async Task<ActionResult<List<GoodsReceiptItemDto>>> BuildDraftItems(
            int purchaseOrderId,
            [FromQuery] int? excludeGrnId,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var items = await _grService.BuildDraftItemsForPurchaseOrderAsync(purchaseOrderId, excludeGrnId, cancellationToken);
            return Ok(items);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<GoodsReceiptDto>> GetById(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var gr = await _grService.GetByIdAsync(id, cancellationToken);
            if (gr is null)
            {
                return NotFound();
            }

            return Ok(gr);
        }

        [HttpPost]
        public async Task<ActionResult<GoodsReceiptDto>> Create(
            [FromBody] GoodsReceiptCreateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var created = await _grService.CreateAsync(
                request,
                ResolveActingUser(userId),
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = created.Id, userId }, created);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<GoodsReceiptDto>> Update(
            int id,
            [FromBody] GoodsReceiptUpdateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var updated = await _grService.UpdateAsync(
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
            var success = await _grService.DeleteAsync(
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
        public async Task<ActionResult<GoodsReceiptDto>> UpdateStatus(
            int id,
            [FromBody] GoodsReceiptStatusUpdateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var updated = await _grService.UpdateStatusAsync(
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
        public async Task<ActionResult<GoodsReceiptDto>> Cancel(
            int id,
            [FromBody] GoodsReceiptStatusUpdateRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var updated = await _grService.CancelAsync(
                id,
                request?.Remarks ?? "Goods receipt cancelled.",
                ResolveActingUser(userId),
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            return Ok(updated);
        }

        [HttpGet("{id:int}/status-history")]
        public async Task<ActionResult<List<GoodsReceiptHistoryDto>>> GetStatusHistory(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var history = await _grService.GetStatusHistoryAsync(id, cancellationToken);
            return Ok(history);
        }

        private static string ResolveActingUser(int? userId) =>
            userId is > 0 ? userId.Value.ToString() : "system";
    }
}
