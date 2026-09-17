namespace ERP.Domain.Sales
{
    public class ProformaInvoiceItem
    {
        public int Id { get; set; }

        public int ProformaInvoiceId { get; set; }

        public ProformaInvoice ProformaInvoice { get; set; } = null!;

        public string LineKey { get; set; } = string.Empty;

        public string ItemName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public string Unit { get; set; } = "Nos";

        public decimal Rate { get; set; }

        public decimal Discount { get; set; }

        public decimal Gst { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal Amount { get; set; }

        /// <summary>How much of this line item has already been converted to a Sales Invoice.</summary>
        public decimal ConvertedQuantity { get; set; } = 0m;

        public decimal RemainingQuantity => Math.Max(0m, Quantity - ConvertedQuantity);

        public int SortOrder { get; set; }
    }
}
