namespace ERP.Domain.Sales
{
    public class QuotationItem
    {
        public int Id { get; set; }

        public int QuotationId { get; set; }

        public Quotation? Quotation { get; set; }

        public int? ProductId { get; set; }

        public int LineNumber { get; set; } = 1;

        public string ItemCode { get; set; } = string.Empty;

        public string ItemName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Quantity { get; set; } = 1m;

        public string Unit { get; set; } = "NOS";

        public decimal UnitPrice { get; set; }

        public decimal DiscountPercent { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal TaxPercent { get; set; } = 18m;

        public decimal TaxAmount { get; set; }

        public decimal LineTotal { get; set; }

        public int SortOrder { get; set; } = 1;
    }
}
