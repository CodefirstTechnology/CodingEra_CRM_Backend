using System;
using ERP.Domain.Procurement;

namespace ERP.Domain.Production
{
    public class WorkOrder
    {
        public int Id { get; set; }

        public string WorkOrderNumber { get; set; } = string.Empty;

        public int PlanId { get; set; }

        public ProductionPlan? Plan { get; set; }

        public string PlanNumber { get; set; } = string.Empty;

        public int BomId { get; set; }

        public BillOfMaterials? Bom { get; set; }

        public string BomNumber { get; set; } = string.Empty;

        public int ProductId { get; set; }

        public FinishedGood? Product { get; set; }

        public string ProductCode { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public decimal PlannedQuantity { get; set; }

        public decimal ProducedQuantity { get; set; }

        public decimal PendingQuantity { get; set; }

        public Priority Priority { get; set; } = Priority.Medium;

        public string Supervisor { get; set; } = string.Empty;

        public int? MachineId { get; set; }

        public Machine? Machine { get; set; }

        public string? MachineCode { get; set; }

        public string? MachineName { get; set; }

        public string AssignedTeam { get; set; } = string.Empty;

        public DateOnly StartDate { get; set; }

        public DateOnly DueDate { get; set; }

        public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Draft;

        public string Notes { get; set; } = string.Empty;

        public string? AttachmentName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }
    }
}
