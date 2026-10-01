using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Backend_ERP.Domain.Entities
{
    public class ProductionEntryEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string EntryNumber { get; set; } = null!;
        public Guid WorkOrderOperationId { get; set; }
        public string ShiftId { get; set; } = null!; // ShiftA, ShiftB, ShiftC
        public string OperatorId { get; set; } = null!;
        public Guid MachineId { get; set; }
        public decimal GoodQuantity { get; set; }
        public decimal ScrappedQuantity { get; set; }
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime EndTime { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "SUBMITTED"; // DRAFT, SUBMITTED, APPROVED, REJECTED
        public string? SupervisorApprovedBy { get; set; }
        public DateTime? SupervisorApprovedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // PostgreSQL xmin concurrency token
        public uint RowVersion { get; set; }

        public ICollection<MaterialConsumptionLogEntity> MaterialLogs { get; set; } = new List<MaterialConsumptionLogEntity>();
        public ICollection<RejectionTrackingLogEntity> RejectionLogs { get; set; } = new List<RejectionTrackingLogEntity>();
    }

    public class MaterialConsumptionLogEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProductionEntryId { get; set; }
        public Guid RawMaterialItemId { get; set; }
        public Guid? BatchId { get; set; }
        public decimal StandardBomQuantity { get; set; }
        public decimal ActualConsumedQuantity { get; set; }
        public decimal VarianceQuantity { get; set; }
        public string ConsumptionType { get; set; } = "BACKFLUSH"; // BACKFLUSH, MANUAL
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ProductionEntryEntity ProductionEntry { get; set; } = null!;
    }

    public class RejectionTrackingLogEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProductionEntryId { get; set; }
        public Guid WorkOrderId { get; set; }
        public string DefectCategory { get; set; } = "OPERATOR_ERROR"; // DIMENSIONAL, SURFACE, MATERIAL_FLAW, OPERATOR_ERROR, SETUP_REJECT
        public string DefectReasonCode { get; set; } = null!;
        public decimal RejectedQuantity { get; set; }
        public string Disposition { get; set; } = "SCRAP"; // SCRAP, REWORK, VENDOR_RETURN
        public decimal UnitScrapCost { get; set; }
        public decimal TotalLossCost { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ProductionEntryEntity ProductionEntry { get; set; } = null!;
    }

    public class DailyProductionReportEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime ReportDate { get; set; }
        public string ShiftId { get; set; } = null!;
        public Guid WorkCenterId { get; set; }
        public decimal TotalPlannedQty { get; set; }
        public decimal TotalGoodQty { get; set; }
        public decimal TotalScrapQty { get; set; }
        public decimal TotalDowntimeMinutes { get; set; }
        public decimal OeePercentage { get; set; }
        public decimal AvailabilityPercentage { get; set; }
        public decimal PerformancePercentage { get; set; }
        public decimal QualityPercentage { get; set; }
        public bool IsFinalized { get; set; } = false;
        public string? FinalizedBy { get; set; }
        public DateTime? FinalizedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // PostgreSQL xmin concurrency token
        public uint RowVersion { get; set; }

        public ICollection<DailyProductionReportEntryJunctionEntity> ReportEntries { get; set; } = new List<DailyProductionReportEntryJunctionEntity>();
    }

    public class DailyProductionReportEntryJunctionEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DailyProductionReportId { get; set; }
        public Guid ProductionEntryId { get; set; }
        public DateTime LinkedAt { get; set; } = DateTime.UtcNow;

        public DailyProductionReportEntity Report { get; set; } = null!;
        public ProductionEntryEntity ProductionEntry { get; set; } = null!;
    }
}
