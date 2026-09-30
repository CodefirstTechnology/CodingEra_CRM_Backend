using System;
using System.Collections.Generic;
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
    public interface IBatchAllocationService
    {
        Task<List<FEFOAllocationResultDto>> AllocateFEFOAsync(BatchAllocationRequestDto dto, CancellationToken ct = default);
        Task QuarantineBatchAsync(Guid batchId, BatchQuarantineRequestDto dto, CancellationToken ct = default);
    }

    public class BatchAllocationService : IBatchAllocationService
    {
        private readonly ERPDbContext _dbContext;
        private readonly ILogger<BatchAllocationService> _logger;

        public BatchAllocationService(ERPDbContext dbContext, ILogger<BatchAllocationService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<List<FEFOAllocationResultDto>> AllocateFEFOAsync(BatchAllocationRequestDto dto, CancellationToken ct = default)
        {
            if (dto.RequestedQty <= 0)
                throw new ArgumentException("Requested quantity must be strictly greater than zero.", nameof(dto.RequestedQty));

            DateTime minCutoffDate = DateTime.UtcNow.AddDays(dto.MinUsableShelfLifeDays);

            // Query active, unlocked batches ordered by ExpiryDate ASC, BatchId ASC with pessimistic row locks
            var eligibleBatches = await _dbContext.Set<ItemBatchEntity>()
                .FromSqlInterpolated($@"
                    SELECT * FROM item_batches 
                    WHERE item_id = {dto.ItemId}
                      AND is_locked = FALSE
                      AND qc_status = 'Released'
                      AND expiry_date >= {minCutoffDate}
                    ORDER BY expiry_date ASC, batch_id ASC
                    FOR UPDATE")
                .ToListAsync(ct);

            if (eligibleBatches.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No active released batches found passing the minimum shelf-life threshold ({dto.MinUsableShelfLifeDays} days).");
            }

            var allocationResults = new List<FEFOAllocationResultDto>();
            decimal remainingQty = dto.RequestedQty;

            foreach (var batch in eligibleBatches)
            {
                if (remainingQty <= 0) break;

                // Lookup active bin allocation
                var defaultBin = await _dbContext.Set<WarehouseBinEntity>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.WarehouseId == dto.WarehouseId && b.Status == BinStatus.Active, ct);

                Guid binId = defaultBin?.BinId ?? Guid.Empty;

                decimal allocateQty = Math.Min(remainingQty, 100.0000m); // Standard batch chunk allocation
                int remainingDays = (int)(batch.ExpiryDate - DateTime.UtcNow).TotalDays;

                allocationResults.Add(new FEFOAllocationResultDto(
                    batch.BatchId,
                    batch.BatchNumber,
                    binId,
                    allocateQty,
                    batch.ExpiryDate,
                    remainingDays
                ));

                remainingQty -= allocateQty;
            }

            return allocationResults;
        }

        public async Task QuarantineBatchAsync(Guid batchId, BatchQuarantineRequestDto dto, CancellationToken ct = default)
        {
            var batch = await _dbContext.Set<ItemBatchEntity>().FindAsync(new object[] { batchId }, ct)
                ?? throw new KeyNotFoundException($"Batch ID {batchId} not found.");

            batch.IsLocked = true;
            batch.QcStatus = "Quarantined";

            // Persist BatchQuarantinedEvent to Outbox
            var eventPayload = System.Text.Json.JsonSerializer.Serialize(new { batchId, batch.BatchNumber, dto.QuarantineReason, dto.UserId });
            _dbContext.OutboxMessages.Add(new OutboxMessageEntity
            {
                EventType = "BatchQuarantinedEvent",
                PayloadJson = eventPayload
            });

            await _dbContext.SaveChangesAsync(ct);
            _logger.LogWarning("Batch {BatchNumber} placed on quarantine hold. Reason: {Reason}", batch.BatchNumber, dto.QuarantineReason);
        }
    }
}
