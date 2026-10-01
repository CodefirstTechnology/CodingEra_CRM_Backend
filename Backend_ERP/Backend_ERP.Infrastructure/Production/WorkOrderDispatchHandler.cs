using System;
using System.Data;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using ERP.Infrastructure.Data;

namespace Backend_ERP.Infrastructure.Production
{
    public class DispatchResultDto
    {
        public bool IsSuccess { get; set; }
        public string WorkOrderId { get; set; } = string.Empty;
        public string WorkOrderNumber { get; set; } = string.Empty;
        public int StagedLotCount { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public interface IWorkOrderDispatchHandler
    {
        Task<DispatchResultDto> DispatchWorkOrderAtomicAsync(Guid workOrderId, Guid stagingBinId, string actingUser, CancellationToken ct = default);
    }

    public class WorkOrderDispatchHandler : IWorkOrderDispatchHandler
    {
        private readonly ERPDbContext _dbContext;
        private readonly ILogger<WorkOrderDispatchHandler> _logger;

        public WorkOrderDispatchHandler(ERPDbContext dbContext, ILogger<WorkOrderDispatchHandler> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<DispatchResultDto> DispatchWorkOrderAtomicAsync(
            Guid workOrderId, Guid stagingBinId, string actingUser, CancellationToken ct = default)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

            try
            {
                var workOrder = await _dbContext.WorkOrders
                    .FirstOrDefaultAsync(w => w.Id.ToString() == workOrderId.ToString(), ct)
                    ?? throw new KeyNotFoundException($"Work Order {workOrderId} not found.");

                if (workOrder.Status != ERP.Domain.Production.WorkOrderStatus.Draft && workOrder.Status != ERP.Domain.Production.WorkOrderStatus.Released)
                {
                    throw new InvalidOperationException($"Work Order {workOrder.WorkOrderNumber} cannot be dispatched from current status '{workOrder.Status}'.");
                }

                // Convert Hard Reserved Allocations to Work Order Staging Reservations
                const string stageMaterialSql = @"
                    INSERT INTO work_order_material_reservations (
                        id, work_order_id, raw_material_item_id, warehouse_id, 
                        staging_bin_id, batch_id, required_quantity, staged_quantity, status, created_at
                    )
                    SELECT 
                        gen_random_uuid(), {0}, ma.raw_material_item_id, ma.warehouse_id, 
                        {1}, ma.batch_id, ma.hard_reserved_qty, ma.hard_reserved_qty, 'STAGED', CURRENT_TIMESTAMP
                    FROM production_plan_material_allocations ma
                    INNER JOIN production_plan_items ppi ON ppi.id = ma.production_plan_item_id
                    WHERE ppi.production_plan_id = {2} AND ma.allocation_status = 'HARD_RESERVED';
                ";

                var conn = _dbContext.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

                using var stageCmd = conn.CreateCommand();
                stageCmd.Transaction = transaction.GetDbTransaction();
                stageCmd.CommandText = stageMaterialSql
                    .Replace("{0}", $"'{workOrderId}'")
                    .Replace("{1}", $"'{stagingBinId}'")
                    .Replace("{2}", $"'{workOrder.PlanId}'");

                int stagedCount = await stageCmd.ExecuteNonQueryAsync(ct);

                // Update Work Order status to Released
                workOrder.Status = ERP.Domain.Production.WorkOrderStatus.Released;
                workOrder.StartDate = DateOnly.FromDateTime(DateTime.UtcNow);
                workOrder.UpdatedBy = actingUser;
                workOrder.UpdatedAt = DateTime.UtcNow;

                // Publish WorkOrderReleasedEvent via Transactional Outbox
                var outboxPayload = JsonSerializer.Serialize(new
                {
                    EventId = Guid.NewGuid(),
                    WorkOrderId = workOrder.Id,
                    WorkOrderNumber = workOrder.WorkOrderNumber,
                    StagedBinId = stagingBinId,
                    DispatchedBy = actingUser,
                    OccurredAt = DateTime.UtcNow
                });

                using var outboxCmd = conn.CreateCommand();
                outboxCmd.Transaction = transaction.GetDbTransaction();
                outboxCmd.CommandText = @"
                    INSERT INTO outbox_messages (outbox_id, event_type, payload_json, created_at, retry_count)
                    VALUES (gen_random_uuid(), 'Production.WorkOrderReleased', '{0}', CURRENT_TIMESTAMP, 0);
                ".Replace("{0}", outboxPayload.Replace("'", "''"));

                await outboxCmd.ExecuteNonQueryAsync(ct);
                await _dbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _logger.LogInformation("Work Order {WorkOrderNumber} successfully dispatched by {User}.", workOrder.WorkOrderNumber, actingUser);

                return new DispatchResultDto
                {
                    IsSuccess = true,
                    WorkOrderId = workOrder.Id.ToString(),
                    WorkOrderNumber = workOrder.WorkOrderNumber,
                    StagedLotCount = stagedCount,
                    Message = $"Work Order {workOrder.WorkOrderNumber} dispatched and staged at bin {stagingBinId}."
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Error dispatching Work Order {WorkOrderId}", workOrderId);
                throw;
            }
        }
    }
}
