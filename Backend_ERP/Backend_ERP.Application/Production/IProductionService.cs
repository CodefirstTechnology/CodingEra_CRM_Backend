using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Production.Dtos;

namespace ERP.Application.Production
{
    public interface IProductionService
    {
        // BOM
        Task<List<BomListItemDto>> GetBomsAsync(string? search, string? status, CancellationToken cancellationToken = default);
        Task<BomDto?> GetBomByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<BomDto> CreateBomAsync(BomCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default);
        Task<BomDto?> UpdateBomAsync(int id, BomUpdateRequestDto request, string actingUser, CancellationToken cancellationToken = default);
        Task<bool> DeleteBomAsync(int id, string actingUser, CancellationToken cancellationToken = default);
        Task<BomDto?> DuplicateBomAsync(int id, string actingUser, CancellationToken cancellationToken = default);
        Task<BomDto?> ApproveBomAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default);
        Task<BomDto?> ActivateBomAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default);
        Task<BomDto?> ArchiveBomAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default);
        Task<BomDashboardDto> GetBomDashboardAsync(CancellationToken cancellationToken = default);

        // Planning
        Task<List<PlanListItemDto>> GetPlansAsync(string? search, string? status, CancellationToken cancellationToken = default);
        Task<PlanDto?> GetPlanByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<PlanDto> CreatePlanAsync(PlanCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default);
        Task<PlanDto?> UpdatePlanAsync(int id, PlanUpdateRequestDto request, string actingUser, CancellationToken cancellationToken = default);
        Task<bool> DeletePlanAsync(int id, string actingUser, CancellationToken cancellationToken = default);
        Task<PlanDto?> ApprovePlanAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default);
        Task<PlanDto?> ReleasePlanAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default);
        Task<List<WorkOrderDto>> GenerateWorkOrdersAsync(int planId, string actingUser, CancellationToken cancellationToken = default);
        Task<PlanDashboardDto> GetPlanDashboardAsync(CancellationToken cancellationToken = default);

        // Work Orders
        Task<List<WorkOrderListItemDto>> GetWorkOrdersAsync(string? search, string? status, CancellationToken cancellationToken = default);
        Task<WorkOrderDto?> GetWorkOrderByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<WorkOrderDto> CreateWorkOrderAsync(WorkOrderCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default);
        Task<WorkOrderDto?> UpdateWorkOrderAsync(int id, WorkOrderUpdateRequestDto request, string actingUser, CancellationToken cancellationToken = default);
        Task<bool> DeleteWorkOrderAsync(int id, string actingUser, CancellationToken cancellationToken = default);
        Task<WorkOrderDto?> StartWorkOrderAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default);
        Task<WorkOrderDto?> ReleaseWorkOrderAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default);
        Task<WorkOrderDto?> PauseWorkOrderAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default);
        Task<WorkOrderDto?> ResumeWorkOrderAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default);
        Task<WorkOrderDto?> CompleteWorkOrderAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default);
        Task<WorkOrderDto?> CloseWorkOrderAsync(int id, StatusActionRequestDto? payload, string actingUser, CancellationToken cancellationToken = default);
        Task<WorkOrderDashboardDto> GetWorkOrderDashboardAsync(CancellationToken cancellationToken = default);

        // Scheduling
        Task<List<ScheduleListItemDto>> GetSchedulesAsync(string? search, string? status, string? shift, int? workOrderId, int? machineId, string? dateFrom, string? dateTo, CancellationToken cancellationToken = default);
        Task<ScheduleDto?> GetScheduleByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<ScheduleDashboardDto> GetScheduleDashboardAsync(CancellationToken cancellationToken = default);

        // Machines
        Task<List<MachineListItemDto>> GetMachinesAsync(string? search, string? status, CancellationToken cancellationToken = default);
        Task<MachineDto?> GetMachineByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<MachineDashboardDto> GetMachineDashboardAsync(CancellationToken cancellationToken = default);
    }
}
