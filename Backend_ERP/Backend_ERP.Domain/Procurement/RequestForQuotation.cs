using System;
using System.Collections.Generic;

namespace ERP.Domain.Procurement
{
    public class RequestForQuotation
    {
        public int Id { get; set; }

        public string RFQNumber { get; set; } = string.Empty;

        public DateTime RFQDate { get; set; } = DateTime.UtcNow;

        public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(7);

        public int? PurchaseRequisitionId { get; set; }

        public PurchaseRequisition? PurchaseRequisition { get; set; }

        public string PurchaseRequisitionNumber { get; set; } = string.Empty;

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

        public List<RequestForQuotationLine> Lines { get; set; } = new();

        public List<RequestForQuotationVendor> Vendors { get; set; } = new();

        public List<RequestForQuotationStatusHistory> History { get; set; } = new();
    }
}
