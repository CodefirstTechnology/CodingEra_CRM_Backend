namespace ERP.Domain.Sales
{
    public class ProformaInvoice
    {
        public int Id { get; set; }

        public string PiNumber { get; set; } = string.Empty;

        public DateOnly InvoiceDate { get; set; }

        public DateOnly ValidUntil { get; set; }

        public string CustomerId { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string ContactPerson { get; set; } = string.Empty;

        public string BillingAddress { get; set; } = string.Empty;

        public string ShippingAddress { get; set; } = string.Empty;

        public int SalesPersonUserId { get; set; }

        public string SalesPerson { get; set; } = string.Empty;

        public string Currency { get; set; } = "INR";

        public decimal ExchangeRate { get; set; } = 1m;

        public int? QuotationId { get; set; }

        public string QuotationNumber { get; set; } = string.Empty;

        public int? SalesOrderId { get; set; }

        public SalesOrder? SalesOrder { get; set; }

        public string SalesOrderNumber { get; set; } = string.Empty;

        public string PaymentTerms { get; set; } = string.Empty;

        public string DeliveryTerms { get; set; } = string.Empty;

        public string CustomerNotes { get; set; } = string.Empty;

        public string InternalNotes { get; set; } = string.Empty;

        public decimal Subtotal { get; set; }

        public decimal DiscountTotal { get; set; }

        public decimal TaxTotal { get; set; }

        public decimal GrandTotal { get; set; }

        public string Status { get; set; } = ProformaInvoiceStatuses.Draft;

        public string Remarks { get; set; } = string.Empty;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTimeOffset CreatedDate { get; set; }

        public string UpdatedBy { get; set; } = string.Empty;

        public DateTimeOffset UpdatedDate { get; set; }

        public bool IsDeleted { get; set; }

        public string? ConvertedInvoiceNumber { get; set; }

        public DateTimeOffset? ConvertedOn { get; set; }

        public ICollection<ProformaInvoiceItem> Items { get; set; } = new List<ProformaInvoiceItem>();

        public ICollection<ProformaInvoiceStatusHistory> StatusHistory { get; set; } = new List<ProformaInvoiceStatusHistory>();

        public ICollection<ProformaInvoiceApprovalHistory> ApprovalHistory { get; set; } = new List<ProformaInvoiceApprovalHistory>();
    }
}
