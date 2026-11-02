using System;

namespace ERP.Domain.Procurement
{
    public class ProcurementAuditLog
    {
        public long Id { get; set; }

        public string TenantId { get; set; } = "TENANT-DEFAULT";

        public long BlockIndex { get; set; } // Monotonically increasing index: 1, 2, 3...

        public string EntityName { get; set; } = string.Empty; // PurchaseBill, PurchaseOrder, GoodsReceipt, Vendor, etc.

        public string EntityId { get; set; } = string.Empty;

        public string ActionType { get; set; } = string.Empty; // CREATE, UPDATE, APPROVE, OVERRIDE, DELETE, REVISION

        public string ChangedByUser { get; set; } = string.Empty;

        public string? UserIpAddress { get; set; }

        public string? OldValuesJson { get; set; } // JSONB in PostgreSQL

        public string? NewValuesJson { get; set; } // JSONB in PostgreSQL

        // Cryptographic Chain Integrity
        public string PreviousLogHash { get; set; } = string.Empty; // 64-char Hex (HMAC-SHA256)

        public string CurrentLogHash { get; set; } = string.Empty;  // 64-char Hex (HMAC-SHA256)

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}

