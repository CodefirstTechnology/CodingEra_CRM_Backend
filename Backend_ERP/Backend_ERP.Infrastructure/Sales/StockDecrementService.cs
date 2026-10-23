using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Quotations;
using ERP.Domain.Procurement;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    public class StockDecrementService : IStockDecrementService
    {
        private readonly ERPDbContext _dbContext;

        public StockDecrementService(ERPDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<StockDeductionResultDto> DeductPhysicalStockForSalesOrderAsync(
            int salesOrderId,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var salesOrder = await _dbContext.SalesOrders
                .Include(so => so.Items)
                .Include(so => so.StatusHistory)
                .FirstOrDefaultAsync(so => so.Id == salesOrderId, cancellationToken)
                ?? throw new InvalidOperationException($"Sales Order with ID {salesOrderId} not found.");

            if (salesOrder.Status == SalesOrderStatuses.Completed)
            {
                throw new InvalidOperationException($"Sales Order {salesOrder.SalesOrderNumber} has already been completed and dispatched.");
            }

            var result = new StockDeductionResultDto
            {
                SalesOrderId = salesOrder.Id,
                SalesOrderNumber = salesOrder.SalesOrderNumber,
                CompletedAtUtc = DateTime.UtcNow
            };

            foreach (var item in salesOrder.Items)
            {
                var remainingToDispatch = item.Quantity - item.DispatchedQuantity;
                if (remainingToDispatch <= 0)
                    continue;

                var warehouseId = item.WarehouseId ?? 1;

                FinishedGood? fg = null;
                if (item.ItemId.HasValue && item.ItemId.Value > 0)
                {
                    fg = await _dbContext.FinishedGoods
                        .FirstOrDefaultAsync(f => f.Id == item.ItemId.Value && f.WarehouseId == warehouseId, cancellationToken);
                }

                if (fg is null && !string.IsNullOrWhiteSpace(item.ItemCode))
                {
                    var cleanCode = item.ItemCode.Trim().ToLower();
                    fg = await _dbContext.FinishedGoods
                        .FirstOrDefaultAsync(f => f.ProductCode.ToLower() == cleanCode && f.WarehouseId == warehouseId, cancellationToken);
                }

                if (fg is null)
                {
                    throw new InvalidOperationException(
                        $"Inventory dispatch error: Finished Good '{item.ItemName}' (Code: {item.ItemCode}) was not found in Warehouse ID {warehouseId}.");
                }

                // Strict Prevention of Negative Inventory
                if (fg.FinishedQuantity < remainingToDispatch)
                {
                    throw new InvalidOperationException(
                        $"Negative inventory violation: Cannot dispatch {remainingToDispatch} of '{fg.ProductCode}'. Physical stock on hand is only {fg.FinishedQuantity}.");
                }

                // Decrement Physical Stock and Release Soft Reservation
                fg.FinishedQuantity -= remainingToDispatch;
                fg.ReservedQuantity = Math.Max(0m, fg.ReservedQuantity - remainingToDispatch);
                fg.RecalculateAvailable();
                fg.CurrentValue = Math.Round(fg.FinishedQuantity * fg.UnitCost, 4);

                if (fg.FinishedQuantity == 0)
                {
                    fg.DispatchStatus = FgDispatchStatus.Dispatched;
                }

                fg.UpdatedAt = DateTime.UtcNow;
                fg.UpdatedBy = actingUser;

                // Log Immutable Double-Entry Stock Transaction
                var txnNumber = $"STX-DISP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
                var stockTxn = new StockTransaction
                {
                    TenantId = salesOrder.TenantId,
                    TransactionNumber = txnNumber,
                    TransactionType = StockTxnType.Dispatch,
                    ReferenceType = StockReferenceType.Dispatch,
                    ReferenceNumber = salesOrder.SalesOrderNumber,
                    ReferenceId = salesOrder.Id,
                    MaterialId = fg.Id,
                    MaterialCode = fg.ProductCode,
                    MaterialName = fg.ProductName,
                    WarehouseId = fg.WarehouseId,
                    WarehouseName = fg.WarehouseName,
                    Quantity = remainingToDispatch,
                    BalanceAfter = fg.FinishedQuantity,
                    Unit = fg.Unit,
                    UnitCost = fg.UnitCost,
                    User = actingUser,
                    TransactionDate = DateTime.UtcNow,
                    Reason = $"Order dispatch fulfillment for Sales Order {salesOrder.SalesOrderNumber}",
                    Remarks = $"Deducted {remainingToDispatch} units. Remaining physical stock: {fg.FinishedQuantity}",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = actingUser,
                    IdempotencyKey = Guid.NewGuid()
                };

                await _dbContext.StockTransactions.AddAsync(stockTxn, cancellationToken);

                item.DispatchedQuantity += remainingToDispatch;

                result.DeductedItems.Add(new StockDeductionItemDto
                {
                    ItemCode = fg.ProductCode,
                    ItemName = fg.ProductName,
                    WarehouseId = fg.WarehouseId,
                    WarehouseName = fg.WarehouseName,
                    QuantityDeducted = remainingToDispatch,
                    RemainingPhysicalStock = fg.FinishedQuantity,
                    TransactionNumber = txnNumber
                });
            }

            salesOrder.Status = SalesOrderStatuses.Completed;
            salesOrder.UpdatedBy = actingUser;
            salesOrder.UpdatedDate = DateTimeOffset.UtcNow;

            salesOrder.StatusHistory.Add(new SalesOrderStatusHistory
            {
                EntryKey = Guid.NewGuid().ToString("N"),
                SalesOrderId = salesOrder.Id,
                Status = SalesOrderStatuses.Completed,
                Date = DateTimeOffset.UtcNow,
                User = actingUser,
                Label = "Dispatched and Completed",
                Remarks = $"Physical stock deducted across {result.DeductedItems.Count} items. Order fulfilled."
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return result;
        }
    }
}
