using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Production.Dtos;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Backend_ERP.Infrastructure.Production
{
    public interface IAtomicProductionEntryHandler
    {
        Task<ProductionEntryResponseDto> ProcessProductionEntryAsync(CreateProductionEntryRequestDto request, string actingUser, CancellationToken ct = default);
    }

    public class AtomicProductionEntryHandler : IAtomicProductionEntryHandler
    {
        private readonly ERPDbContext _dbContext;
        private readonly ILogger<AtomicProductionEntryHandler> _logger;

        public AtomicProductionEntryHandler(ERPDbContext dbContext, ILogger<AtomicProductionEntryHandler> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ProductionEntryResponseDto> ProcessProductionEntryAsync(
            CreateProductionEntryRequestDto request,
            string actingUser,
            CancellationToken ct = default)
        {
            if (request.GoodQuantity < 0 || request.ScrappedQuantity < 0)
            {
                throw new ArgumentException("Good and Scrapped quantities must be non-negative.");
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

            try
            {
                var conn = _dbContext.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

                // 1. Fetch Operation and linked Work Order
                using var opCmd = conn.CreateCommand();
                opCmd.Transaction = transaction.GetDbTransaction();
                opCmd.CommandText = @"
                    SELECT woo.id, woo.work_order_id, woo.produced_quantity, woo.scrapped_quantity, woo.status, wo.completed_quantity, wo.scrapped_quantity, wo.target_quantity
                    FROM work_order_operations woo
                    INNER JOIN work_orders wo ON wo.id = woo.work_order_id
                    WHERE woo.id = @opId
                    FOR UPDATE;
                ";

                var paramOpId = opCmd.CreateParameter();
                paramOpId.ParameterName = "@opId";
                paramOpId.Value = request.WorkOrderOperationId;
                opCmd.Parameters.Add(paramOpId);

                Guid workOrderId = Guid.Empty;
                decimal currentOpProduced = 0, currentOpScrapped = 0;
                decimal currentWoCompleted = 0, currentWoScrapped = 0, woTargetQty = 0;
                string opStatus = "PENDING";

                using (var reader = await opCmd.ExecuteReaderAsync(ct))
                {
                    if (await reader.ReadAsync(ct))
                    {
                        workOrderId = reader.GetGuid(1);
                        currentOpProduced = reader.GetDecimal(2);
                        currentOpScrapped = reader.GetDecimal(3);
                        opStatus = reader.GetString(4);
                        currentWoCompleted = reader.GetDecimal(5);
                        currentWoScrapped = reader.GetDecimal(6);
                        woTargetQty = reader.GetDecimal(7);
                    }
                    else
                    {
                        throw new KeyNotFoundException($"Work Order Operation {request.WorkOrderOperationId} not found.");
                    }
                }

                // 2. Generate Production Entry Number
                string entryNumber = $"PE-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(10000, 99999)}";
                Guid productionEntryId = Guid.NewGuid();

                // 3. Create Production Entry record
                using var insertEntryCmd = conn.CreateCommand();
                insertEntryCmd.Transaction = transaction.GetDbTransaction();
                insertEntryCmd.CommandText = @"
                    INSERT INTO shop_floor_production_entries (
                        id, entry_number, work_order_operation_id, shift_id, operator_id, machine_id,
                        good_quantity, scrapped_quantity, start_time, end_time, status, created_at
                    ) VALUES (
                        @id, @entryNum, @opId, @shift, @operator, @machine,
                        @goodQty, @scrapQty, @start, @end, 'SUBMITTED', CURRENT_TIMESTAMP
                    );
                ";

                AddParam(insertEntryCmd, "@id", productionEntryId);
                AddParam(insertEntryCmd, "@entryNum", entryNumber);
                AddParam(insertEntryCmd, "@opId", request.WorkOrderOperationId);
                AddParam(insertEntryCmd, "@shift", request.ShiftId);
                AddParam(insertEntryCmd, "@operator", request.OperatorId);
                AddParam(insertEntryCmd, "@machine", request.MachineId);
                AddParam(insertEntryCmd, "@goodQty", request.GoodQuantity);
                AddParam(insertEntryCmd, "@scrapQty", request.ScrappedQuantity);
                AddParam(insertEntryCmd, "@start", request.StartTime);
                AddParam(insertEntryCmd, "@end", request.EndTime);

                await insertEntryCmd.ExecuteNonQueryAsync(ct);

                // 4. Update Operation & Work Order Quantities
                decimal newOpProduced = currentOpProduced + request.GoodQuantity;
                decimal newOpScrapped = currentOpScrapped + request.ScrappedQuantity;
                string newOpStatus = newOpProduced >= woTargetQty ? "COMPLETED" : "IN_PROGRESS";

                using var updateOpCmd = conn.CreateCommand();
                updateOpCmd.Transaction = transaction.GetDbTransaction();
                updateOpCmd.CommandText = @"
                    UPDATE work_order_operations
                    SET produced_quantity = @pQty, scrapped_quantity = @sQty, status = @status
                    WHERE id = @opId;
                ";
                AddParam(updateOpCmd, "@pQty", newOpProduced);
                AddParam(updateOpCmd, "@sQty", newOpScrapped);
                AddParam(updateOpCmd, "@status", newOpStatus);
                AddParam(updateOpCmd, "@opId", request.WorkOrderOperationId);
                await updateOpCmd.ExecuteNonQueryAsync(ct);

                decimal newWoCompleted = currentWoCompleted + request.GoodQuantity;
                decimal newWoScrapped = currentWoScrapped + request.ScrappedQuantity;
                string newWoStatus = newWoCompleted >= woTargetQty ? "COMPLETED" : "IN_PROGRESS";

                using var updateWoCmd = conn.CreateCommand();
                updateWoCmd.Transaction = transaction.GetDbTransaction();
                updateWoCmd.CommandText = @"
                    UPDATE work_orders
                    SET completed_quantity = @cQty, scrapped_quantity = @sQty, status = @status, updated_at = CURRENT_TIMESTAMP, updated_by = @actingUser
                    WHERE id = @woId;
                ";
                AddParam(updateWoCmd, "@cQty", newWoCompleted);
                AddParam(updateWoCmd, "@sQty", newWoScrapped);
                AddParam(updateWoCmd, "@status", newWoStatus);
                AddParam(updateWoCmd, "@actingUser", actingUser);
                AddParam(updateWoCmd, "@woId", workOrderId);
                await updateWoCmd.ExecuteNonQueryAsync(ct);

                // 5. Raw Material Consumption & Inventory Batch Deduction (Backflush vs Manual)
                foreach (var mat in request.MaterialConsumptions)
                {
                    decimal remainingToDeduct = mat.ActualConsumedQuantity;

                    if (mat.BatchId.HasValue && mat.BatchId.Value != Guid.Empty)
                    {
                        // Specific Batch Manual Deduction
                        using var batchCmd = conn.CreateCommand();
                        batchCmd.Transaction = transaction.GetDbTransaction();
                        batchCmd.CommandText = @"
                            SELECT ""RemainingQuantity"", ""ConsumedQuantity""
                            FROM ""InventoryBatches""
                            WHERE ""Id"" = @batchId
                            FOR UPDATE;
                        ";
                        AddParam(batchCmd, "@batchId", mat.BatchId.Value);

                        decimal avail = 0, consumed = 0;
                        using (var r = await batchCmd.ExecuteReaderAsync(ct))
                        {
                            if (await r.ReadAsync(ct))
                            {
                                avail = r.GetDecimal(0);
                                consumed = r.GetDecimal(1);
                            }
                            else
                            {
                                throw new InvalidOperationException($"Batch {mat.BatchId.Value} not found.");
                            }
                        }

                        if (avail < mat.ActualConsumedQuantity)
                        {
                            throw new InvalidOperationException($"Insufficient inventory in batch {mat.BatchId.Value}. Available: {avail}, Requested: {mat.ActualConsumedQuantity}");
                        }

                        using var updateBatchCmd = conn.CreateCommand();
                        updateBatchCmd.Transaction = transaction.GetDbTransaction();
                        updateBatchCmd.CommandText = @"
                            UPDATE ""InventoryBatches""
                            SET ""RemainingQuantity"" = ""RemainingQuantity"" - @qty,
                                ""ConsumedQuantity"" = ""ConsumedQuantity"" + @qty,
                                ""UpdatedAt"" = CURRENT_TIMESTAMP,
                                ""UpdatedBy"" = @actingUser
                            WHERE ""Id"" = @batchId;
                        ";
                        AddParam(updateBatchCmd, "@qty", mat.ActualConsumedQuantity);
                        AddParam(updateBatchCmd, "@actingUser", actingUser);
                        AddParam(updateBatchCmd, "@batchId", mat.BatchId.Value);
                        await updateBatchCmd.ExecuteNonQueryAsync(ct);
                    }
                    else
                    {
                        // FIFO Backflushing across available batches
                        using var fifoCmd = conn.CreateCommand();
                        fifoCmd.Transaction = transaction.GetDbTransaction();
                        fifoCmd.CommandText = @"
                            SELECT ""Id"", ""RemainingQuantity""
                            FROM ""InventoryBatches""
                            WHERE ""RemainingQuantity"" > 0
                            ORDER BY ""ManufacturingDate"" ASC
                            FOR UPDATE;
                        ";

                        var availableBatches = new List<(int batchId, decimal qty)>();
                        using (var r = await fifoCmd.ExecuteReaderAsync(ct))
                        {
                            while (await r.ReadAsync(ct))
                            {
                                availableBatches.Add((r.GetInt32(0), r.GetDecimal(1)));
                            }
                        }

                        decimal totalAvail = availableBatches.Sum(b => b.qty);
                        if (totalAvail < mat.ActualConsumedQuantity)
                        {
                            _logger.LogWarning("Insufficient stock for material backflush. Available: {Avail}, Required: {Req}", totalAvail, mat.ActualConsumedQuantity);
                        }

                        foreach (var b in availableBatches)
                        {
                            if (remainingToDeduct <= 0) break;
                            decimal deduct = Math.Min(b.qty, remainingToDeduct);

                            using var updateFifoCmd = conn.CreateCommand();
                            updateFifoCmd.Transaction = transaction.GetDbTransaction();
                            updateFifoCmd.CommandText = @"
                                UPDATE ""InventoryBatches""
                                SET ""RemainingQuantity"" = ""RemainingQuantity"" - @deduct,
                                    ""ConsumedQuantity"" = ""ConsumedQuantity"" + @deduct,
                                    ""UpdatedAt"" = CURRENT_TIMESTAMP,
                                    ""UpdatedBy"" = @actingUser
                                WHERE ""Id"" = @bId;
                            ";
                            AddParam(updateFifoCmd, "@deduct", deduct);
                            AddParam(updateFifoCmd, "@actingUser", actingUser);
                            AddParam(updateFifoCmd, "@bId", b.batchId);
                            await updateFifoCmd.ExecuteNonQueryAsync(ct);

                            remainingToDeduct -= deduct;
                        }
                    }

                    // Insert Material Consumption Log
                    decimal variance = mat.ActualConsumedQuantity - mat.StandardBomQuantity;
                    using var matLogCmd = conn.CreateCommand();
                    matLogCmd.Transaction = transaction.GetDbTransaction();
                    matLogCmd.CommandText = @"
                        INSERT INTO material_consumption_logs (
                            id, production_entry_id, raw_material_item_id, batch_id,
                            standard_bom_quantity, actual_consumed_quantity, variance_quantity,
                            consumption_type, created_at
                        ) VALUES (
                            gen_random_uuid(), @entryId, @matId, @batchId,
                            @stdQty, @actQty, @varQty, @cType, CURRENT_TIMESTAMP
                        );
                    ";
                    AddParam(matLogCmd, "@entryId", productionEntryId);
                    AddParam(matLogCmd, "@matId", mat.RawMaterialItemId);
                    AddParam(matLogCmd, "@batchId", mat.BatchId.HasValue ? (object)mat.BatchId.Value : DBNull.Value);
                    AddParam(matLogCmd, "@stdQty", mat.StandardBomQuantity);
                    AddParam(matLogCmd, "@actQty", mat.ActualConsumedQuantity);
                    AddParam(matLogCmd, "@varQty", variance);
                    AddParam(matLogCmd, "@cType", mat.ConsumptionType);
                    await matLogCmd.ExecuteNonQueryAsync(ct);
                }

                // 6. Record Rejection Tracking Logs
                foreach (var rej in request.Rejections)
                {
                    decimal totalLoss = rej.RejectedQuantity * rej.UnitScrapCost;
                    using var rejCmd = conn.CreateCommand();
                    rejCmd.Transaction = transaction.GetDbTransaction();
                    rejCmd.CommandText = @"
                        INSERT INTO rejection_tracking_logs (
                            id, production_entry_id, work_order_id, defect_category,
                            defect_reason_code, rejected_quantity, disposition,
                            unit_scrap_cost, total_loss_cost, created_at
                        ) VALUES (
                            gen_random_uuid(), @entryId, @woId, @category,
                            @code, @qty, @disp, @uCost, @tCost, CURRENT_TIMESTAMP
                        );
                    ";
                    AddParam(rejCmd, "@entryId", productionEntryId);
                    AddParam(rejCmd, "@woId", workOrderId);
                    AddParam(rejCmd, "@category", rej.DefectCategory);
                    AddParam(rejCmd, "@code", rej.DefectReasonCode);
                    AddParam(rejCmd, "@qty", rej.RejectedQuantity);
                    AddParam(rejCmd, "@disp", rej.Disposition);
                    AddParam(rejCmd, "@uCost", rej.UnitScrapCost);
                    AddParam(rejCmd, "@tCost", totalLoss);
                    await rejCmd.ExecuteNonQueryAsync(ct);
                }

                // 7. Transactional Outbox Event Publishing
                var outboxPayload = JsonSerializer.Serialize(new
                {
                    EventId = Guid.NewGuid(),
                    ProductionEntryId = productionEntryId,
                    EntryNumber = entryNumber,
                    WorkOrderOperationId = request.WorkOrderOperationId,
                    WorkOrderId = workOrderId,
                    GoodQuantity = request.GoodQuantity,
                    ScrappedQuantity = request.ScrappedQuantity,
                    LoggedBy = actingUser,
                    OccurredAt = DateTime.UtcNow
                });

                using var outboxCmd = conn.CreateCommand();
                outboxCmd.Transaction = transaction.GetDbTransaction();
                outboxCmd.CommandText = @"
                    INSERT INTO outbox_messages (outbox_id, event_type, payload_json, created_at, retry_count)
                    VALUES (gen_random_uuid(), 'Production.EntryLoggedEvent', @payload, CURRENT_TIMESTAMP, 0);
                ";
                AddParam(outboxCmd, "@payload", outboxPayload);
                await outboxCmd.ExecuteNonQueryAsync(ct);

                await transaction.CommitAsync(ct);

                _logger.LogInformation("Production Entry {EntryNumber} logged successfully for Operation {OpId} by {User}.", entryNumber, request.WorkOrderOperationId, actingUser);

                return new ProductionEntryResponseDto
                {
                    Id = productionEntryId,
                    EntryNumber = entryNumber,
                    WorkOrderOperationId = request.WorkOrderOperationId,
                    ShiftId = request.ShiftId,
                    OperatorId = request.OperatorId,
                    MachineId = request.MachineId,
                    GoodQuantity = request.GoodQuantity,
                    ScrappedQuantity = request.ScrappedQuantity,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    Status = "SUBMITTED",
                    CreatedAt = DateTime.UtcNow,
                    Consumptions = request.MaterialConsumptions,
                    Rejections = request.Rejections
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Failed to log atomic production entry for operation {OpId}.", request.WorkOrderOperationId);
                throw;
            }
        }

        private static void AddParam(IDbCommand cmd, string name, object value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }
    }
}
