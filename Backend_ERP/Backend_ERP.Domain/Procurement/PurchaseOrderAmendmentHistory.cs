using System;

namespace ERP.Domain.Procurement
{
    public class PurchaseOrderAmendmentHistory
    {
        public int Id { get; set; }
        public int TenantId { get; set; } = 1;

        public int PurchaseOrderId { get; set; }
        public PurchaseOrder? PurchaseOrder { get; set; }

        public int RevisionNumber { get; set; }
        public string ChangedField { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string AmendmentReason { get; set; } = string.Empty;
        public string ActionedByUserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

