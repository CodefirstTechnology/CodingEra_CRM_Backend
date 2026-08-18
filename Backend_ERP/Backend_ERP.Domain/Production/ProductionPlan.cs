using System;
using ERP.Domain.Procurement;

namespace ERP.Domain.Production
{
    public class ProductionPlan
    {
        public int Id { get; set; }

        public string PlanNumber { get; set; } = string.Empty;

        public string PlanningPeriod { get; set; } = string.Empty;

        public int ProductId { get; set; }

        public FinishedGood? Product { get; set; }

        public string ProductCode { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public decimal RequiredQuantity { get; set; }

        public decimal PlannedQuantity { get; set; }

        public int BomId { get; set; }

        public BillOfMaterials? Bom { get; set; }

        public string BomNumber { get; set; } = string.Empty;

        public int WarehouseId { get; set; }

        public Warehouse? Warehouse { get; set; }

        public string WarehouseName { get; set; } = string.Empty;

        public Priority Priority { get; set; } = Priority.Medium;

        public string Planner { get; set; } = string.Empty;

        public DateOnly ExpectedStart { get; set; }

        public DateOnly ExpectedFinish { get; set; }

        public PlanStatus Status { get; set; } = PlanStatus.Draft;

        public string Notes { get; set; } = string.Empty;

        public string? AttachmentName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }
    }
}
