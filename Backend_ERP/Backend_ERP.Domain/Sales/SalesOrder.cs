namespace ERP.Domain.Sales
{
    public class SalesOrder
    {
        public int Id { get; set; }

        public string SalesOrderNumber { get; set; } = string.Empty;

        public int? QuotationId { get; set; }

        public string QuotationNumber { get; set; } = string.Empty;

        public string SourceType { get; set; } = SalesOrderSourceTypes.Manual;

        public string CustomerName { get; set; } = string.Empty;

        public string ContactPerson { get; set; } = string.Empty;

        public string BillingAddress { get; set; } = string.Empty;

        public string ShippingAddress { get; set; } = string.Empty;

        public string? CustomerEmail { get; set; }

        public string? CustomerPhone { get; set; }

        public string SalesPerson { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public DateOnly OrderDate { get; set; }

        public DateOnly? ExpectedDeliveryDate { get; set; }

        public string PaymentTerms { get; set; } = string.Empty;

        public string DeliveryTerms { get; set; } = string.Empty;

        public decimal Subtotal { get; set; }

        public decimal DiscountTotal { get; set; }

        public decimal GstTotal { get; set; }

        public decimal GrandTotal { get; set; }

        public string Status { get; set; } = SalesOrderStatuses.Draft;

        public string Remarks { get; set; } = string.Empty;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTimeOffset CreatedDate { get; set; }

        public string UpdatedBy { get; set; } = string.Empty;

        public DateTimeOffset UpdatedDate { get; set; }

        public DateTimeOffset? PdfGeneratedDate { get; set; }

        public DateTimeOffset? LastCommunicationDate { get; set; }

        public ICollection<SalesOrderItem> Items { get; set; } = new List<SalesOrderItem>();

        public ICollection<SalesOrderStatusHistory> StatusHistory { get; set; } = new List<SalesOrderStatusHistory>();

        public ICollection<SalesOrderEmailHistory> EmailHistory { get; set; } = new List<SalesOrderEmailHistory>();
    }
}
