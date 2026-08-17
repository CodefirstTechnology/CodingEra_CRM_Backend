using System;

namespace ERP.Domain.Procurement
{
    public class PurchaseOrderApprovalHistory
    {
        public int Id { get; set; }

        public int PurchaseOrderId { get; set; }

        public PurchaseOrder? PurchaseOrder { get; set; }

        public string EventKind { get; set; } = string.Empty; // submitted, approved, rejected, revision_requested, reopened

        public string Decision { get; set; } = string.Empty;

        public string Approver { get; set; } = string.Empty;

        public string Role { get; set; } = "Procurement Manager";

        public DateTime DecisionDate { get; set; } = DateTime.UtcNow;

        public string Remarks { get; set; } = string.Empty;
    }
}
