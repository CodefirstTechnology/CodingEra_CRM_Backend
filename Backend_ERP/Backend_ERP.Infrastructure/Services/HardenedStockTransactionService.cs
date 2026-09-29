using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Backend_ERP.Application.DTOs.Inventory;
using Backend_ERP.Domain.Entities;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Backend_ERP.Infrastructure.Services
{
    public interface IHardenedStockTransactionService
    {
        Task<List<string>> ExecuteBatchStockMovementAsync(BatchStockMovementRequestDto dto, CancellationToken ct = default);
    }

    public class HardenedStockTransactionService : IHardenedStockTransactionService
    {
        private readonly ERPDbContext _dbContext;
        private readonly ILogger<HardenedStockTransactionService> _logger;

        public HardenedStockTransactionService(ERPDbContext dbContext, ILogger<HardenedStockTransactionService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<List<string>> ExecuteBatchStockMovementAsync(BatchStockMovementRequestDto dto, CancellationToken ct = default)
        {
            if (dto.Lines == null || dto.Lines.Count == 0)
                throw new ArgumentException("Stock movement request must contain at least one line item.", nameof(dto.Lines));

            // 1. IDEMPOTENCY GATE: Return cached result if key was already executed
            var existingTxns = await _dbContext.StockTransactions
                .AsNoTracking()
                .Where(x => x.IdempotencyKey == dto.IdempotencyKey)
                .Select(x => x.TransactionNumber)
                .ToListAsync(ct);

            if (existingTxns.Count > 0)
            {
                _logger.LogWarning("Idempotency key {IdempotencyKey} already executed. Returning cached transactions.", dto.IdempotencyKey);
                return existingTxns;
            }

            // 2. DETERMINISTIC LOCKING ORDER: Sort target items by (ItemId, WarehouseId, BinId) ASC to guarantee deadlock-free execution
            var sortedLines = dto.Lines
                .OrderBy(x => x.ItemId)
                .ThenBy(x => x.WarehouseId)
                .ThenBy(x => x.BinId)
                .ToList();

            var generatedTxnNumbers = new List<string>();

            await using var dbTransaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                foreach (var line in sortedLines)
                {
                    // PESSIMISTIC LOCK: Execute SELECT FOR UPDATE in deterministic sorted order
                    var material = await _dbContext.RawMaterials
                        .FromSqlInterpolated($"SELECT * FROM raw_materials WHERE \"Id\" = {line.ItemId} FOR UPDATE")
                        .SingleOrDefaultAsync(ct);

                    if (material != null && line.SecondaryQuantity.HasValue)
                    {
                        // 3. CATCH-WEIGHT DUAL UOM VARIANCE VALIDATION
                        decimal nominalWeight = line.SecondaryQuantity.Value * 1.0m;
                        if (nominalWeight > 0)
                        {
                            decimal variancePercent = Math.Abs((line.Quantity - nominalWeight) / nominalWeight) * 100m;
                            if (variancePercent > material.CatchWeightTolerancePercent)
                            {
                                throw new InvalidOperationException(
                                    $"Catch-weight variance for material {material.MaterialCode} ({variancePercent:F2}%) exceeds configured limit ({material.CatchWeightTolerancePercent:F2}%).");
                            }
                        }
                    }

                    bool isInward = dto.TransactionType is TransactionType.GRN_IN or TransactionType.PROD_OUT or TransactionType.ADJ_POS_IN;

                    var txnNo = $"TXN-{(isInward ? "IN" : "OUT")}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
                    generatedTxnNumbers.Add(txnNo);

                    var ledgerEntry = new StockTransaction
                    {
                        TransactionNumber = txnNo,
                        IdempotencyKey = dto.IdempotencyKey,
                        TransactionType = isInward ? StockTxnType.StockIn : StockTxnType.StockOut,
                        MaterialId = line.ItemId.GetHashCode(),
                        WarehouseId = line.WarehouseId.GetHashCode(),
                        Quantity = line.Quantity,
                        SecondaryQty = line.SecondaryQuantity,
                        UnitCost = line.UnitCost,
                        ReferenceType = StockReferenceType.PO,
                        ReferenceNumber = dto.ReferenceDocumentNo,
                        TransactionDate = DateTime.UtcNow,
                        CreatedBy = dto.UserId.ToString(),
                        CreatedAt = DateTime.UtcNow
                    };

                    _dbContext.StockTransactions.Add(ledgerEntry);

                    // 4. TRANSACTIONAL OUTBOX PERSISTENCE: Save domain event payload within the exact DB transaction boundary
                    if (!isInward && material != null && material.AvailableStock <= material.ReorderLevel)
                    {
                        var lowStockPayload = new { material.Id, material.MaterialCode, material.WarehouseId, material.AvailableStock, material.ReorderLevel };
                        _dbContext.OutboxMessages.Add(new OutboxMessageEntity
                        {
                            EventType = "LowStockThresholdReachedEvent",
                            PayloadJson = JsonSerializer.Serialize(lowStockPayload)
                        });
                    }
                }

                await _dbContext.SaveChangesAsync(ct);
                await dbTransaction.CommitAsync(ct);

                return generatedTxnNumbers;
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync(ct);
                _logger.LogError(ex, "Failed to execute batch stock movement for IdempotencyKey {IdempotencyKey}.", dto.IdempotencyKey);
                throw;
            }
        }
    }
}
