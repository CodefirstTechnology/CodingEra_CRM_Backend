using System;

namespace ERP.Domain.Procurement
{
    public class RejectionAnalysis
    {
        public int Id { get; set; }

        public string RejectionNumber { get; set; } = string.Empty;

        public DateTime RejectionDate { get; set; } = DateTime.UtcNow;

        public RejectionSource Source { get; set; } = RejectionSource.Incoming;

        public int SourceRecordId { get; set; }

        public string SourceRecordNumber { get; set; } = string.Empty;

        public int? MaterialId { get; set; }

        public string? MaterialCode { get; set; }

        public string? MaterialName { get; set; }

        public int? ProductId { get; set; }

        public string? ProductCode { get; set; }

        public string? ProductName { get; set; }

        public string BatchNumber { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public string Reason { get; set; } = string.Empty;

        public string RootCause { get; set; } = string.Empty;

        public string Department { get; set; } = string.Empty;

        public string Operator { get; set; } = string.Empty;

        public int? MachineId { get; set; }

        public string? MachineCode { get; set; }

        public string? MachineName { get; set; }

        public string? SupplierName { get; set; }

        public string CorrectiveAction { get; set; } = string.Empty;

        public string PreventiveAction { get; set; } = string.Empty;

        public RejectionAnalysisStatus Status { get; set; } = RejectionAnalysisStatus.Open;

        public string Remarks { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }
    }
}
