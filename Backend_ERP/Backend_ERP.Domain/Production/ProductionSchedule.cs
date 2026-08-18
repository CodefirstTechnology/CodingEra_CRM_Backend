using System;
using ERP.Domain.Procurement;

namespace ERP.Domain.Production
{
    public class ProductionSchedule
    {
        public int Id { get; set; }

        public string ScheduleNumber { get; set; } = string.Empty;

        public int WorkOrderId { get; set; }

        public WorkOrder? WorkOrder { get; set; }

        public string WorkOrderNumber { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public int MachineId { get; set; }

        public Machine? Machine { get; set; }

        public string MachineCode { get; set; } = string.Empty;

        public string MachineName { get; set; } = string.Empty;

        public Shift Shift { get; set; } = Shift.A;

        public string Operator { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public decimal Capacity { get; set; }

        public decimal UtilizationPercent { get; set; }

        public int DelayMinutes { get; set; }

        public ScheduleStatus Status { get; set; } = ScheduleStatus.Scheduled;

        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }
    }
}
