using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using ERP.Infrastructure.Data;

namespace Backend_ERP.Infrastructure.Production
{
    public class AllocationExecutionResult
    {
        public bool IsSuccess { get; set; }
        public Guid ProductionPlanId { get; set; }
        public int TotalItemsProcessed { get; set; }
        public int FullyAllocatedCount { get; set; }
        public int ShortageCount { get; set; }
        public List<Guid> GeneratedMrnIds { get; set; } = new();
        public string ExecutionMessage { get; set; } = string.Empty;
    }

    public interface IStockAllocationEngine
    {
        Task<AllocationExecutionResult> ExecuteHardAllocationAtomicAsync(Guid productionPlanId, Guid warehouseId, string actingUser, CancellationToken ct = default);
    }

    public class StockAllocationEngine : IStockAllocationEngine
    {
        private readonly ERPDbContext _dbContext;
        private readonly ILogger<StockAllocationEngine> _logger;

        public StockAllocationEngine(ERPDbContext dbContext, ILogger<StockAllocationEngine> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AllocationExecutionResult> ExecuteHardAllocationAtomicAsync(
            Guid productionPlanId, Guid warehouseId, string actingUser, CancellationToken ct = default)
        {
            var result = new AllocationExecutionResult
            {
                ProductionPlanId = productionPlanId,
                IsSuccess = false
            };

            // Begin explicit Database Transaction
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

            try
            {
                // 1. Fetch all material allocations for this production plan
                var allocationsRaw = await _dbContext.Database.SqlQueryRaw<AllocationRecordDto>(@"
                    SELECT 
                        ma.id AS AllocationId,
                        ma.production_plan_item_id AS PlanItemId,
                        ma.raw_material_item_id AS RawMaterialItemId,
                        ma.gross_required_qty AS GrossRequiredQty,
                        ma.soft_reserved_qty AS SoftReservedQty,
                        ma.hard_reserved_qty AS HardReservedQty,
                        ma.shortage_qty AS ShortageQty,
                        ma.allocation_status AS AllocationStatus
                    FROM production_plan_material_allocations ma
                    INNER JOIN production_plan_items ppi ON ppi.id = ma.production_plan_item_id
                    WHERE ppi.production_plan_id = {0}
                      AND ma.allocation_status != 'HARD_RESERVED'
                ", productionPlanId).ToListAsync(ct);

                if (!allocationsRaw.Any())
                {
                    result.IsSuccess = true;
                    result.ExecutionMessage = "No pending stock allocations found for this production plan.";
                    return result;
                }

                // 2. CRITICAL DEADLOCK PREVENTION GUARD:
                // Sort allocations deterministically by RawMaterialItemId then AllocationId
                var sortedAllocations = allocationsRaw
                    .OrderBy(a => a.RawMaterialItemId)
                    .ThenBy(a => a.AllocationId)
                    .ToList();

                result.TotalItemsProcessed = sortedAllocations.Count;

                var conn = _dbContext.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                {
                    await conn.OpenAsync(ct);
                }

                foreach (var alloc in sortedAllocations)
                {
                    decimal remainingNeeded = alloc.GrossRequiredQty - alloc.HardReservedQty;

                    if (remainingNeeded <= 0)
                    {
                        result.FullyAllocatedCount++;
                        continue;
                    }

                    // 3. Execute Deterministic FOR UPDATE Lock on Inventory Batches using FEFO order
                    const string lockBatchSql = @"
                        SELECT id, unallocated_qty, hard_reserved_qty, expiry_date
                        FROM inventory_batches
                        WHERE item_id = '{0}' AND warehouse_id = '{1}' AND unallocated_qty > 0 AND is_deleted = FALSE
                        ORDER BY expiry_date ASC, created_at ASC
                        FOR UPDATE;
                    ";

                    using var lockCmd = conn.CreateCommand();
                    lockCmd.Transaction = transaction.GetDbTransaction();
                    lockCmd.CommandText = string.Format(lockBatchSql, alloc.RawMaterialItemId, warehouseId);

                    var batchLocks = new List<BatchLockRow>();
                    using (var reader = await lockCmd.ExecuteReaderAsync(ct))
                    {
                        while (await reader.ReadAsync(ct))
                        {
                            batchLocks.Add(new BatchLockRow
                            {
                                BatchId = reader.GetGuid(0),
                                UnallocatedQty = reader.GetDecimal(1),
                                HardReservedQty = reader.GetDecimal(2)
                            });
                        }
                    }

                    decimal allocatedThisItem = 0m;
                    Guid? selectedBatchId = null;

                    foreach (var batch in batchLocks)
                    {
                        if (remainingNeeded <= 0) break;

                        decimal allocFromBatch = Math.Min(batch.UnallocatedQty, remainingNeeded);
                        remainingNeeded -= allocFromBatch;
                        allocatedThisItem += allocFromBatch;
                        selectedBatchId = batch.BatchId;

                        // Update Batch record atomically
                        using var updateBatchCmd = conn.CreateCommand();
                        updateBatchCmd.Transaction = transaction.GetDbTransaction();
                        updateBatchCmd.CommandText = @"
                            UPDATE inventory_batches
                            SET unallocated_qty = unallocated_qty - {0},
                                hard_reserved_qty = hard_reserved_qty + {0},
                                updated_at = CURRENT_TIMESTAMP
                            WHERE id = '{1}';
                        ".Replace("{0}", allocFromBatch.ToString(System.Globalization.CultureInfo.InvariantCulture))
                         .Replace("{1}", batch.BatchId.ToString());

                        await updateBatchCmd.ExecuteNonQueryAsync(ct);
                    }

                    // 4. Update Material Allocation Record & Transactional Outbox
                    string newStatus;
                    decimal shortage;
                    Guid? mrnId = null;

                    if (remainingNeeded > 0)
                    {
                        newStatus = "SHORTAGE";
                        shortage = remainingNeeded;
                        result.ShortageCount++;

                        // Raise Auto MRN Requisition
                        mrnId = Guid.NewGuid();
                        result.GeneratedMrnIds.Add(mrnId.Value);

                        // Insert Transactional Outbox Message for Shortage Event
                        var shortageEventPayload = JsonSerializer.Serialize(new
                        {
                            EventId = Guid.NewGuid(),
                            ProductionPlanId = productionPlanId,
                            RawMaterialItemId = alloc.RawMaterialItemId,
                            ShortageQuantity = shortage,
                            GeneratedMrnId = mrnId,
                            OccurredAt = DateTime.UtcNow
                        });

                        using var outboxCmd = conn.CreateCommand();
                        outboxCmd.Transaction = transaction.GetDbTransaction();
                        outboxCmd.CommandText = @"
                            INSERT INTO outbox_messages (outbox_id, event_type, payload_json, created_at, retry_count)
                            VALUES (gen_random_uuid(), 'Production.MaterialShortageDetected', '{0}', CURRENT_TIMESTAMP, 0);
                        ".Replace("{0}", shortageEventPayload.Replace("'", "''"));

                        await outboxCmd.ExecuteNonQueryAsync(ct);
                    }
                    else
                    {
                        newStatus = "HARD_RESERVED";
                        shortage = 0m;
                        result.FullyAllocatedCount++;
                    }

                    using var updateAllocCmd = conn.CreateCommand();
                    updateAllocCmd.Transaction = transaction.GetDbTransaction();
                    updateAllocCmd.CommandText = @"
                        UPDATE production_plan_material_allocations
                        SET hard_reserved_qty = hard_reserved_qty + {0},
                            shortage_qty = {1},
                            allocation_status = '{2}',
                            batch_id = {3},
                            indent_mrn_id = {4},
                            updated_at = CURRENT_TIMESTAMP
                        WHERE id = '{5}';
                    ".Replace("{0}", allocatedThisItem.ToString(System.Globalization.CultureInfo.InvariantCulture))
                     .Replace("{1}", shortage.ToString(System.Globalization.CultureInfo.InvariantCulture))
                     .Replace("{2}", newStatus)
                     .Replace("{3}", selectedBatchId.HasValue ? $"'{selectedBatchId}'" : "NULL")
                     .Replace("{4}", mrnId.HasValue ? $"'{mrnId}'" : "NULL")
                     .Replace("{5}", alloc.AllocationId.ToString());

                    await updateAllocCmd.ExecuteNonQueryAsync(ct);
                }

                // 5. Publish Plan Approved Outbox Event
                var planApprovedPayload = JsonSerializer.Serialize(new
                {
                    EventId = Guid.NewGuid(),
                    ProductionPlanId = productionPlanId,
                    ApprovedBy = actingUser,
                    ApprovedAt = DateTime.UtcNow
                });

                using var planOutboxCmd = conn.CreateCommand();
                planOutboxCmd.Transaction = transaction.GetDbTransaction();
                planOutboxCmd.CommandText = @"
                    INSERT INTO outbox_messages (outbox_id, event_type, payload_json, created_at, retry_count)
                    VALUES (gen_random_uuid(), 'Production.PlanApprovedAndAllocated', '{0}', CURRENT_TIMESTAMP, 0);
                ".Replace("{0}", planApprovedPayload.Replace("'", "''"));

                await planOutboxCmd.ExecuteNonQueryAsync(ct);

                // Commit Transaction
                await transaction.CommitAsync(ct);

                result.IsSuccess = true;
                result.ExecutionMessage = $"Successfully executed atomic stock allocation for Plan {productionPlanId}. Allocated: {result.FullyAllocatedCount}, Shortages: {result.ShortageCount}.";
                _logger.LogInformation(result.ExecutionMessage);
                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Concurrency deadlock or error during atomic stock allocation for plan {ProductionPlanId}", productionPlanId);
                result.IsSuccess = false;
                result.ExecutionMessage = $"Failed stock allocation: {ex.Message}";
                throw;
            }
        }

        private class AllocationRecordDto
        {
            public Guid AllocationId { get; set; }
            public Guid PlanItemId { get; set; }
            public Guid RawMaterialItemId { get; set; }
            public decimal GrossRequiredQty { get; set; }
            public decimal SoftReservedQty { get; set; }
            public decimal HardReservedQty { get; set; }
            public decimal ShortageQty { get; set; }
            public string AllocationStatus { get; set; } = string.Empty;
        }

        private class BatchLockRow
        {
            public Guid BatchId { get; set; }
            public decimal UnallocatedQty { get; set; }
            public decimal HardReservedQty { get; set; }
        }
    }
}
