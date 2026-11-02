using System;
using System.Collections.Generic;

namespace ERP.Domain.Procurement
{
    public class PurchaseBill
    {
        public int Id { get; set; }

        public string BillNumber { get; set; } = string.Empty;

        public string InvoiceNumber { get; set; } = string.Empty;

        public int VendorId { get; set; }

        public string VendorName { get; set; } = string.Empty;

        public int? PurchaseOrderId { get; set; }

        public string? PurchaseOrderNumber { get; set; }

        public int? GRNId { get; set; }

        public string? GRNNumber { get; set; }

        // Multi-GRN Consolidation
        public string? ConsolidatedGrnIds { get; set; }

        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

        public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(30);

        public string Currency { get; set; } = "INR";

        public decimal SubTotal { get; set; }

        public decimal DiscountTotal { get; set; }

        public decimal TaxTotal { get; set; }

        public decimal RoundOff { get; set; }

        public decimal GrandTotal { get; set; }

        public decimal PaidAmount { get; set; }

        public decimal BalanceAmount { get; set; }

        public string PaymentTerms { get; set; } = "Net 30 Days";

        public PurchaseBillPaymentStatus PaymentStatus { get; set; } = PurchaseBillPaymentStatus.Unpaid;

        public PurchaseBillStatus Status { get; set; } = PurchaseBillStatus.Draft;

        // 3-Way Matching Engine Attributes
        public ThreeWayMatchStatus ThreeWayMatchStatus { get; set; } = ThreeWayMatchStatus.Pending;

        public bool IsThreeWayMatched { get; set; }

        public decimal MatchedAmount { get; set; }

        public decimal VarianceAmount { get; set; }

        public string? VarianceOverrideReason { get; set; }

        public string? VarianceOverriddenBy { get; set; }

        public DateTime? VarianceOverriddenAt { get; set; }

        // GST Breakup
        public decimal CgstAmount { get; set; }

        public decimal SgstAmount { get; set; }

        public decimal IgstAmount { get; set; }

        // Statutory MSME 43B(h) Countdown
        public bool IsMsmeSubjectTo43B { get; set; }

        public DateTime? MsmeDueDate { get; set; }

        public int MsmeAllowedDays { get; set; } = 45;

        // Statutory TDS Section 194Q
        public bool IsTds194QApplicable { get; set; }

        public decimal Tds194QPercentage { get; set; } = 0.10m;

        public decimal Tds194QAmount { get; set; }

        public string? TdsSection { get; set; }

        public decimal TdsPercentage { get; set; }

        public decimal TdsAmount { get; set; }

        // GSTR-2B ITC Matching
        public string? Gstr2BStatus { get; set; } = "Pending";

        public string? Gstr2BRef { get; set; }

        public string? VarianceReason { get; set; }

        public DateTime? PostedAt { get; set; }

        public int? PostedByUserId { get; set; }

        public string Remarks { get; set; } = string.Empty;

        public int AttachmentsCount { get; set; }

        public uint RowVersion { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }

        public List<PurchaseBillLine> Lines { get; set; } = new();

        public List<PurchaseBillHistory> History { get; set; } = new();
    }
}
