using System;
using System.Collections.Generic;

namespace ERP.Domain.Procurement
{
    public class VendorQuotation
    {
        public int Id { get; set; }

        public string QuotationNumber { get; set; } = string.Empty;

        public int? RequestForQuotationId { get; set; }

        public RequestForQuotation? RequestForQuotation { get; set; }

        public int VendorId { get; set; }

        public Vendor? Vendor { get; set; }

        public string VendorName { get; set; } = string.Empty;

        public string QuotationRef { get; set; } = string.Empty;

        public DateTime QuotationDate { get; set; } = DateTime.UtcNow;

        public DateTime? ValidityDate { get; set; }

        public int DeliveryTimeDays { get; set; }

        public string LeadTime { get; set; } = string.Empty;

        public string WarrantyPeriod { get; set; } = string.Empty;

        public string PaymentTerms { get; set; } = string.Empty;

        public decimal SubTotal { get; set; }

        public decimal DiscountPercent { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal TaxPercent { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal TotalCost { get; set; }

        public string Remarks { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }

        public List<VendorQuotationLine> Lines { get; set; } = new();
    }
}
