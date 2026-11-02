using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;

namespace ERP.Application.Procurement
{
    public interface IProcurementWorkflowSlaEngine
    {
        Task<ProcurementWorkflowStageLog> StartStageAsync(string entityType, int entityId, string docNumber, int stageSeq, string stageName, decimal targetSlaHours, string? dept = null, CancellationToken ct = default);
        Task<ProcurementWorkflowStageLog> CompleteStageAsync(string entityType, int entityId, int stageSeq, int? actionedBy = null, string? userName = null, string? remarks = null, DateTime? completedAt = null, CancellationToken ct = default);
        Task<WorkflowVelocityReportDto> GetVelocityReportAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);
        Task<List<DepartmentBottleneckScorecardDto>> GetDepartmentBottlenecksAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default);
    }
}
