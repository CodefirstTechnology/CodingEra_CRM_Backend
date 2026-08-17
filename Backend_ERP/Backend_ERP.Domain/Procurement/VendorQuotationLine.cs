namespace ERP.Domain.Procurement
{
    public class VendorQuotationLine
    {
        public int Id { get; set; }

        public int VendorQuotationId { get; set; }

        public VendorQuotation? VendorQuotation { get; set; }

        public string ItemName { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public string Uom { get; set; } = "PCS";

        public decimal UnitPrice { get; set; }

        public decimal TaxPercent { get; set; }

        public decimal DiscountPercent { get; set; }

        public decimal Amount { get; set; }
    }
}
