using System;
using System.Collections.Generic;
using ERP.Domain.Production;
using ERP.Domain.Procurement;

namespace ERP.Application.Production.Dtos
{
    public class ProductionAttachmentDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal SizeKb { get; set; }
        public string UploadedBy { get; set; } = string.Empty;
        public string UploadedAt { get; set; } = string.Empty;
    }

    public class ProductionTimelineEventDto
    {
        public string Id { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? FromStatus { get; set; }
        public string? ToStatus { get; set; }
        public string? Remarks { get; set; }
    }

    public class BomMaterialLineDto
    {
        public string Id { get; set; } = string.Empty;
        public int MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Uom { get; set; } = string.Empty;
        public decimal WastagePercent { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
    }

    public class BomListItemDto
    {
        public int Id { get; set; }
        public string BomNumber { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Revision { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string EffectiveDate { get; set; } = string.Empty;
        public int MaterialCount { get; set; }
        public string UpdatedAt { get; set; } = string.Empty;
    }

    public class BomDto
    {
        public int Id { get; set; }
        public string BomNumber { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Revision { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string EffectiveDate { get; set; } = string.Empty;
        public string? ExpiryDate { get; set; }
        public List<BomMaterialLineDto> Materials { get; set; } = new();
        public string ProcessNotes { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public List<ProductionAttachmentDto> Attachments { get; set; } = new();
        public List<ProductionTimelineEventDto> Timeline { get; set; } = new();
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
    }

    public class BomCreateRequestDto
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Revision { get; set; } = string.Empty;
        public string EffectiveDate { get; set; } = string.Empty;
        public string? ExpiryDate { get; set; }
        public List<BomMaterialLineDto> Materials { get; set; } = new();
        public string? ProcessNotes { get; set; }
        public string? Notes { get; set; }
    }

    public class BomUpdateRequestDto : BomCreateRequestDto
    {
        public string? Version { get; set; }
    }

    public class BomDashboardDto
    {
        public int TotalBoms { get; set; }
        public int ActiveBoms { get; set; }
        public int DraftBoms { get; set; }
        public int ArchivedBoms { get; set; }
    }

    public class PlanListItemDto
    {
        public int Id { get; set; }
        public string PlanNumber { get; set; } = string.Empty;
        public string PlanningPeriod { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal RequiredQuantity { get; set; }
        public decimal PlannedQuantity { get; set; }
        public string BomNumber { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Planner { get; set; } = string.Empty;
        public string ExpectedStart { get; set; } = string.Empty;
        public string ExpectedFinish { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class PlanDto
    {
        public int Id { get; set; }
        public string PlanNumber { get; set; } = string.Empty;
        public string PlanningPeriod { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal RequiredQuantity { get; set; }
        public decimal PlannedQuantity { get; set; }
        public int BomId { get; set; }
        public string BomNumber { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Planner { get; set; } = string.Empty;
        public string ExpectedStart { get; set; } = string.Empty;
        public string ExpectedFinish { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public List<ProductionAttachmentDto> Attachments { get; set; } = new();
        public List<ProductionTimelineEventDto> Timeline { get; set; } = new();
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
    }

    public class PlanCreateRequestDto
    {
        public string PlanningPeriod { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal RequiredQuantity { get; set; }
        public decimal PlannedQuantity { get; set; }
        public int BomId { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Planner { get; set; } = string.Empty;
        public string ExpectedStart { get; set; } = string.Empty;
        public string ExpectedFinish { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class PlanUpdateRequestDto : PlanCreateRequestDto
    {
    }

    public class PlanDashboardDto
    {
        public int PendingPlans { get; set; }
        public int ReleasedPlans { get; set; }
        public int CompletedPlans { get; set; }
        public int TotalPlans { get; set; }
    }

    public class WorkOrderListItemDto
    {
        public int Id { get; set; }
        public string WorkOrderNumber { get; set; } = string.Empty;
        public string PlanNumber { get; set; } = string.Empty;
        public string BomNumber { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal PlannedQuantity { get; set; }
        public decimal ProducedQuantity { get; set; }
        public decimal PendingQuantity { get; set; }
        public string Priority { get; set; } = string.Empty;
        public string Supervisor { get; set; } = string.Empty;
        public string? MachineName { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string DueDate { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsDelayed { get; set; }
    }

    public class WorkOrderDto
    {
        public int Id { get; set; }
        public string WorkOrderNumber { get; set; } = string.Empty;
        public int PlanId { get; set; }
        public string PlanNumber { get; set; } = string.Empty;
        public int BomId { get; set; }
        public string BomNumber { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal PlannedQuantity { get; set; }
        public decimal ProducedQuantity { get; set; }
        public decimal PendingQuantity { get; set; }
        public string Priority { get; set; } = string.Empty;
        public string Supervisor { get; set; } = string.Empty;
        public int? MachineId { get; set; }
        public string? MachineCode { get; set; }
        public string? MachineName { get; set; }
        public string AssignedTeam { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string DueDate { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public List<ProductionAttachmentDto> Attachments { get; set; } = new();
        public List<ProductionTimelineEventDto> Timeline { get; set; } = new();
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
    }

    public class WorkOrderCreateRequestDto
    {
        public int PlanId { get; set; }
        public int BomId { get; set; }
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal PlannedQuantity { get; set; }
        public string Priority { get; set; } = string.Empty;
        public string Supervisor { get; set; } = string.Empty;
        public int? MachineId { get; set; }
        public string AssignedTeam { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string DueDate { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class WorkOrderUpdateRequestDto : WorkOrderCreateRequestDto
    {
    }

    public class WorkOrderDashboardDto
    {
        public int OpenWorkOrders { get; set; }
        public int Running { get; set; }
        public int Completed { get; set; }
        public int Delayed { get; set; }
    }

    public class ScheduleListItemDto
    {
        public int Id { get; set; }
        public string ScheduleNumber { get; set; } = string.Empty;
        public string WorkOrderNumber { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string MachineCode { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string Shift { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public decimal Capacity { get; set; }
        public decimal UtilizationPercent { get; set; }
        public int DelayMinutes { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class ScheduleDto
    {
        public int Id { get; set; }
        public string ScheduleNumber { get; set; } = string.Empty;
        public int WorkOrderId { get; set; }
        public string WorkOrderNumber { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int MachineId { get; set; }
        public string MachineCode { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string Shift { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public decimal Capacity { get; set; }
        public decimal UtilizationPercent { get; set; }
        public int DelayMinutes { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public List<ProductionTimelineEventDto> Timeline { get; set; } = new();
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
    }

    public class ScheduleDashboardDto
    {
        public int TodaysSchedule { get; set; }
        public int RunningJobs { get; set; }
        public int UpcomingJobs { get; set; }
        public int DelayedJobs { get; set; }
    }

    public class MachineListItemDto
    {
        public int Id { get; set; }
        public string MachineCode { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public decimal RunningHours { get; set; }
        public decimal IdleHours { get; set; }
        public decimal BreakdownHours { get; set; }
        public decimal UtilizationPercent { get; set; }
        public string MaintenanceDue { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int AssignedWorkOrderCount { get; set; }
    }

    public class MachineDto
    {
        public int Id { get; set; }
        public string MachineCode { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public decimal RunningHours { get; set; }
        public decimal IdleHours { get; set; }
        public decimal BreakdownHours { get; set; }
        public decimal UtilizationPercent { get; set; }
        public string MaintenanceDue { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<int> AssignedWorkOrderIds { get; set; } = new();
        public string Notes { get; set; } = string.Empty;
        public List<ProductionTimelineEventDto> Timeline { get; set; } = new();
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
    }

    public class MachineDashboardDto
    {
        public int RunningMachines { get; set; }
        public int IdleMachines { get; set; }
        public int Breakdown { get; set; }
        public decimal AverageUtilization { get; set; }
    }

    public class EntryListItemDto
    {
        public int Id { get; set; }
        public string EntryNumber { get; set; } = string.Empty;
        public string WorkOrderNumber { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal ProducedQuantity { get; set; }
        public decimal GoodQuantity { get; set; }
        public decimal RejectedQuantity { get; set; }
        public string Shift { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string ProductionDate { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class EntryDto
    {
        public int Id { get; set; }
        public string EntryNumber { get; set; } = string.Empty;
        public int WorkOrderId { get; set; }
        public string WorkOrderNumber { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal ProducedQuantity { get; set; }
        public decimal GoodQuantity { get; set; }
        public decimal RejectedQuantity { get; set; }
        public string Shift { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public int MachineId { get; set; }
        public string MachineCode { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string ProductionDate { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public List<ProductionAttachmentDto> Attachments { get; set; } = new();
        public List<ProductionTimelineEventDto> Timeline { get; set; } = new();
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
    }

    public class EntryCreateRequestDto
    {
        public int WorkOrderId { get; set; }
        public decimal ProducedQuantity { get; set; }
        public decimal GoodQuantity { get; set; }
        public decimal RejectedQuantity { get; set; }
        public string Shift { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public int MachineId { get; set; }
        public string ProductionDate { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class EntryUpdateRequestDto
    {
        public int? WorkOrderId { get; set; }
        public decimal ProducedQuantity { get; set; }
        public decimal GoodQuantity { get; set; }
        public decimal RejectedQuantity { get; set; }
        public string Shift { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public int MachineId { get; set; }
        public string ProductionDate { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class EntryDashboardDto
    {
        public decimal TodaysProduction { get; set; }
        public decimal GoodQuantity { get; set; }
        public decimal RejectedQuantity { get; set; }
        public decimal Productivity { get; set; }
    }

    public class ConsumptionListItemDto
    {
        public int Id { get; set; }
        public string ConsumptionNumber { get; set; } = string.Empty;
        public string WorkOrderNumber { get; set; } = string.Empty;
        public string BomNumber { get; set; } = string.Empty;
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public decimal PlannedQuantity { get; set; }
        public decimal ActualQuantity { get; set; }
        public decimal Variance { get; set; }
        public string Uom { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public string StockOutReference { get; set; } = string.Empty;
    }

    public class ConsumptionDto
    {
        public int Id { get; set; }
        public string ConsumptionNumber { get; set; } = string.Empty;
        public int WorkOrderId { get; set; }
        public string WorkOrderNumber { get; set; } = string.Empty;
        public int BomId { get; set; }
        public string BomNumber { get; set; } = string.Empty;
        public int? EntryId { get; set; }
        public int MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public decimal PlannedQuantity { get; set; }
        public decimal ActualQuantity { get; set; }
        public decimal Variance { get; set; }
        public string Uom { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public string StockOutReference { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public List<ProductionTimelineEventDto> Timeline { get; set; } = new();
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
    }

    public class ConsumptionDashboardDto
    {
        public decimal PlannedConsumption { get; set; }
        public decimal ActualConsumption { get; set; }
        public decimal Variance { get; set; }
    }

    public class RejectionListItemDto
    {
        public int Id { get; set; }
        public string RejectionNumber { get; set; } = string.Empty;
        public string WorkOrderNumber { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string RejectionDate { get; set; } = string.Empty;
    }

    public class RejectionDto
    {
        public int Id { get; set; }
        public string RejectionNumber { get; set; } = string.Empty;
        public int WorkOrderId { get; set; }
        public string WorkOrderNumber { get; set; } = string.Empty;
        public int? EntryId { get; set; }
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public int MachineId { get; set; }
        public string MachineCode { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string RejectionDate { get; set; } = string.Empty;
        public string CorrectiveAction { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public List<ProductionTimelineEventDto> Timeline { get; set; } = new();
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
    }

    public class RejectionDashboardDto
    {
        public int TotalRejections { get; set; }
        public List<RejectionReasonCountDto> TopReasons { get; set; } = new();
        public decimal RejectionRate { get; set; }
    }

    public class RejectionReasonCountDto
    {
        public string Reason { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class DailyReportDto
    {
        public string Date { get; set; } = string.Empty;
        public string? Shift { get; set; }
        public DailyProductionSummaryDto ProductionSummary { get; set; } = new();
        public List<DailyShiftSummaryDto> ShiftSummary { get; set; } = new();
        public List<DailyMachineSummaryDto> MachineSummary { get; set; } = new();
        public List<DailyProductSummaryDto> ProductSummary { get; set; } = new();
        public decimal MaterialConsumed { get; set; }
        public decimal Utilization { get; set; }
        public int PendingWorkOrders { get; set; }
    }

    public class DailyProductionSummaryDto
    {
        public decimal TotalProduced { get; set; }
        public decimal GoodQuantity { get; set; }
        public decimal RejectedQuantity { get; set; }
        public decimal Productivity { get; set; }
    }

    public class DailyShiftSummaryDto
    {
        public string Shift { get; set; } = string.Empty;
        public decimal Produced { get; set; }
        public decimal Good { get; set; }
        public decimal Rejected { get; set; }
    }

    public class DailyMachineSummaryDto
    {
        public string MachineCode { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public decimal Utilization { get; set; }
        public decimal Produced { get; set; }
    }

    public class DailyProductSummaryDto
    {
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Produced { get; set; }
        public decimal Good { get; set; }
        public decimal Rejected { get; set; }
    }

    public class MonthlySummaryDto
    {
        public string Month { get; set; } = string.Empty;
        public decimal TotalProduced { get; set; }
        public decimal GoodQuantity { get; set; }
        public decimal RejectedQuantity { get; set; }
        public decimal MaterialConsumed { get; set; }
        public decimal AverageUtilization { get; set; }
        public int WorkOrdersCompleted { get; set; }
        public decimal RejectionRate { get; set; }
        public List<DailyTrendItemDto> DailyTrend { get; set; } = new();
    }

    public class DailyTrendItemDto
    {
        public string Date { get; set; } = string.Empty;
        public decimal Produced { get; set; }
        public decimal Good { get; set; }
        public decimal Rejected { get; set; }
    }

    public class ReportDashboardDto
    {
        public decimal TodaysProduced { get; set; }
        public decimal WeekProduced { get; set; }
        public decimal MonthProduced { get; set; }
        public decimal RejectionRate { get; set; }
        public decimal AverageUtilization { get; set; }
        public int PendingWorkOrders { get; set; }
    }

    public class StatusActionRequestDto
    {
        public string? Remarks { get; set; }
    }
}
