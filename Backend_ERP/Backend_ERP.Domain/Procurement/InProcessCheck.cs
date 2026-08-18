using System;

namespace ERP.Domain.Procurement
{
    public class InProcessCheck
    {
        public int Id { get; set; }

        public string QCNumber { get; set; } = string.Empty;

        public int ProductionEntryId { get; set; }

        public string ProductionEntryNumber { get; set; } = string.Empty;

        public int WorkOrderId { get; set; }

        public string WorkOrderNumber { get; set; } = string.Empty;

        public int MachineId { get; set; }

        public string MachineCode { get; set; } = string.Empty;

        public string MachineName { get; set; } = string.Empty;

        public string Operator { get; set; } = string.Empty;

        public Shift Shift { get; set; } = Shift.A;

        public string Stage { get; set; } = string.Empty;

        public int ProductId { get; set; }

        public string ProductCode { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public string BatchNumber { get; set; } = string.Empty;

        public string Parameter { get; set; } = string.Empty;

        public string Tolerance { get; set; } = string.Empty;

        public string ExpectedValue { get; set; } = string.Empty;

        public string ActualValue { get; set; } = string.Empty;

        public QcCheckResult Result { get; set; } = QcCheckResult.Pending;

        public string Remarks { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public InProcessQcStatus Status { get; set; } = InProcessQcStatus.Draft;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }
    }
}
