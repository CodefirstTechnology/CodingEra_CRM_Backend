using System;
using System.Collections.Generic;

namespace ERP.Domain.Procurement
{
    public class RequestForQuotation
    {
        public int Id { get; set; }

        public int TenantId { get; set; } = 1;

        public string RFQNumber { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public DateTime RFQDate { get; set; } = DateTime.UtcNow;

        public DateTime IssueDate { get; set; } = DateTime.UtcNow;

        public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(7);

        public DateTime BidClosingDate { get; set; } = DateTime.UtcNow.AddDays(7);

        public int? PurchaseRequisitionId { get; set; }

        public PurchaseRequisition? PurchaseRequisition { get; set; }

        public string PurchaseRequisitionNumber { get; set; } = string.Empty;

        public RFQSourcingType SourcingType { get; set; } = RFQSourcingType.OpenCompetitive;

        public bool IsSealedBid { get; set; } = true;

        public RFQStatus Status { get; set; } = RFQStatus.Draft;

        public string DeliveryTerms { get; set; } = string.Empty;

        public string PaymentTerms { get; set; } = string.Empty;

        public string VendorNotes { get; set; } = string.Empty;

        public int? ComparisonId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }

        public uint ConcurrencyVersion { get; set; }

        public List<RequestForQuotationLine> Lines { get; set; } = new();

        public List<RequestForQuotationVendor> Vendors { get; set; } = new();

        public List<RequestForQuotationStatusHistory> History { get; set; } = new();
    }
}
