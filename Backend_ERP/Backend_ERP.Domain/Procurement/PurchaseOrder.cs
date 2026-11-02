using System;
using System.Collections.Generic;

namespace ERP.Domain.Procurement
{
    public class PurchaseOrder
    {
        public int Id { get; set; }
        public int TenantId { get; set; } = 1;

        // Document Identification & Revision Engine
        public string PurchaseOrderNumber { get; set; } = string.Empty;
        public int RevisionNumber { get; set; } = 0;
        public int? ParentPOId { get; set; }
        public PurchaseOrder? ParentPO { get; set; }
        public bool IsCurrentRevision { get; set; } = true;
        public DateTime? SupersededAt { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;

        // Order Timestamps
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public DateTime? ExpectedDeliveryDate { get; set; }
        public DateTime? AcknowledgedAt { get; set; }

        // Vendor Snapshot (Immutable Contractual Baseline)
        public int? VendorId { get; set; }
        public Vendor? Vendor { get; set; }
        public string VendorCode { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public string VendorGstin { get; set; } = string.Empty;
        public string VendorStateCode { get; set; } = string.Empty;
        public string VendorPan { get; set; } = string.Empty;
        public string VendorContact { get; set; } = string.Empty;
        public string VendorEmail { get; set; } = string.Empty;
        public string VendorPhone { get; set; } = string.Empty;
        public string GstNumber { get => VendorGstin; set => VendorGstin = value; }
        public string BillingAddress { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;

        // Commercial Terms
        public int PaymentTermsDays { get; set; } = 30;
        public string PaymentTerms { get; set; } = "Net 30 Days";
        public string Incoterms { get; set; } = "FOR Destination";
        public string DeliveryTerms { get; set; } = "Door Delivery";
        public string BuyerName { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;

        // Multi-Currency & Treasury
        public string Currency { get; set; } = "INR";
        public decimal ExchangeRate { get; set; } = 1.000000m;

        // Upstream Lineage Bridges
        public int? PurchaseRequisitionId { get; set; }
        public PurchaseRequisition? PurchaseRequisition { get; set; }
        public int? RFQId { get; set; }
        public RequestForQuotation? RFQ { get; set; }
        public int? VendorComparisonId { get; set; }
        public VendorComparison? VendorComparison { get; set; }
        public string SalesOrderNumber { get; set; } = string.Empty;

        // Financial Totals (precision 18,2)
        public decimal Subtotal { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal TotalCgstAmount { get; set; }
        public decimal TotalSgstAmount { get; set; }
        public decimal TotalIgstAmount { get; set; }
        public decimal TaxTotal { get; set; }
        public decimal FreightAmount { get; set; }
        public decimal InsuranceAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal GrandTotalBaseCurrency { get; set; }

        // Lifecycle & Governance
        public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
        public PurchaseOrderPriority Priority { get; set; } = PurchaseOrderPriority.Normal;
        public PurchaseOrderSourceType SourceType { get; set; } = PurchaseOrderSourceType.Manual;

        // DOA & Cryptographic Sign-Off
        public PurchaseOrderApprovalTier RequiredApprovalTier { get; set; } = PurchaseOrderApprovalTier.Tier1_PlantManager;
        public PurchaseOrderApprovalTier CurrentApprovalTier { get; set; } = PurchaseOrderApprovalTier.None;
        public bool IsExceptionApproval { get; set; } = false;
        public string ExceptionReasonsJson { get; set; } = "[]";
        public string? ApprovalToken { get; set; }
        public string? ApprovedByUserId { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? SubmittedDate { get; set; }

        // Amendment Metadata
        public string? AmendmentReason { get; set; }
        public POAmendmentType? AmendmentType { get; set; }

        // Auditing & Concurrency
        public uint ConcurrencyVersion { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string UpdatedBy { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }

        // Child Collections
        public List<PurchaseOrderLine> Lines { get; set; } = new();
        public List<PurchaseOrderApproval> Approvals { get; set; } = new();
        public List<PurchaseOrderAmendmentHistory> AmendmentHistories { get; set; } = new();
        public List<PurchaseOrderStatusHistory> History { get; set; } = new();
        public List<PurchaseOrderApprovalHistory> ApprovalHistory { get; set; } = new();
    }
}
