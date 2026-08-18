using System;

namespace ERP.Domain.Production
{
    public class Machine
    {
        public int Id { get; set; }

        public string MachineCode { get; set; } = string.Empty;

        public string MachineName { get; set; } = string.Empty;

        public string Department { get; set; } = string.Empty;

        public decimal RunningHours { get; set; }

        public decimal IdleHours { get; set; }

        public decimal BreakdownHours { get; set; }

        public decimal UtilizationPercent { get; set; }

        public DateOnly MaintenanceDue { get; set; }

        public MachineStatus Status { get; set; } = MachineStatus.Idle;

        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }
    }
}
