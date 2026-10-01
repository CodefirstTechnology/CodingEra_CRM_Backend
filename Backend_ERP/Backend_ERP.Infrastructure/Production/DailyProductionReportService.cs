using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Production.Dtos;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Backend_ERP.Infrastructure.Production
{
    public interface IDailyProductionReportService
    {
        Task<DailyProductionReportDto> GenerateDailyProductionReportAsync(GenerateDprRequestDto request, CancellationToken ct = default);
        Task<DailyProductionReportDto> FinalizeReportAsync(FinalizeDprRequestDto request, CancellationToken ct = default);
        Task<List<DailyProductionReportDto>> GetReportsByDateAsync(DateTime reportDate, CancellationToken ct = default);
    }

    public class DailyProductionReportService : IDailyProductionReportService
    {
        private readonly ERPDbContext _dbContext;
        private readonly ILogger<DailyProductionReportService> _logger;

        public DailyProductionReportService(ERPDbContext dbContext, ILogger<DailyProductionReportService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<DailyProductionReportDto> GenerateDailyProductionReportAsync(GenerateDprRequestDto request, CancellationToken ct = default)
        {
            var dateOnly = request.ReportDate.Date;

            // Check if report already exists and is finalized
            var existingReport = await _dbContext.DailyProductionReports
                .FirstOrDefaultAsync(r => r.ReportDate == dateOnly && r.ShiftId == request.ShiftId && r.WorkCenterId == request.WorkCenterId, ct);

            if (existingReport != null && existingReport.IsFinalized)
            {
                throw new InvalidOperationException($"Daily Production Report for shift {request.ShiftId} on {dateOnly:yyyy-MM-dd} is finalized and locked against updates.");
            }

            var conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

            // Fetch production entries for this shift/date/workcenter
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT pe.id, pe.good_quantity, pe.scrapped_quantity, pe.start_time, pe.end_time, woo.work_center_id
                FROM shop_floor_production_entries pe
                INNER JOIN work_order_operations woo ON woo.id = pe.work_order_operation_id
                WHERE DATE(pe.start_time) = @rDate
                  AND pe.shift_id = @shiftId
                  AND woo.work_center_id = @wcId
                  AND pe.status IN ('SUBMITTED', 'APPROVED');
            ";

            var p1 = cmd.CreateParameter(); p1.ParameterName = "@rDate"; p1.Value = dateOnly; cmd.Parameters.Add(p1);
            var p2 = cmd.CreateParameter(); p2.ParameterName = "@shiftId"; p2.Value = request.ShiftId; cmd.Parameters.Add(p2);
            var p3 = cmd.CreateParameter(); p3.ParameterName = "@wcId"; p3.Value = request.WorkCenterId; cmd.Parameters.Add(p3);

            var entryIds = new List<Guid>();
            decimal totalGood = 0;
            decimal totalScrap = 0;

            using (var reader = await cmd.ExecuteReaderAsync(ct))
            {
                while (await reader.ReadAsync(ct))
                {
                    entryIds.Add(reader.GetGuid(0));
                    totalGood += reader.GetDecimal(1);
                    totalScrap += reader.GetDecimal(2);
                }
            }

            // Calculate Downtime from machine_downtime_logs
            using var dtCmd = conn.CreateCommand();
            dtCmd.CommandText = @"
                SELECT COALESCE(SUM(EXTRACT(EPOCH FROM (COALESCE(end_timestamp, CURRENT_TIMESTAMP) - start_timestamp)) / 60.0), 0)
                FROM machine_downtime_logs mdl
                LEFT JOIN work_order_operations woo ON woo.id = mdl.work_order_operation_id
                WHERE DATE(mdl.start_timestamp) = @rDate
                  AND (woo.work_center_id = @wcId OR mdl.work_order_operation_id IS NULL);
            ";
            var dt1 = dtCmd.CreateParameter(); dt1.ParameterName = "@rDate"; dt1.Value = dateOnly; dtCmd.Parameters.Add(dt1);
            var dt2 = dtCmd.CreateParameter(); dt2.ParameterName = "@wcId"; dt2.Value = request.WorkCenterId; dtCmd.Parameters.Add(dt2);

            decimal totalDowntimeMinutes = 0;
            var dtResult = await dtCmd.ExecuteScalarAsync(ct);
            if (dtResult != null && dtResult != DBNull.Value)
            {
                totalDowntimeMinutes = Math.Round(Convert.ToDecimal(dtResult), 2);
            }

            // Total Planned Qty baseline
            decimal plannedQty = Math.Max(totalGood + totalScrap, 100.0m);

            // OEE Component Calculations
            // Standard shift = 480 minutes (8 hrs)
            const decimal plannedOperatingMinutes = 480.0m;
            decimal actualOperatingMinutes = Math.Max(0, plannedOperatingMinutes - totalDowntimeMinutes);
            decimal availability = Math.Clamp((actualOperatingMinutes / plannedOperatingMinutes) * 100.0m, 0m, 100m);

            decimal totalProduced = totalGood + totalScrap;
            decimal performance = Math.Clamp((totalProduced / plannedQty) * 100.0m, 0m, 100m);

            decimal quality = totalProduced > 0 ? Math.Clamp((totalGood / totalProduced) * 100.0m, 0m, 100m) : 100.0m;

            decimal oee = Math.Round((availability * performance * quality) / 10000.0m, 2);
            availability = Math.Round(availability, 2);
            performance = Math.Round(performance, 2);
            quality = Math.Round(quality, 2);

            Guid reportId = existingReport?.Id ?? Guid.NewGuid();

            if (existingReport == null)
            {
                var newReport = new Backend_ERP.Domain.Entities.DailyProductionReportEntity
                {
                    Id = reportId,
                    ReportDate = dateOnly,
                    ShiftId = request.ShiftId,
                    WorkCenterId = request.WorkCenterId,
                    TotalPlannedQty = plannedQty,
                    TotalGoodQty = totalGood,
                    TotalScrapQty = totalScrap,
                    TotalDowntimeMinutes = totalDowntimeMinutes,
                    OeePercentage = oee,
                    AvailabilityPercentage = availability,
                    PerformancePercentage = performance,
                    QualityPercentage = quality,
                    IsFinalized = false,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.DailyProductionReports.Add(newReport);
            }
            else
            {
                existingReport.TotalPlannedQty = plannedQty;
                existingReport.TotalGoodQty = totalGood;
                existingReport.TotalScrapQty = totalScrap;
                existingReport.TotalDowntimeMinutes = totalDowntimeMinutes;
                existingReport.OeePercentage = oee;
                existingReport.AvailabilityPercentage = availability;
                existingReport.PerformancePercentage = performance;
                existingReport.QualityPercentage = quality;
                existingReport.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(ct);

            // Update junction entries
            using var clearJunctionCmd = conn.CreateCommand();
            clearJunctionCmd.CommandText = "DELETE FROM daily_production_report_entries WHERE daily_production_report_id = @rId;";
            var c1 = clearJunctionCmd.CreateParameter(); c1.ParameterName = "@rId"; c1.Value = reportId; clearJunctionCmd.Parameters.Add(c1);
            await clearJunctionCmd.ExecuteNonQueryAsync(ct);

            foreach (var eId in entryIds)
            {
                using var insJunctionCmd = conn.CreateCommand();
                insJunctionCmd.CommandText = @"
                    INSERT INTO daily_production_report_entries (id, daily_production_report_id, production_entry_id, linked_at)
                    VALUES (gen_random_uuid(), @rId, @eId, CURRENT_TIMESTAMP)
                    ON CONFLICT DO NOTHING;
                ";
                var j1 = insJunctionCmd.CreateParameter(); j1.ParameterName = "@rId"; j1.Value = reportId; insJunctionCmd.Parameters.Add(j1);
                var j2 = insJunctionCmd.CreateParameter(); j2.ParameterName = "@eId"; j2.Value = eId; insJunctionCmd.Parameters.Add(j2);
                await insJunctionCmd.ExecuteNonQueryAsync(ct);
            }

            return new DailyProductionReportDto
            {
                Id = reportId,
                ReportDate = dateOnly,
                ShiftId = request.ShiftId,
                WorkCenterId = request.WorkCenterId,
                TotalPlannedQty = plannedQty,
                TotalGoodQty = totalGood,
                TotalScrapQty = totalScrap,
                TotalDowntimeMinutes = totalDowntimeMinutes,
                OeePercentage = oee,
                AvailabilityPercentage = availability,
                PerformancePercentage = performance,
                QualityPercentage = quality,
                IsFinalized = false,
                LinkedProductionEntryIds = entryIds
            };
        }

        public async Task<DailyProductionReportDto> FinalizeReportAsync(FinalizeDprRequestDto request, CancellationToken ct = default)
        {
            var report = await _dbContext.DailyProductionReports
                .FirstOrDefaultAsync(r => r.Id == request.ReportId, ct)
                ?? throw new KeyNotFoundException($"Daily Production Report {request.ReportId} not found.");

            if (report.IsFinalized)
            {
                throw new InvalidOperationException($"Daily Production Report {request.ReportId} is already finalized by {report.FinalizedBy} at {report.FinalizedAt}.");
            }

            report.IsFinalized = true;
            report.FinalizedBy = request.FinalizedBy;
            report.FinalizedAt = DateTime.UtcNow;
            report.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Daily Production Report {ReportId} successfully finalized by supervisor {User}.", request.ReportId, request.FinalizedBy);

            var linkedEntries = await _dbContext.DailyProductionReportEntries
                .Where(e => e.DailyProductionReportId == request.ReportId)
                .Select(e => e.ProductionEntryId)
                .ToListAsync(ct);

            return new DailyProductionReportDto
            {
                Id = report.Id,
                ReportDate = report.ReportDate,
                ShiftId = report.ShiftId,
                WorkCenterId = report.WorkCenterId,
                TotalPlannedQty = report.TotalPlannedQty,
                TotalGoodQty = report.TotalGoodQty,
                TotalScrapQty = report.TotalScrapQty,
                TotalDowntimeMinutes = report.TotalDowntimeMinutes,
                OeePercentage = report.OeePercentage,
                AvailabilityPercentage = report.AvailabilityPercentage,
                PerformancePercentage = report.PerformancePercentage,
                QualityPercentage = report.QualityPercentage,
                IsFinalized = report.IsFinalized,
                FinalizedBy = report.FinalizedBy,
                FinalizedAt = report.FinalizedAt,
                LinkedProductionEntryIds = linkedEntries
            };
        }

        public async Task<List<DailyProductionReportDto>> GetReportsByDateAsync(DateTime reportDate, CancellationToken ct = default)
        {
            var dateOnly = reportDate.Date;
            var reports = await _dbContext.DailyProductionReports
                .Where(r => r.ReportDate == dateOnly)
                .ToListAsync(ct);

            var dtos = new List<DailyProductionReportDto>();
            foreach (var r in reports)
            {
                var entryIds = await _dbContext.DailyProductionReportEntries
                    .Where(e => e.DailyProductionReportId == r.Id)
                    .Select(e => e.ProductionEntryId)
                    .ToListAsync(ct);

                dtos.Add(new DailyProductionReportDto
                {
                    Id = r.Id,
                    ReportDate = r.ReportDate,
                    ShiftId = r.ShiftId,
                    WorkCenterId = r.WorkCenterId,
                    TotalPlannedQty = r.TotalPlannedQty,
                    TotalGoodQty = r.TotalGoodQty,
                    TotalScrapQty = r.TotalScrapQty,
                    TotalDowntimeMinutes = r.TotalDowntimeMinutes,
                    OeePercentage = r.OeePercentage,
                    AvailabilityPercentage = r.AvailabilityPercentage,
                    PerformancePercentage = r.PerformancePercentage,
                    QualityPercentage = r.QualityPercentage,
                    IsFinalized = r.IsFinalized,
                    FinalizedBy = r.FinalizedBy,
                    FinalizedAt = r.FinalizedAt,
                    LinkedProductionEntryIds = entryIds
                });
            }

            return dtos;
        }
    }
}
