using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement.Dtos;
using ERP.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.API.Controllers
{
    [Route("api/inventory-transactions")]
    [Route("api/v1/inventory-transactions")]
    [ApiController]
    public class InventoryTransactionsController : ControllerBase
    {
        private readonly ERPDbContext _db;

        public InventoryTransactionsController(ERPDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<List<InventoryTransactionDto>>> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? purchaseOrderNumber,
            [FromQuery] int? goodsReceiptId,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            CancellationToken cancellationToken = default)
        {
            var query = _db.GoodsReceipts
                .Include(g => g.Items)
                .AsNoTracking()
                .Where(g => !g.IsDeleted);

            if (goodsReceiptId.HasValue && goodsReceiptId.Value > 0)
            {
                query = query.Where(g => g.Id == goodsReceiptId.Value);
            }

            if (!string.IsNullOrWhiteSpace(purchaseOrderNumber))
            {
                var poNum = purchaseOrderNumber.Trim().ToLower();
                query = query.Where(g => g.PurchaseOrderNumber.ToLower().Contains(poNum));
            }

            if (dateFrom.HasValue)
            {
                query = query.Where(g => g.ReceiptDate >= dateFrom.Value.ToUniversalTime());
            }

            if (dateTo.HasValue)
            {
                query = query.Where(g => g.ReceiptDate <= dateTo.Value.ToUniversalTime());
            }

            var receipts = await query
                .OrderByDescending(g => g.ReceiptDate)
                .Take(200)
                .ToListAsync(cancellationToken);

            var transactions = new List<InventoryTransactionDto>();
            foreach (var grn in receipts)
            {
                foreach (var item in grn.Items)
                {
                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        var s = search.Trim().ToLower();
                        bool match = item.ItemName.ToLower().Contains(s) ||
                                     grn.PurchaseOrderNumber.ToLower().Contains(s) ||
                                     grn.GRNNumber.ToLower().Contains(s) ||
                                     grn.Warehouse.ToLower().Contains(s);
                        if (!match) continue;
                    }

                    transactions.Add(new InventoryTransactionDto
                    {
                        Id = $"inv-{grn.Id}-{item.Id}",
                        ItemName = item.ItemName,
                        QuantityReceived = item.AcceptedQuantity > 0 ? item.AcceptedQuantity : item.ReceivedQuantity,
                        ReceiptDate = grn.ReceiptDate.ToString("yyyy-MM-dd"),
                        PurchaseOrderNumber = grn.PurchaseOrderNumber,
                        GoodsReceiptNumber = grn.GRNNumber,
                        GoodsReceiptId = grn.Id,
                        Warehouse = grn.Warehouse,
                        CreatedAt = grn.CreatedAt.ToString("o")
                    });
                }
            }

            return Ok(transactions);
        }

        [HttpGet("by-goods-receipt/{goodsReceiptId:int}")]
        public async Task<ActionResult<List<InventoryTransactionDto>>> GetByGoodsReceipt(
            int goodsReceiptId,
            CancellationToken cancellationToken = default)
        {
            var grn = await _db.GoodsReceipts
                .Include(g => g.Items)
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == goodsReceiptId && !g.IsDeleted, cancellationToken);

            if (grn == null)
            {
                return Ok(new List<InventoryTransactionDto>());
            }

            var list = grn.Items.Select(item => new InventoryTransactionDto
            {
                Id = $"inv-{grn.Id}-{item.Id}",
                ItemName = item.ItemName,
                QuantityReceived = item.AcceptedQuantity > 0 ? item.AcceptedQuantity : item.ReceivedQuantity,
                ReceiptDate = grn.ReceiptDate.ToString("yyyy-MM-dd"),
                PurchaseOrderNumber = grn.PurchaseOrderNumber,
                GoodsReceiptNumber = grn.GRNNumber,
                GoodsReceiptId = grn.Id,
                Warehouse = grn.Warehouse,
                CreatedAt = grn.CreatedAt.ToString("o")
            }).ToList();

            return Ok(list);
        }

        [HttpPost]
        public ActionResult<List<InventoryTransactionDto>> Record([FromBody] RecordInventoryTransactionsRequestDto request)
        {
            return Ok(request?.Transactions ?? new List<InventoryTransactionDto>());
        }

        [HttpDelete("by-goods-receipt/{goodsReceiptId:int}")]
        public ActionResult RemoveByGoodsReceipt(int goodsReceiptId)
        {
            return NoContent();
        }
    }
}

