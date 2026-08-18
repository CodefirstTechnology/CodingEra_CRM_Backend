using System;
using ERP.Domain.Procurement;

namespace ERP.Domain.Production
{
    public class RejectionRecord
    {
        public int Id { get; set; }

        public string RejectionNumber { get; set; } = string.Empty;

        public int WorkOrderId { get; set; }

        public WorkOrder? WorkOrder { get; set; }

        public string WorkOrderNumber { get; set; } = string.Empty;

        public int? EntryId { get; set; }

        public ProductionEntry? Entry { get; set; }

        public int ProductId { get; set; }

        public FinishedGood? Product { get; set; }

        public string ProductCode { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public string Reason { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Operator { get; set; } = string.Empty;

        public int MachineId { get; set; }

        public Machine? Machine { get; set; }

        public string MachineCode { get; set; } = string.Empty;

        public string MachineName { get; set; } = string.Empty;

        public DateOnly RejectionDate { get; set; }

        public string CorrectiveAction { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }
    }
}
