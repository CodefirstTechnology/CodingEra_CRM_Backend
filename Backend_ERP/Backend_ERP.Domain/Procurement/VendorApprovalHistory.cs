using System;

namespace ERP.Domain.Procurement
{
    public class VendorApprovalHistory
    {
        public int Id { get; set; }

        public string TenantId { get; set; } = "default";

        public int VendorId { get; set; }

        public Vendor? Vendor { get; set; }

        public VendorStatus PreviousStatus { get; set; }

        public VendorStatus NewStatus { get; set; }

        public string Action { get; set; } = string.Empty;

        public string Comments { get; set; } = string.Empty;

        public string ActionedByUserId { get; set; } = string.Empty;

        public string ActionedByUserName { get; set; } = string.Empty;

        public string? ApprovalToken { get; set; }

        public DateTime ActionedAt { get; set; } = DateTime.UtcNow;
    }
}

