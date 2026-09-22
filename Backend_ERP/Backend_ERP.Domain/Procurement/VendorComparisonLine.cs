namespace ERP.Domain.Procurement
{
    public class VendorComparisonLine
    {
        public int Id { get; set; }

        public int VendorComparisonId { get; set; }

        public VendorComparison? VendorComparison { get; set; }

        public int? ItemId { get; set; }

        public string ItemName { get; set; } = string.Empty;

        public int? RequestForQuotationLineId { get; set; }

        public RequestForQuotationLine? RequestForQuotationLine { get; set; }

        public decimal Quantity { get; set; }

        public string Uom { get; set; } = "PCS";

        public decimal LowestUnitPrice { get; set; }

        public int? LowestVendorId { get; set; }

        public string LowestVendorName { get; set; } = string.Empty;

        public bool IsSplitAwarded { get; set; }

        public string Remarks { get; set; } = string.Empty;
    }
}
