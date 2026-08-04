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

        public int SortOrder { get; set; }
    }
}
