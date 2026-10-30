using System;

namespace ERP.Domain.Sales
{
    public class SalesTargetApprovalHistory
    {
        public int Id { get; set; }

        public string TenantId { get; set; } = "default";

        public int SalesTargetId { get; set; }

        public SalesTarget SalesTarget { get; set; } = null!;

        public string Action { get; set; } = string.Empty; // "Submitted", "Approved", "Rejected", "Revised"

        public string ApproverUserId { get; set; } = string.Empty;

        public string ApproverName { get; set; } = string.Empty;

        public string ApproverRole { get; set; } = string.Empty;

        public string Comments { get; set; } = string.Empty;

        public string HmacSignatureToken { get; set; } = string.Empty;

        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    }
}

