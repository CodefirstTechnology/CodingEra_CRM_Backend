using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using ERP.Infrastructure.Data;

namespace Backend_ERP.Infrastructure.Production
{
    public class MachineSlotBookingResult
    {
        public bool IsSuccess { get; set; }
        public Guid MachineId { get; set; }
        public Guid WorkOrderOperationId { get; set; }
        public DateTime ScheduledStart { get; set; }
        public DateTime ScheduledEnd { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class WorkOrderOperationRowDto
    {
        public Guid Id { get; set; }
        public int OperationSequence { get; set; }
        public string OperationName { get; set; } = string.Empty;
        public Guid WorkCenterId { get; set; }
        public Guid? AssignedMachineId { get; set; }
        public decimal SetupTimeMinutes { get; set; }
        public decimal RunTimeSecondsPerUnit { get; set; }
    }

    public interface IFiniteCapacitySchedulingEngine
    {
        Task<List<MachineSlotBookingResult>> ScheduleWorkOrderForwardAsync(Guid workOrderId, Guid productionScheduleId, DateTime searchStartDate, CancellationToken ct = default);
    }

    public class FiniteCapacitySchedulingEngine : IFiniteCapacitySchedulingEngine
    {
        private readonly ERPDbContext _dbContext;
        private readonly ILogger<FiniteCapacitySchedulingEngine> _logger;

        public FiniteCapacitySchedulingEngine(ERPDbContext dbContext, ILogger<FiniteCapacitySchedulingEngine> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<MachineSlotBookingResult>> ScheduleWorkOrderForwardAsync(
            Guid workOrderId, Guid productionScheduleId, DateTime searchStartDate, CancellationToken ct = default)
        {
            var results = new List<MachineSlotBookingResult>();

            var workOrder = await _dbContext.WorkOrders
                .FirstOrDefaultAsync(w => w.Id.ToString() == workOrderId.ToString(), ct)
                ?? throw new KeyNotFoundException($"Work Order {workOrderId} not found.");

            // Raw SQL fetch for work_order_operations
            const string getOpsSql = @"
                SELECT id AS Id, operation_sequence AS OperationSequence, operation_name AS OperationName,
                       work_center_id AS WorkCenterId, assigned_machine_id AS AssignedMachineId,
                       setup_time_minutes AS SetupTimeMinutes, run_time_seconds_per_unit AS RunTimeSecondsPerUnit
                FROM work_order_operations
                WHERE work_order_id = '{0}'
                ORDER BY operation_sequence ASC;
            ";

            var conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

            var operations = new List<WorkOrderOperationRowDto>();
            using (var opCmd = conn.CreateCommand())
            {
                opCmd.CommandText = string.Format(getOpsSql, workOrderId);
                using var reader = await opCmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    operations.Add(new WorkOrderOperationRowDto
                    {
                        Id = reader.GetGuid(0),
                        OperationSequence = reader.GetInt32(1),
                        OperationName = reader.GetString(2),
                        WorkCenterId = reader.GetGuid(3),
                        AssignedMachineId = reader.IsDBNull(4) ? null : reader.GetGuid(4),
                        SetupTimeMinutes = reader.GetDecimal(5),
                        RunTimeSecondsPerUnit = reader.GetDecimal(6)
                    });
                }
            }

            DateTime currentEarliestStart = searchStartDate;

            foreach (var op in operations)
            {
                decimal runMinutes = (op.RunTimeSecondsPerUnit * workOrder.PlannedQuantity) / 60.0m;
                decimal totalDurationMinutes = op.SetupTimeMinutes + runMinutes;

                var candidateMachineId = op.AssignedMachineId ?? await FindAvailableMachineForWorkCenterAsync(op.WorkCenterId, ct);
                var bookedSlot = await FindNextAvailableMachineSlotAsync(candidateMachineId, currentEarliestStart, totalDurationMinutes, ct);

                await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
                try
                {
                    const string insertSlotSql = @"
                        INSERT INTO machine_schedules (
                            id, production_schedule_id, machine_id, work_order_operation_id, 
                            booking_type, schedule_range, status, created_at
                        )
                        VALUES (
                            gen_random_uuid(), '{0}', '{1}', '{2}', 
                            'PRODUCTION', tsrange('{3}'::timestamptz, '{4}'::timestamptz, '[)'), 'BOOKED', CURRENT_TIMESTAMP
                        );
                    ";

                    using var cmd = conn.CreateCommand();
                    cmd.Transaction = transaction.GetDbTransaction();
                    cmd.CommandText = string.Format(insertSlotSql, 
                        productionScheduleId, candidateMachineId, op.Id, 
                        bookedSlot.Start.ToString("yyyy-MM-dd HH:mm:ss.fffZ"), 
                        bookedSlot.End.ToString("yyyy-MM-dd HH:mm:ss.fffZ"));

                    await cmd.ExecuteNonQueryAsync(ct);

                    // Update Work Order Operation Status
                    using var updateOpCmd = conn.CreateCommand();
                    updateOpCmd.Transaction = transaction.GetDbTransaction();
                    updateOpCmd.CommandText = @"
                        UPDATE work_order_operations
                        SET assigned_machine_id = '{0}', status = 'SCHEDULED'
                        WHERE id = '{1}';
                    ".Replace("{0}", candidateMachineId.ToString()).Replace("{1}", op.Id.ToString());
                    await updateOpCmd.ExecuteNonQueryAsync(ct);

                    await transaction.CommitAsync(ct);

                    results.Add(new MachineSlotBookingResult
                    {
                        IsSuccess = true,
                        MachineId = candidateMachineId,
                        WorkOrderOperationId = op.Id,
                        ScheduledStart = bookedSlot.Start,
                        ScheduledEnd = bookedSlot.End
                    });

                    currentEarliestStart = bookedSlot.End;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(ct);
                    _logger.LogError(ex, "Exclusion constraint clash when booking slot for machine {MachineId}", candidateMachineId);
                    results.Add(new MachineSlotBookingResult
                    {
                        IsSuccess = false,
                        MachineId = candidateMachineId,
                        WorkOrderOperationId = op.Id,
                        ErrorMessage = $"Slot clash or exclusion constraint violation: {ex.Message}"
                    });
                    break;
                }
            }

            if (results.All(r => r.IsSuccess))
            {
                workOrder.Status = ERP.Domain.Production.WorkOrderStatus.Released;
                workOrder.StartDate = DateOnly.FromDateTime(results.First().ScheduledStart);
                workOrder.DueDate = DateOnly.FromDateTime(results.Last().ScheduledEnd);
                await _dbContext.SaveChangesAsync(ct);
            }

            return results;
        }

        private async Task<Guid> FindAvailableMachineForWorkCenterAsync(Guid workCenterId, CancellationToken ct)
        {
            var machine = await _dbContext.Machines
                .Where(m => !m.IsDeleted && m.Status != ERP.Domain.Production.MachineStatus.Maintenance)
                .Select(m => m.Id)
                .FirstOrDefaultAsync(ct);

            if (machine == 0)
            {
                return Guid.NewGuid();
            }

            return Guid.NewGuid();
        }

        private async Task<(DateTime Start, DateTime End)> FindNextAvailableMachineSlotAsync(Guid machineId, DateTime searchFrom, decimal durationMinutes, CancellationToken ct)
        {
            const string existingSlotsSql = @"
                SELECT lower(schedule_range) as slot_start, upper(schedule_range) as slot_end
                FROM machine_schedules
                WHERE machine_id = '{0}' AND status IN ('BOOKED', 'IN_PROGRESS')
                  AND upper(schedule_range) >= '{1}'
                ORDER BY lower(schedule_range) ASC;
            ";

            var conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = string.Format(existingSlotsSql, machineId, searchFrom.ToString("yyyy-MM-dd HH:mm:ss.fffZ"));

            var bookedRanges = new List<(DateTime Start, DateTime End)>();
            using (var reader = await cmd.ExecuteReaderAsync(ct))
            {
                while (await reader.ReadAsync(ct))
                {
                    bookedRanges.Add((reader.GetDateTime(0), reader.GetDateTime(1)));
                }
            }

            DateTime candidateStart = searchFrom;
            DateTime candidateEnd = candidateStart.AddMinutes((double)durationMinutes);

            foreach (var slot in bookedRanges)
            {
                if (candidateEnd <= slot.Start)
                {
                    return (candidateStart, candidateEnd);
                }

                if (candidateStart < slot.End)
                {
                    candidateStart = slot.End;
                    candidateEnd = candidateStart.AddMinutes((double)durationMinutes);
                }
            }

            return (candidateStart, candidateEnd);
        }
    }
}
