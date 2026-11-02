using System;

namespace ERP.Domain.Procurement
{
    public class ThreeWayMatchAuditLog
    {
        public int Id { get; set; }

        public int PurchaseBillId { get; set; }

        public string BillNumber { get; set; } = string.Empty;

        public int? PurchaseOrderId { get; set; }

        public int? GoodsReceiptId { get; set; }

        public int? PurchaseBillLineId { get; set; }

        public string ItemName { get; set; } = string.Empty;

        public string CheckType { get; set; } = string.Empty; // Rate, Quantity, Tax, Rounding

        public decimal PoValue { get; set; }

        public decimal GrnValue { get; set; }

        public decimal BillValue { get; set; }

        public decimal Variance { get; set; }

        public decimal VariancePercentage { get; set; }

        public bool IsWithinTolerance { get; set; }

        public string Status { get; set; } = string.Empty; // Passed, Warning, Failed, Blocked

        public string? DiscrepancyMessage { get; set; }

        public string? OverriddenBy { get; set; }

        public string? OverrideReason { get; set; }

        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    }
}

