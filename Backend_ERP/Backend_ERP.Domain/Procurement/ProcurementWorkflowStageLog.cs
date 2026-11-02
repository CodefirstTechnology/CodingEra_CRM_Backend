using System;

namespace ERP.Domain.Procurement
{
    public class ProcurementWorkflowStageLog
    {
        public int Id { get; set; }

        public string TenantId { get; set; } = "TENANT-DEFAULT";

        public string EntityType { get; set; } = string.Empty; // PR, RFQ, PO, GRN, Bill

        public int EntityId { get; set; }

        public string DocumentNumber { get; set; } = string.Empty;

        public int StageSequence { get; set; } // 1 through 7

        public string StageName { get; set; } = string.Empty;
        // Stage 1: PR Creation -> Department Approval
        // Stage 2: PR Approval -> Consolidated RFQ Issuance
        // Stage 3: RFQ Bidding -> Comparison Matrix Award
        // Stage 4: Matrix Award -> Authorized PO Release
        // Stage 5: PO Transmission -> Gate Inward GRN
        // Stage 6: Gate Inward -> QC Inspection Clearance
        // Stage 7: QC Clearance -> 3-Way Match & Bill Posting

        public DateTime StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public decimal? ElapsedBusinessHours { get; set; }

        public decimal TargetSlaHours { get; set; }

        public bool IsSlaBreached { get; set; }

        public string? DepartmentName { get; set; }

        public int? ActionedByUserId { get; set; }

        public string? ActionedByUserName { get; set; }

        public string? Remarks { get; set; }
    }
}

