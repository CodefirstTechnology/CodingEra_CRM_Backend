using System;
using ERP.Domain.Procurement;

namespace ERP.Domain.Production
{
    public class ProductionEntry
    {
        public int Id { get; set; }

        public string EntryNumber { get; set; } = string.Empty;

        public int WorkOrderId { get; set; }

        public WorkOrder? WorkOrder { get; set; }

        public string WorkOrderNumber { get; set; } = string.Empty;

        public int ProductId { get; set; }

        public FinishedGood? Product { get; set; }

        public string ProductCode { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public decimal ProducedQuantity { get; set; }

        public decimal GoodQuantity { get; set; }

        public decimal RejectedQuantity { get; set; }

        public Shift Shift { get; set; } = Shift.A;

        public string Operator { get; set; } = string.Empty;

        public int MachineId { get; set; }

        public Machine? Machine { get; set; }

        public string MachineCode { get; set; } = string.Empty;

        public string MachineName { get; set; } = string.Empty;

        public DateOnly ProductionDate { get; set; }

        public EntryStatus Status { get; set; } = EntryStatus.Draft;

        public string Notes { get; set; } = string.Empty;

        public string? AttachmentName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }
    }
}
