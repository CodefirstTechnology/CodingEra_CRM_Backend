using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Procurement;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    public class SalesOrderAllocationService : ISalesOrderAllocationService
    {
        private readonly ERPDbContext _db;

        public SalesOrderAllocationService(ERPDbContext db)
        {
            _db = db;
        }

        public async Task<AllocationResultDto> AllocateOrderInventoryAsync(
            int salesOrderId,
            string actingUser,
            CancellationToken ct = default)
        {
            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                var order = await _db.SalesOrders
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x => x.Id == salesOrderId, ct);

                if (order == null)
                {
                    return new AllocationResultDto
                    {
                        Success = false,
                        Message = $"Sales Order #{salesOrderId} not found."
                    };
                }

                // Deterministically sort line items by ItemId to prevent PostgreSQL FOR UPDATE deadlocks under concurrency
                var targetLines = order.Items
                    .Where(i => i.ItemType == PricingItemType.FinishedGood && i.ItemId.HasValue)
                    .OrderBy(i => i.ItemId!.Value)
                    .ThenBy(i => i.Id)
                    .ToList();

                var lineResults = new List<LineAllocationStatusDto>();

                foreach (var line in targetLines)
                {
                    var needed = line.Quantity - line.AllocatedQuantity;
                    if (needed <= 0)
                    {
                        lineResults.Add(new LineAllocationStatusDto
                        {
                            SalesOrderItemId = line.Id,
                            ItemCode = line.ItemCode,
                            OrderedQuantity = line.Quantity,
                            AllocatedQuantity = line.AllocatedQuantity,
                            DeficitQuantity = 0m
                        });
                        continue;
                    }

                    // Acquire PostgreSQL row lock on candidate Finished Goods records for this item (ordered by Id)
                    FinishedGood? fg;
                    if (_db.Database.IsRelational())
                    {
                        fg = await _db.FinishedGoods
                            .FromSqlInterpolated($@"
                                SELECT * FROM finished_goods 
                                WHERE ""Id"" = {line.ItemId!.Value} 
                                FOR UPDATE")
                            .FirstOrDefaultAsync(ct);
                    }
                    else
                    {
                        fg = await _db.FinishedGoods
                            .FirstOrDefaultAsync(f => f.Id == line.ItemId!.Value, ct);
                    }
                    if (fg == null)
                    {
                        lineResults.Add(new LineAllocationStatusDto
                        {
                            SalesOrderItemId = line.Id,
                            ItemCode = line.ItemCode,
                            OrderedQuantity = line.Quantity,
                            AllocatedQuantity = line.AllocatedQuantity,
                            DeficitQuantity = needed
                        });
                        continue;
                    }

                    var available = Math.Max(0m, fg.FinishedQuantity - fg.ReservedQuantity);
                    var allocatable = Math.Min(needed, available);

                    if (allocatable > 0)
                    {
                        fg.ReservedQuantity += allocatable;
                        fg.RecalculateAvailable();

                        var allocation = new SalesOrderAllocation
                        {
                            TenantId = order.TenantId,
                            SalesOrderId = order.Id,
                            SalesOrderItemId = line.Id,
                            FinishedGoodId = fg.Id,
                            WarehouseId = fg.WarehouseId,
                            BatchNumber = string.IsNullOrWhiteSpace(fg.BatchNumber) ? "DEFAULT-BATCH" : fg.BatchNumber,
                            AllocatedQuantity = allocatable,
                            AllocationStatus = AllocationStatus.Reserved,
                            AllocatedAt = DateTimeOffset.UtcNow,
                            AllocatedBy = actingUser
                        };

                        _db.SalesOrderAllocations.Add(allocation);
                        line.AllocatedQuantity += allocatable;
                    }

                    lineResults.Add(new LineAllocationStatusDto
                    {
                        SalesOrderItemId = line.Id,
                        ItemCode = line.ItemCode,
                        OrderedQuantity = line.Quantity,
                        AllocatedQuantity = line.AllocatedQuantity,
                        DeficitQuantity = Math.Max(0m, line.Quantity - line.AllocatedQuantity)
                    });
                }

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                var totalOrdered = lineResults.Sum(x => x.OrderedQuantity);
                var totalAllocated = lineResults.Sum(x => x.AllocatedQuantity);

                return new AllocationResultDto
                {
                    Success = true,
                    Message = totalAllocated >= totalOrdered
                        ? "Order inventory fully allocated across warehouse stock."
                        : $"Order partially allocated ({totalAllocated:N2} / {totalOrdered:N2}). Backorders flagged.",
                    Lines = lineResults
                };
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<AllocationResultDto> SplitWarehouseAllocationAsync(
            int salesOrderId,
            SplitAllocationRequestDto request,
            string actingUser,
            CancellationToken ct = default)
        {
            if (request == null || request.Splits.Count == 0)
            {
                throw new InvalidOperationException("No warehouse split allocations provided.");
            }

            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                var order = await _db.SalesOrders
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x => x.Id == salesOrderId, ct);

                if (order == null)
                {
                    return new AllocationResultDto
                    {
                        Success = false,
                        Message = $"Sales Order #{salesOrderId} not found."
                    };
                }

                // Deterministically sort requested splits by WarehouseId and SalesOrderItemId to avoid deadlocks
                var sortedSplits = request.Splits
                    .OrderBy(s => s.WarehouseId)
                    .ThenBy(s => s.SalesOrderItemId)
                    .ToList();

                var lineResults = new List<LineAllocationStatusDto>();

                foreach (var split in sortedSplits)
                {
                    var line = order.Items.FirstOrDefault(i => i.Id == split.SalesOrderItemId);
                    if (line == null || !line.ItemId.HasValue) continue;

                    // Acquire row lock on target finished goods stock in specified warehouse
                    FinishedGood? fg;
                    if (_db.Database.IsRelational())
                    {
                        fg = await _db.FinishedGoods
                            .FromSqlInterpolated($@"
                                SELECT * FROM finished_goods 
                                WHERE ""ProductCode"" = {line.ItemCode} AND ""WarehouseId"" = {split.WarehouseId} 
                                FOR UPDATE")
                            .FirstOrDefaultAsync(ct);
                    }
                    else
                    {
                        fg = await _db.FinishedGoods
                            .FirstOrDefaultAsync(f => f.ProductCode == line.ItemCode && f.WarehouseId == split.WarehouseId, ct);
                    }

                    if (fg == null)
                    {
                        throw new InvalidOperationException(
                            $"Finished good '{line.ItemCode}' does not exist in target Warehouse #{split.WarehouseId}.");
                    }

                    var available = Math.Max(0m, fg.FinishedQuantity - fg.ReservedQuantity);
                    if (split.Quantity > available)
                    {
                        throw new InvalidOperationException(
                            $"Requested split quantity ({split.Quantity:N2}) exceeds available stock ({available:N2}) in Warehouse #{split.WarehouseId}.");
                    }

                    fg.ReservedQuantity += split.Quantity;
                    fg.RecalculateAvailable();

                    var allocation = new SalesOrderAllocation
                    {
                        TenantId = order.TenantId,
                        SalesOrderId = order.Id,
                        SalesOrderItemId = line.Id,
                        FinishedGoodId = fg.Id,
                        WarehouseId = split.WarehouseId,
                        BatchNumber = string.IsNullOrWhiteSpace(fg.BatchNumber) ? "SPLIT-BATCH" : fg.BatchNumber,
                        AllocatedQuantity = split.Quantity,
                        AllocationStatus = AllocationStatus.Reserved,
                        AllocatedAt = DateTimeOffset.UtcNow,
                        AllocatedBy = actingUser
                    };

                    _db.SalesOrderAllocations.Add(allocation);
                    line.AllocatedQuantity += split.Quantity;

                    lineResults.Add(new LineAllocationStatusDto
                    {
                        SalesOrderItemId = line.Id,
                        ItemCode = line.ItemCode,
                        OrderedQuantity = line.Quantity,
                        AllocatedQuantity = line.AllocatedQuantity,
                        DeficitQuantity = Math.Max(0m, line.Quantity - line.AllocatedQuantity)
                    });
                }

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return new AllocationResultDto
                {
                    Success = true,
                    Message = "Split warehouse soft allocations confirmed successfully.",
                    Lines = lineResults
                };
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        public async Task ReleaseOrderAllocationsAsync(
            int salesOrderId,
            string reason,
            string actingUser,
            CancellationToken ct = default)
        {
            var allocations = await _db.SalesOrderAllocations
                .Where(x => x.SalesOrderId == salesOrderId && x.AllocationStatus == AllocationStatus.Reserved)
                .OrderBy(x => x.FinishedGoodId) // Deterministic ordering
                .ToListAsync(ct);

            if (allocations.Count == 0) return;

            foreach (var alloc in allocations)
            {
                var fg = await _db.FinishedGoods.FirstOrDefaultAsync(x => x.Id == alloc.FinishedGoodId, ct);
                if (fg != null)
                {
                    fg.ReservedQuantity = Math.Max(0m, fg.ReservedQuantity - alloc.AllocatedQuantity);
                    fg.RecalculateAvailable();
                }

                alloc.AllocationStatus = AllocationStatus.Released;
                alloc.ReleasedDate = DateTimeOffset.UtcNow;
                alloc.ReleaseRemarks = $"{reason} (by {actingUser})";
            }

            var orderItems = await _db.SalesOrderItems
                .Where(x => x.SalesOrderId == salesOrderId)
                .ToListAsync(ct);

            foreach (var item in orderItems)
            {
                item.AllocatedQuantity = 0m;
            }

            await _db.SaveChangesAsync(ct);
        }

        public async Task<IReadOnlyList<SalesOrderAllocationDto>> GetAllocationsForOrderAsync(
            int salesOrderId,
            CancellationToken ct = default)
        {
            var rows = await _db.SalesOrderAllocations
                .AsNoTracking()
                .Include(x => x.SalesOrderItem)
                .Include(x => x.Warehouse)
                .Where(x => x.SalesOrderId == salesOrderId)
                .OrderByDescending(x => x.AllocatedAt)
                .ToListAsync(ct);

            return rows.Select(SalesOrderMapper.ToAllocationDto).ToList();
        }
    }
}
