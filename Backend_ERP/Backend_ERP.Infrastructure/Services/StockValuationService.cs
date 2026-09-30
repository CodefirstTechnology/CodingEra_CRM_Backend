using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backend_ERP.Application.DTOs.Inventory;
using Backend_ERP.Domain.Entities;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Backend_ERP.Infrastructure.Services
{
    public interface IStockValuationService
    {
        Task RecordInwardReceiptAsync(ValuationInwardReceiptDto dto, CancellationToken ct = default);
        Task<ValuationDepletionResultDto> ConsumeValuationOnStockOutAsync(ValuationOutwardDepletionDto dto, CancellationToken ct = default);
        Task<StockValuationSummaryDto?> GetValuationSummaryAsync(int itemId, Guid warehouseId, CancellationToken ct = default);
    }

    public class StockValuationService : IStockValuationService
    {
        private readonly ERPDbContext _dbContext;
        private readonly ILogger<StockValuationService> _logger;

        public StockValuationService(ERPDbContext dbContext, ILogger<StockValuationService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task RecordInwardReceiptAsync(ValuationInwardReceiptDto dto, CancellationToken ct = default)
        {
            if (dto.Quantity <= 0)
                throw new ArgumentException("Inward receipt quantity must be strictly greater than zero.", nameof(dto.Quantity));

            if (dto.UnitCost < 0)
                throw new ArgumentException("Unit cost cannot be negative.", nameof(dto.UnitCost));

            await using var tx = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                // 1. Create FIFO Valuation Layer
                var layer = new ItemValuationLayerEntity
                {
                    ItemId = dto.ItemId,
                    WarehouseId = dto.WarehouseId,
                    ReceiptDate = dto.ReceiptDate ?? DateTime.UtcNow,
                    UnitCost = dto.UnitCost,
                    InitialQty = dto.Quantity,
                    RemainingQty = dto.Quantity,
                    IsDepleted = false,
                    CreatedAt = DateTime.UtcNow
                };
                _dbContext.ItemValuationLayers.Add(layer);

                // 2. Pessimistic Row-Lock on WAC Balance
                var wacBalances = await _dbContext.ItemWacBalances
                    .FromSqlInterpolated($@"
                        SELECT * FROM item_wac_balances
                        WHERE item_id = {dto.ItemId} AND warehouse_id = {dto.WarehouseId}
                        FOR UPDATE")
                    .ToListAsync(ct);

                var wacBalance = wacBalances.FirstOrDefault();

                if (wacBalance == null)
                {
                    wacBalance = new ItemWacBalanceEntity
                    {
                        ItemId = dto.ItemId,
                        WarehouseId = dto.WarehouseId,
                        TotalQty = dto.Quantity,
                        TotalValue = Math.Round(dto.Quantity * dto.UnitCost, 4),
                        WeightedAvgCost = dto.UnitCost,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _dbContext.ItemWacBalances.Add(wacBalance);
                }
                else
                {
                    decimal newTotalQty = wacBalance.TotalQty + dto.Quantity;
                    decimal newTotalValue = wacBalance.TotalValue + Math.Round(dto.Quantity * dto.UnitCost, 4);
                    decimal newWac = newTotalQty > 0 ? Math.Round(newTotalValue / newTotalQty, 6) : 0m;

                    wacBalance.TotalQty = newTotalQty;
                    wacBalance.TotalValue = newTotalValue;
                    wacBalance.WeightedAvgCost = newWac;
                    wacBalance.UpdatedAt = DateTime.UtcNow;
                }

                await _dbContext.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                _logger.LogInformation("Recorded inward valuation receipt for Item {ItemId} at WH {WarehouseId}. Qty: {Qty}, UnitCost: {Cost}.", dto.ItemId, dto.WarehouseId, dto.Quantity, dto.UnitCost);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                _logger.LogError(ex, "Failed to record inward valuation receipt for Item {ItemId}.", dto.ItemId);
                throw;
            }
        }

        public async Task<ValuationDepletionResultDto> ConsumeValuationOnStockOutAsync(ValuationOutwardDepletionDto dto, CancellationToken ct = default)
        {
            if (dto.Quantity <= 0)
                throw new ArgumentException("Depletion quantity must be strictly greater than zero.", nameof(dto.Quantity));

            await using var tx = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                // 1. Query Active FIFO Layers ordered by receipt_date ASC, layer_id ASC FOR UPDATE
                var activeLayers = await _dbContext.ItemValuationLayers
                    .FromSqlInterpolated($@"
                        SELECT * FROM item_valuation_layers
                        WHERE item_id = {dto.ItemId} AND warehouse_id = {dto.WarehouseId} AND is_depleted = FALSE
                        ORDER BY receipt_date ASC, layer_id ASC
                        FOR UPDATE")
                    .ToListAsync(ct);

                decimal remainingToDeplete = dto.Quantity;
                decimal totalCostDepleted = 0m;
                int layersDepletedCount = 0;

                foreach (var layer in activeLayers)
                {
                    if (remainingToDeplete <= 0) break;

                    decimal takeQty = Math.Min(remainingToDeplete, layer.RemainingQty);
                    totalCostDepleted += Math.Round(takeQty * layer.UnitCost, 4);
                    layer.RemainingQty -= takeQty;
                    remainingToDeplete -= takeQty;

                    if (layer.RemainingQty <= 0)
                    {
                        layer.RemainingQty = 0m;
                        layer.IsDepleted = true;
                        layersDepletedCount++;
                    }
                }

                // If remainingToDeplete > 0, fallback to last known unit cost or zero
                if (remainingToDeplete > 0)
                {
                    _logger.LogWarning("Depletion quantity ({Qty}) exceeded active FIFO layers for Item {ItemId}. Uncovered Qty: {UncoveredQty}.", dto.Quantity, dto.ItemId, remainingToDeplete);
                }

                // 2. Lock & Update WAC Balance
                var wacBalances = await _dbContext.ItemWacBalances
                    .FromSqlInterpolated($@"
                        SELECT * FROM item_wac_balances
                        WHERE item_id = {dto.ItemId} AND warehouse_id = {dto.WarehouseId}
                        FOR UPDATE")
                    .ToListAsync(ct);

                var wacBalance = wacBalances.FirstOrDefault();
                if (wacBalance != null)
                {
                    decimal newTotalQty = Math.Max(0m, wacBalance.TotalQty - dto.Quantity);
                    decimal newTotalValue = Math.Max(0m, wacBalance.TotalValue - totalCostDepleted);
                    decimal newWac = newTotalQty > 0 ? Math.Round(newTotalValue / newTotalQty, 6) : 0m;

                    wacBalance.TotalQty = newTotalQty;
                    wacBalance.TotalValue = newTotalValue;
                    wacBalance.WeightedAvgCost = newWac;
                    wacBalance.UpdatedAt = DateTime.UtcNow;
                }

                await _dbContext.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                decimal avgCostPerUnit = dto.Quantity > 0 ? Math.Round(totalCostDepleted / dto.Quantity, 6) : 0m;

                return new ValuationDepletionResultDto
                {
                    TotalCostDepleted = totalCostDepleted,
                    AverageCostPerUnit = avgCostPerUnit,
                    LayersDepleted = layersDepletedCount
                };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                _logger.LogError(ex, "Failed to consume valuation on stock out for Item {ItemId}.", dto.ItemId);
                throw;
            }
        }

        public async Task<StockValuationSummaryDto?> GetValuationSummaryAsync(int itemId, Guid warehouseId, CancellationToken ct = default)
        {
            var wacBalance = await _dbContext.ItemWacBalances
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.ItemId == itemId && b.WarehouseId == warehouseId, ct);

            var activeLayers = await _dbContext.ItemValuationLayers
                .AsNoTracking()
                .Where(l => l.ItemId == itemId && l.WarehouseId == warehouseId && !l.IsDepleted)
                .ToListAsync(ct);

            if (wacBalance == null && activeLayers.Count == 0)
                return null;

            decimal totalFifoValue = activeLayers.Sum(l => l.RemainingQty * l.UnitCost);

            return new StockValuationSummaryDto
            {
                ItemId = itemId,
                WarehouseId = warehouseId,
                TotalQty = wacBalance?.TotalQty ?? activeLayers.Sum(l => l.RemainingQty),
                TotalWacValue = wacBalance?.TotalValue ?? 0m,
                WeightedAvgCost = wacBalance?.WeightedAvgCost ?? 0m,
                TotalFifoValue = Math.Round(totalFifoValue, 4),
                ActiveFifoLayers = activeLayers.Count
            };
        }
    }
}
