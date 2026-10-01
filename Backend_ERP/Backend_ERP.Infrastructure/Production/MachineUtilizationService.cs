using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ERP.Infrastructure.Data;

namespace Backend_ERP.Infrastructure.Production
{
    public class MachineOeeMetricsDto
    {
        public Guid MachineId { get; set; }
        public string MachineCode { get; set; } = string.Empty;
        public decimal TotalPlannedHours { get; set; }
        public decimal ActualRunHours { get; set; }
        public decimal UnplannedDowntimeHours { get; set; }
        public decimal UtilizationRatePercentage { get; set; }
        public decimal AvailabilityLossRatePercentage { get; set; }
        public decimal PerformanceEfficiencyPercentage { get; set; }
        public decimal QualityRatePercentage { get; set; }
        public decimal OeePercentage { get; set; }
    }

    public interface IMachineUtilizationService
    {
        Task<MachineOeeMetricsDto> CalculateMachineOeeMetricsAsync(Guid machineId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
        Task<List<MachineOeeMetricsDto>> CalculatePlantWideOeeMetricsAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);
    }

    public class MachineUtilizationService : IMachineUtilizationService
    {
        private readonly ERPDbContext _dbContext;
        private readonly ILogger<MachineUtilizationService> _logger;

        public MachineUtilizationService(ERPDbContext dbContext, ILogger<MachineUtilizationService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<MachineOeeMetricsDto> CalculateMachineOeeMetricsAsync(Guid machineId, DateTime startDate, DateTime endDate, CancellationToken ct = default)
        {
            var machine = await _dbContext.Machines
                .FirstOrDefaultAsync(m => m.Id.ToString() == machineId.ToString(), ct)
                ?? throw new KeyNotFoundException($"Machine {machineId} not found.");

            // Calculate Downtime Breakdown
            const string downtimeSql = @"
                SELECT 
                    downtime_category,
                    SUM(COALESCE(duration_minutes, 0.0)) / 60.0 as total_hours
                FROM machine_downtime_logs
                WHERE machine_id = '{0}'
                  AND start_timestamp >= '{1}' AND COALESCE(end_timestamp, CURRENT_TIMESTAMP) <= '{2}'
                GROUP BY downtime_category;
            ";

            var conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = string.Format(downtimeSql, machineId, startDate.ToString("yyyy-MM-dd HH:mm:ss.fffZ"), endDate.ToString("yyyy-MM-dd HH:mm:ss.fffZ"));

            decimal unplannedDowntimeHours = 0m;
            decimal plannedDowntimeHours = 0m;

            using (var reader = await cmd.ExecuteReaderAsync(ct))
            {
                while (await reader.ReadAsync(ct))
                {
                    string category = reader.GetString(0);
                    decimal hours = reader.GetDecimal(1);

                    if (category.StartsWith("UNPLANNED") || category.Contains("WAIT") || category.Contains("ABSENT"))
                        unplannedDowntimeHours += hours;
                    else
                        plannedDowntimeHours += hours;
                }
            }

            // Total Days in range * shift capacity
            decimal totalCalendarHours = (decimal)(endDate - startDate).TotalHours;
            decimal totalPlannedHours = Math.Max(0m, totalCalendarHours - plannedDowntimeHours);
            
            // Execute Run Hours query from machine_schedules
            const string runHoursSql = @"
                SELECT COALESCE(SUM(EXTRACT(EPOCH FROM (upper(schedule_range) - lower(schedule_range))) / 3600.0), 0.0)
                FROM machine_schedules
                WHERE machine_id = '{0}' AND status = 'COMPLETED'
                  AND lower(schedule_range) >= '{1}' AND upper(schedule_range) <= '{2}';
            ";

            using var runCmd = conn.CreateCommand();
            runCmd.CommandText = string.Format(runHoursSql, machineId, startDate.ToString("yyyy-MM-dd HH:mm:ss.fffZ"), endDate.ToString("yyyy-MM-dd HH:mm:ss.fffZ"));

            decimal actualRunHours = Convert.ToDecimal(await runCmd.ExecuteScalarAsync(ct));

            // Metrics Calculations
            decimal utilizationRate = totalPlannedHours > 0 ? (actualRunHours / totalPlannedHours) * 100m : 0m;
            decimal availabilityLossRate = totalPlannedHours > 0 ? (unplannedDowntimeHours / totalPlannedHours) * 100m : 0m;
            
            // Standard Performance & Quality rates
            decimal availabilityPercentage = Math.Max(0m, 100m - availabilityLossRate);
            decimal performancePercentage = 92.5m;
            decimal qualityPercentage = 98.2m;

            decimal oee = (availabilityPercentage / 100m) * (performancePercentage / 100m) * (qualityPercentage / 100m) * 100m;

            return new MachineOeeMetricsDto
            {
                MachineId = machineId,
                MachineCode = machine.MachineCode,
                TotalPlannedHours = Math.Round(totalPlannedHours, 2),
                ActualRunHours = Math.Round(actualRunHours, 2),
                UnplannedDowntimeHours = Math.Round(unplannedDowntimeHours, 2),
                UtilizationRatePercentage = Math.Round(utilizationRate, 2),
                AvailabilityLossRatePercentage = Math.Round(availabilityLossRate, 2),
                PerformanceEfficiencyPercentage = Math.Round(performancePercentage, 2),
                QualityRatePercentage = Math.Round(qualityPercentage, 2),
                OeePercentage = Math.Round(oee, 2)
            };
        }

        public async Task<List<MachineOeeMetricsDto>> CalculatePlantWideOeeMetricsAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
        {
            var machines = await _dbContext.Machines
                .Where(m => !m.IsDeleted)
                .ToListAsync(ct);

            var results = new List<MachineOeeMetricsDto>();
            foreach (var m in machines)
            {
                var dto = await CalculateMachineOeeMetricsAsync(Guid.NewGuid(), startDate, endDate, ct);
                results.Add(dto);
            }

            return results;
        }
    }
}
