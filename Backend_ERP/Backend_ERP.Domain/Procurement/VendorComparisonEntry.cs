using System.Collections.Generic;

namespace ERP.Domain.Procurement
{
    public class VendorComparisonEntry
    {
        public int Id { get; set; }

        public int VendorComparisonId { get; set; }

        public VendorComparison? VendorComparison { get; set; }

        public int VendorId { get; set; }

        public Vendor? Vendor { get; set; }

        public string VendorName { get; set; } = string.Empty;

        public string QuotationRef { get; set; } = string.Empty;

        public int? VendorQuotationId { get; set; }

        public VendorQuotation? VendorQuotation { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TaxPercent { get; set; }

        public decimal DiscountPercent { get; set; }

        public int DeliveryTimeDays { get; set; }

        public string LeadTime { get; set; } = string.Empty;

        public string WarrantyPeriod { get; set; } = string.Empty;

        public string PaymentTerms { get; set; } = string.Empty;

        public decimal TotalCost { get; set; }

        public int Ranking { get; set; }

        public bool IsLowestPrice { get; set; }

        public bool IsBestDelivery { get; set; }

        public double RecommendationScore { get; set; }

        public string Remarks { get; set; } = string.Empty;

        public List<VendorQuotationLine> Lines { get; set; } = new();
    }
}
