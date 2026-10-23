using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Dtos;
using ERP.Application.Sales.Quotations;
using ERP.Domain.Procurement;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    public class InventoryAllocationService : IInventoryAllocationService
    {
        private readonly ERPDbContext _dbContext;

        public InventoryAllocationService(ERPDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<QuotationAtpCheckResponseDto> CheckAtpAsync(
            int warehouseId,
            int? itemId,
            string? itemCode,
            decimal requestedQuantity,
            string tenantId = "default",
            CancellationToken cancellationToken = default)
        {
            var query = _dbContext.FinishedGoods
                .AsNoTracking()
                .Where(fg => !fg.IsDeleted && fg.WarehouseId == warehouseId);

            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                query = query.Where(fg => fg.TenantId == tenantId || fg.TenantId == "default");
            }

            if (itemId.HasValue && itemId.Value > 0)
            {
                query = query.Where(fg => fg.Id == itemId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(itemCode))
            {
                var cleanCode = itemCode.Trim();
                query = query.Where(fg => fg.ProductCode.ToLower() == cleanCode.ToLower());
            }
            else
            {
                throw new ArgumentException("Either ItemId or ItemCode must be specified for ATP check.");
            }

            var fg = await query.FirstOrDefaultAsync(cancellationToken);

            if (fg is null)
            {
                return new QuotationAtpCheckResponseDto
                {
                    ItemId = itemId,
                    ItemCode = itemCode ?? string.Empty,
                    ProductName = "Unknown Product",
                    WarehouseId = warehouseId,
                    WarehouseName = "Unknown Warehouse",
                    PhysicalStock = 0m,
                    ReservedStock = 0m,
                    AvailableQuantity = 0m,
                    IsAvailable = false,
                    ShortageQuantity = requestedQuantity,
                    EarliestAvailableDateUtc = DateTime.UtcNow.AddDays(7) // Standard replenishment lead-time
                };
            }

            var physical = fg.FinishedQuantity;
            var reserved = fg.ReservedQuantity;
            var available = Math.Max(0m, physical - reserved);
            var isAvailable = available >= requestedQuantity;
            var shortage = isAvailable ? 0m : requestedQuantity - available;

            return new QuotationAtpCheckResponseDto
            {
                ItemId = fg.Id,
                ItemCode = fg.ProductCode,
                ProductName = fg.ProductName,
                WarehouseId = fg.WarehouseId,
                WarehouseName = fg.WarehouseName,
                PhysicalStock = physical,
                ReservedStock = reserved,
                AvailableQuantity = available,
                IsAvailable = isAvailable,
                ShortageQuantity = shortage,
                EarliestAvailableDateUtc = isAvailable ? DateTime.UtcNow : DateTime.UtcNow.AddDays(7)
            };
        }

        public async Task<IReadOnlyList<QuotationAtpCheckResponseDto>> CheckQuotationAtpAsync(
            int quotationId,
            CancellationToken cancellationToken = default)
        {
            var quotation = await _dbContext.Quotations
                .AsNoTracking()
                .Include(q => q.Items)
                .FirstOrDefaultAsync(q => q.Id == quotationId, cancellationToken)
                ?? throw new InvalidOperationException($"Quotation with ID {quotationId} not found.");

            var results = new List<QuotationAtpCheckResponseDto>();

            foreach (var item in quotation.Items)
            {
                var warehouseId = item.WarehouseId ?? 1; // Default to central warehouse 1 if not set
                var atp = await CheckAtpAsync(
                    warehouseId,
                    item.ItemId,
                    item.ItemCode,
                    item.Quantity,
                    quotation.TenantId,
                    cancellationToken);

                results.Add(atp);
            }

            return results;
        }

        public async Task SoftReserveQuotationStockAsync(
            Quotation quotation,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(quotation);

            foreach (var item in quotation.Items)
            {
                if (item.Quantity <= 0)
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

                if (fg is not null)
                {
                    // Increase soft reservation
                    fg.ReservedQuantity += item.Quantity;
                    fg.RecalculateAvailable();

                    if (fg.DispatchStatus == FgDispatchStatus.NotReady || fg.DispatchStatus == FgDispatchStatus.ReadyToDispatch)
                    {
                        fg.DispatchStatus = FgDispatchStatus.Reserved;
                    }

                    fg.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task ReleaseQuotationStockReservationAsync(
            int quotationId,
            CancellationToken cancellationToken = default)
        {
            var quotation = await _dbContext.Quotations
                .Include(q => q.Items)
                .FirstOrDefaultAsync(q => q.Id == quotationId, cancellationToken);

            if (quotation is null) return;

            foreach (var item in quotation.Items)
            {
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

                if (fg is not null)
                {
                    fg.ReservedQuantity = Math.Max(0m, fg.ReservedQuantity - item.Quantity);
                    fg.RecalculateAvailable();
                    fg.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
