using System;

namespace ERP.Domain.Procurement
{
    public class LoadTestReport
    {
        public int Id { get; set; }

        public string ReportNumber { get; set; } = string.Empty;

        public DateTime TestDate { get; set; } = DateTime.UtcNow;

        public int ProductId { get; set; }

        public string ProductCode { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public int FinalInspectionId { get; set; }

        public string FinalInspectionNumber { get; set; } = string.Empty;

        public int MachineId { get; set; }

        public string MachineCode { get; set; } = string.Empty;

        public string MachineName { get; set; } = string.Empty;

        public decimal LoadCapacity { get; set; }

        public decimal AppliedLoad { get; set; }

        public int DurationMinutes { get; set; }

        public LoadTestStatus Result { get; set; } = LoadTestStatus.Pending;

        public string PassFail { get; set; } = "Pending";

        public string Remarks { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }
    }
}
