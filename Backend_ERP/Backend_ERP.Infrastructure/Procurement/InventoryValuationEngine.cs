using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Procurement
{
    public class InventoryValuationEngine : IInventoryValuationEngine
    {
        private readonly ERPDbContext _db;

        public InventoryValuationEngine(ERPDbContext db)
        {
            _db = db;
        }

        public decimal CalculateMovingWac(
            decimal currentStock,
            decimal currentWac,
            decimal acceptedQuantity,
            decimal landedUnitCost)
        {
            if (acceptedQuantity <= 0)
                return currentWac;

            if (currentStock <= 0)
                return Math.Round(landedUnitCost, 4, MidpointRounding.AwayFromZero);

            decimal totalQuantity = currentStock + acceptedQuantity;
            if (totalQuantity <= 0)
                return 0m;

            decimal totalValue = (currentStock * currentWac) + (acceptedQuantity * landedUnitCost);
            return Math.Round(totalValue / totalQuantity, 4, MidpointRounding.AwayFromZero);
        }

        public async Task<InventoryValuationResultDto> ProcessGrnInventoryValuationAsync(
            int goodsReceiptId,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var grn = await _db.GoodsReceipts
                .Include(g => g.Items)
                    .ThenInclude(i => i.PurchaseOrderLine)
                .FirstOrDefaultAsync(g => g.Id == goodsReceiptId && !g.IsDeleted, cancellationToken);

            if (grn == null)
            {
                throw new KeyNotFoundException($"Goods Receipt with ID {goodsReceiptId} was not found.");
            }

            if (grn.IsPostedToInventory)
            {
                throw new InvalidOperationException($"Goods Receipt #{grn.GRNNumber} is already posted to inventory.");
            }

            var result = new InventoryValuationResultDto
            {
                GoodsReceiptId = grn.Id
            };

            decimal totalValuation = 0m;
            int valuedCount = 0;
            var now = DateTime.UtcNow;

            foreach (var item in grn.Items)
            {
                // Physical stock accepted after QC inspection (fallback to AcceptedQuantity if QcAcceptedQuantity is 0)
                decimal acceptedQty = item.QcAcceptedQuantity > 0 ? item.QcAcceptedQuantity : item.AcceptedQuantity;
                if (acceptedQty <= 0)
                    continue;

                decimal poRate = item.PoRate > 0 
                    ? item.PoRate 
                    : (item.PurchaseOrderLine?.Rate ?? 0m);

                decimal landedCost = item.LandedCostPerUnit > 0 
                    ? item.LandedCostPerUnit 
                    : poRate;

                decimal lineValuation = Math.Round(acceptedQty * landedCost, 2, MidpointRounding.AwayFromZero);
                item.ValuationAmount = lineValuation;
                item.PoRate = poRate;
                item.LandedCostPerUnit = landedCost;
                totalValuation += lineValuation;
                valuedCount++;

                // Lookup matching Raw Material
                RawMaterial? rawMaterial = null;
                if (item.ItemId.HasValue && item.ItemId.Value > 0)
                {
                    rawMaterial = await _db.RawMaterials.FirstOrDefaultAsync(r => r.Id == item.ItemId.Value, cancellationToken);
                }
                if (rawMaterial == null && !string.IsNullOrWhiteSpace(item.ItemName))
                {
                    rawMaterial = await _db.RawMaterials.FirstOrDefaultAsync(r => r.MaterialName == item.ItemName || r.MaterialCode == item.ItemName, cancellationToken);
                }

                decimal previousWac = rawMaterial?.UnitCost ?? landedCost;
                decimal previousStock = rawMaterial?.AvailableStock ?? 0m;
                decimal newWac = CalculateMovingWac(previousStock, previousWac, acceptedQty, landedCost);
                decimal newStock = previousStock + acceptedQty;

                if (rawMaterial != null)
                {
                    rawMaterial.AvailableStock = newStock;
                    rawMaterial.UnitCost = newWac;
                    rawMaterial.CurrentValue = Math.Round(newStock * newWac, 2, MidpointRounding.AwayFromZero);
                    rawMaterial.LastReceiptDate = grn.ReceiptDate;
                    rawMaterial.LinkedGRNId = grn.Id;
                    rawMaterial.LinkedGRNNumber = grn.GRNNumber;
                    if (grn.PurchaseOrderId > 0)
                    {
                        rawMaterial.LinkedPOId = grn.PurchaseOrderId;
                        rawMaterial.LinkedPONumber = grn.PurchaseOrderNumber;
                    }
                }

                // Create immutable StockTransaction: Dr Raw Material Asset / Cr GRN/IR Clearing
                var stockTxn = new StockTransaction
                {
                    TenantId = "default",
                    TransactionNumber = $"STX-GRN-{grn.Id}-{item.Id}",
                    TransactionType = StockTxnType.PurchaseReceipt,
                    ReferenceType = StockReferenceType.GRN,
                    ReferenceId = grn.Id,
                    ReferenceNumber = grn.GRNNumber,
                    MaterialId = rawMaterial?.Id ?? item.ItemId,
                    MaterialCode = rawMaterial?.MaterialCode ?? item.ItemName,
                    MaterialName = item.ItemName,
                    WarehouseId = grn.WarehouseId ?? rawMaterial?.WarehouseId ?? 1,
                    WarehouseName = grn.Warehouse ?? "Main Store",
                    Quantity = acceptedQty,
                    Unit = item.Unit ?? "Nos",
                    UnitCost = landedCost,
                    BalanceAfter = newStock,
                    Reason = "Goods Receipt Inward & Moving WAC Recalculation",
                    Remarks = $"GRN #{grn.GRNNumber} for PO #{grn.PurchaseOrderNumber}. Double-entry: Dr Raw Material Asset / Cr GRN/IR Clearing Account.",
                    TransactionDate = now,
                    User = actingUser,
                    CreatedBy = actingUser,
                    CreatedAt = now,
                    IdempotencyKey = Guid.NewGuid()
                };

                _db.StockTransactions.Add(stockTxn);

                result.Transactions.Add(new StockTransactionResultDto
                {
                    MaterialCode = stockTxn.MaterialCode,
                    Quantity = acceptedQty,
                    PreviousWac = previousWac,
                    NewWac = newWac,
                    NewBalance = newStock
                });
            }

            grn.IsPostedToInventory = true;
            grn.PostedToInventoryAt = now;
            grn.TotalAcceptedAmount = totalValuation;
            grn.TotalLandedCost = totalValuation;
            grn.Status = GoodsReceiptStatus.Posted;
            grn.UpdatedAt = now;
            grn.UpdatedBy = actingUser;

            await _db.SaveChangesAsync(cancellationToken);

            result.ItemsValuedCount = valuedCount;
            result.TotalValuationAmount = totalValuation;

            return result;
        }
    }
}

