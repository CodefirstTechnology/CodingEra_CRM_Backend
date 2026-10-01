using System;
using System.Collections.Generic;

namespace ERP.Application.Production.Dtos
{
    public class CreateProductionEntryRequestDto
    {
        public Guid WorkOrderOperationId { get; set; }
        public string ShiftId { get; set; } = string.Empty;
        public string OperatorId { get; set; } = string.Empty;
        public Guid MachineId { get; set; }
        public decimal GoodQuantity { get; set; }
        public decimal ScrappedQuantity { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<MaterialConsumptionDto> MaterialConsumptions { get; set; } = new();
        public List<RejectionLogDto> Rejections { get; set; } = new();
    }

    public class MaterialConsumptionDto
    {
        public Guid RawMaterialItemId { get; set; }
        public Guid? BatchId { get; set; }
        public decimal StandardBomQuantity { get; set; }
        public decimal ActualConsumedQuantity { get; set; }
        public string ConsumptionType { get; set; } = "BACKFLUSH"; // BACKFLUSH or MANUAL
    }

    public class RejectionLogDto
    {
        public string DefectCategory { get; set; } = "OPERATOR_ERROR"; // DIMENSIONAL, SURFACE, MATERIAL_FLAW, OPERATOR_ERROR, SETUP_REJECT
        public string DefectReasonCode { get; set; } = string.Empty;
        public decimal RejectedQuantity { get; set; }
        public string Disposition { get; set; } = "SCRAP"; // SCRAP, REWORK, VENDOR_RETURN
        public decimal UnitScrapCost { get; set; }
    }

    public class ProductionEntryResponseDto
    {
        public Guid Id { get; set; }
        public string EntryNumber { get; set; } = string.Empty;
        public Guid WorkOrderOperationId { get; set; }
        public string ShiftId { get; set; } = string.Empty;
        public string OperatorId { get; set; } = string.Empty;
        public Guid MachineId { get; set; }
        public decimal GoodQuantity { get; set; }
        public decimal ScrappedQuantity { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? SupervisorApprovedBy { get; set; }
        public DateTime? SupervisorApprovedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<MaterialConsumptionDto> Consumptions { get; set; } = new();
        public List<RejectionLogDto> Rejections { get; set; } = new();
    }

    public class GenerateDprRequestDto
    {
        public DateTime ReportDate { get; set; }
        public string ShiftId { get; set; } = string.Empty;
        public Guid WorkCenterId { get; set; }
    }

    public class FinalizeDprRequestDto
    {
        public Guid ReportId { get; set; }
        public string FinalizedBy { get; set; } = string.Empty;
    }

    public class DailyProductionReportDto
    {
        public Guid Id { get; set; }
        public DateTime ReportDate { get; set; }
        public string ShiftId { get; set; } = string.Empty;
        public Guid WorkCenterId { get; set; }
        public decimal TotalPlannedQty { get; set; }
        public decimal TotalGoodQty { get; set; }
        public decimal TotalScrapQty { get; set; }
        public decimal TotalDowntimeMinutes { get; set; }
        public decimal OeePercentage { get; set; }
        public decimal AvailabilityPercentage { get; set; }
        public decimal PerformancePercentage { get; set; }
        public decimal QualityPercentage { get; set; }
        public bool IsFinalized { get; set; }
        public string? FinalizedBy { get; set; }
        public DateTime? FinalizedAt { get; set; }
        public List<Guid> LinkedProductionEntryIds { get; set; } = new();
    }
}
