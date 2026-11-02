using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Infrastructure.Procurement
{
    public class ProcurementWorkflowSlaEngine : IProcurementWorkflowSlaEngine
    {
        private readonly ERPDbContext _dbContext;
        private readonly ILogger<ProcurementWorkflowSlaEngine> _logger;

        private const int WORK_DAY_START_HOUR = 9;  // 09:00 IST
        private const int WORK_DAY_END_HOUR = 18;   // 18:00 IST
        private const decimal DAILY_WORK_HOURS = 9.0m;

        public ProcurementWorkflowSlaEngine(ERPDbContext dbContext, ILogger<ProcurementWorkflowSlaEngine> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<ProcurementWorkflowStageLog> StartStageAsync(
            string entityType,
            int entityId,
            string docNumber,
            int stageSeq,
            string stageName,
            decimal targetSlaHours,
            string? dept = null,
            CancellationToken ct = default)
        {
            var log = new ProcurementWorkflowStageLog
            {
                TenantId = "TENANT-DEFAULT",
                EntityType = entityType,
                EntityId = entityId,
                DocumentNumber = docNumber,
                StageSequence = stageSeq,
                StageName = stageName,
                StartedAt = DateTime.UtcNow,
                TargetSlaHours = targetSlaHours,
                DepartmentName = dept ?? GetDefaultDepartmentForStage(stageSeq),
                IsSlaBreached = false
            };

            _dbContext.ProcurementWorkflowStageLogs.Add(log);
            await _dbContext.SaveChangesAsync(ct);
            return log;
        }

        public async Task<ProcurementWorkflowStageLog> CompleteStageAsync(
            string entityType,
            int entityId,
            int stageSeq,
            int? actionedBy = null,
            string? userName = null,
            string? remarks = null,
            DateTime? completedAt = null,
            CancellationToken ct = default)
        {
            var log = await _dbContext.ProcurementWorkflowStageLogs
                .Where(l => l.EntityType == entityType && l.EntityId == entityId && l.StageSequence == stageSeq && l.CompletedAt == null)
                .OrderByDescending(l => l.StartedAt)
                .FirstOrDefaultAsync(ct);

            if (log == null)
            {
                // Create a self-closing retroactive stage entry if start was skipped
                log = new ProcurementWorkflowStageLog
                {
                    TenantId = "TENANT-DEFAULT",
                    EntityType = entityType,
                    EntityId = entityId,
                    DocumentNumber = $"{entityType}-{entityId}",
                    StageSequence = stageSeq,
                    StageName = GetDefaultStageName(stageSeq),
                    StartedAt = (completedAt ?? DateTime.UtcNow).AddHours(-1),
                    TargetSlaHours = 24m,
                    DepartmentName = GetDefaultDepartmentForStage(stageSeq)
                };
                _dbContext.ProcurementWorkflowStageLogs.Add(log);
            }

            var now = completedAt ?? DateTime.UtcNow;
            log.CompletedAt = now;
            log.ActionedByUserId = actionedBy;
            log.ActionedByUserName = userName;
            log.Remarks = remarks;

            decimal elapsedHours = CalculateBusinessHours(log.StartedAt, now);
            log.ElapsedBusinessHours = elapsedHours;
            log.IsSlaBreached = elapsedHours > log.TargetSlaHours;

            await _dbContext.SaveChangesAsync(ct);
            return log;
        }

        public async Task<WorkflowVelocityReportDto> GetVelocityReportAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
        {
            var logs = await _dbContext.ProcurementWorkflowStageLogs
                .Where(l => l.StartedAt >= fromDate && l.StartedAt <= toDate && l.CompletedAt != null)
                .AsNoTracking()
                .ToListAsync(ct);

            var report = new WorkflowVelocityReportDto
            {
                FromDate = fromDate,
                ToDate = toDate,
                TotalDocumentsTracked = logs.Select(l => $"{l.EntityType}-{l.EntityId}").Distinct().Count(),
                TotalSlaBreaches = logs.Count(l => l.IsSlaBreached)
            };

            if (logs.Count > 0)
            {
                report.OverallSlaComplianceRate = Math.Round(
                    ((decimal)(logs.Count - report.TotalSlaBreaches) / logs.Count) * 100m, 2);
            }
            else
            {
                report.OverallSlaComplianceRate = 100m;
            }

            // Group by stages 1 to 7
            for (int stage = 1; stage <= 7; stage++)
            {
                var stageLogs = logs.Where(l => l.StageSequence == stage).ToList();
                int total = stageLogs.Count;
                int breaches = stageLogs.Count(l => l.IsSlaBreached);
                decimal avgElapsed = total > 0 ? Math.Round(stageLogs.Average(l => l.ElapsedBusinessHours ?? 0m), 2) : 0m;
                decimal maxElapsed = total > 0 ? Math.Round(stageLogs.Max(l => l.ElapsedBusinessHours ?? 0m), 2) : 0m;
                decimal compliance = total > 0 ? Math.Round(((decimal)(total - breaches) / total) * 100m, 2) : 100m;

                report.StageMetrics.Add(new WorkflowStageVelocityDto
                {
                    StageSequence = stage,
                    StageName = GetDefaultStageName(stage),
                    TotalInstances = total,
                    TargetSlaHours = GetDefaultTargetSla(stage),
                    AverageElapsedHours = avgElapsed,
                    MaxElapsedHours = maxElapsed,
                    BreachedCount = breaches,
                    SlaCompliancePercentage = compliance
                });
            }

            return report;
        }

        public async Task<List<DepartmentBottleneckScorecardDto>> GetDepartmentBottlenecksAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default)
        {
            var query = _dbContext.ProcurementWorkflowStageLogs
                .Where(l => l.CompletedAt != null);

            if (fromDate.HasValue)
                query = query.Where(l => l.StartedAt >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(l => l.StartedAt <= toDate.Value);

            var logs = await query.AsNoTracking().ToListAsync(ct);

            var deptGroups = logs
                .Where(l => !string.IsNullOrWhiteSpace(l.DepartmentName))
                .GroupBy(l => l.DepartmentName!);

            var list = new List<DepartmentBottleneckScorecardDto>();

            foreach (var group in deptGroups)
            {
                int total = group.Count();
                int breached = group.Count(l => l.IsSlaBreached);
                decimal breachRate = total > 0 ? Math.Round(((decimal)breached / total) * 100m, 2) : 0m;
                decimal avgHours = total > 0 ? Math.Round(group.Average(l => l.ElapsedBusinessHours ?? 0m), 2) : 0m;

                var primaryBottleneck = group
                    .Where(l => l.IsSlaBreached)
                    .GroupBy(l => l.StageName)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault() ?? "Nominal (Within SLA)";

                list.Add(new DepartmentBottleneckScorecardDto
                {
                    DepartmentName = group.Key,
                    TotalTasksAssigned = total,
                    BreachedTasksCount = breached,
                    BreachRatePercentage = breachRate,
                    AverageTurnaroundHours = avgHours,
                    PrimaryBottleneckStage = primaryBottleneck
                });
            }

            return list.OrderByDescending(d => d.BreachRatePercentage).ToList();
        }

        public static decimal CalculateBusinessHours(DateTime startUtc, DateTime endUtc)
        {
            if (endUtc <= startUtc) return 0m;

            decimal totalHours = 0m;
            DateTime current = startUtc;

            while (current.Date <= endUtc.Date)
            {
                // Skip Saturday & Sunday
                if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                {
                    DateTime dayStart = current.Date.AddHours(WORK_DAY_START_HOUR);
                    DateTime dayEnd = current.Date.AddHours(WORK_DAY_END_HOUR);

                    DateTime intervalStart = current > dayStart ? current : dayStart;
                    DateTime intervalEnd = endUtc < dayEnd ? endUtc : dayEnd;

                    if (intervalEnd > intervalStart)
                    {
                        totalHours += (decimal)(intervalEnd - intervalStart).TotalHours;
                    }
                }

                current = current.Date.AddDays(1);
            }

            return Math.Round(totalHours, 2);
        }

        private static string GetDefaultStageName(int stage) => stage switch
        {
            1 => "1. Requisition -> Approval",
            2 => "2. PR Approval -> RFQ Issuance",
            3 => "3. RFQ Bidding -> Matrix Award",
            4 => "4. Matrix Award -> PO Authorization",
            5 => "5. PO Transmission -> Gate Inward",
            6 => "6. Gate Inward -> QC Clearance",
            7 => "7. QC Clearance -> 3-Way Match & Bill Posting",
            _ => $"Stage {stage}"
        };

        private static string GetDefaultDepartmentForStage(int stage) => stage switch
        {
            1 => "User Department",
            2 => "Procurement / Sourcing",
            3 => "Commercial / Sourcing",
            4 => "Executive Management / DOA",
            5 => "Vendor Logistics",
            6 => "Quality Assurance",
            7 => "Accounts Payable",
            _ => "Procurement"
        };

        private static decimal GetDefaultTargetSla(int stage) => stage switch
        {
            1 => 24m, // 24 hours
            2 => 48m, // 48 hours
            3 => 72m, // 72 hours
            4 => 24m, // 24 hours
            5 => 120m, // 5 days lead time
            6 => 24m, // 24 hours QC
            7 => 24m, // 24 hours 3-Way match
            _ => 24m
        };
    }
}
