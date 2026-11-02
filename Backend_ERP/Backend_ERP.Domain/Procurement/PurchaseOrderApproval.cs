using System;

namespace ERP.Domain.Procurement
{
    public class PurchaseOrderApproval
    {
        public int Id { get; set; }
        public int TenantId { get; set; } = 1;

        public int PurchaseOrderId { get; set; }
        public PurchaseOrder? PurchaseOrder { get; set; }

        public PurchaseOrderApprovalTier TierLevel { get; set; }
        public string ApproverRole { get; set; } = string.Empty;
        public string ApproverUserId { get; set; } = string.Empty;
        public string ApproverName { get; set; } = string.Empty;
        public PurchaseOrderApprovalDecision Decision { get; set; } = PurchaseOrderApprovalDecision.Pending;
        public string? Comments { get; set; }
        public string? HmacSignatureToken { get; set; }
        public bool IsExceptionApproval { get; set; } = false;
        public DateTime? ActionedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

