namespace ERP.Application.Sales.Dtos
{
    public class SalesOrderCustomerDto
    {
        public string CustomerName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string BillingAddress { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
    }

    public class SalesOrderItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "Nos";
        public decimal Rate { get; set; }
        public decimal Discount { get; set; }
        public decimal Gst { get; set; }
        public decimal Amount { get; set; }
    }

    public class SalesOrderStatusHistoryDto
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string? Label { get; set; }
    }

    public class SalesOrderEmailHistoryDto
    {
        public string Id { get; set; } = string.Empty;
        public int SalesOrderId { get; set; }
        public string Recipient { get; set; } = string.Empty;
        public string SentDate { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class SalesOrderDto
    {
        public int Id { get; set; }
        public string SalesOrderNumber { get; set; } = string.Empty;
        public int? QuotationId { get; set; }
        public string QuotationNumber { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public SalesOrderCustomerDto Customer { get; set; } = new();
        public string SalesPerson { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string OrderDate { get; set; } = string.Empty;
        public string? ExpectedDeliveryDate { get; set; }
        public string PaymentTerms { get; set; } = string.Empty;
        public string DeliveryTerms { get; set; } = string.Empty;
        public List<SalesOrderItemDto> Items { get; set; } = new();
        public decimal Amount { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal GstTotal { get; set; }
        public decimal GrandTotal { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public List<SalesOrderStatusHistoryDto> StatusHistory { get; set; } = new();
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedDate { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public string UpdatedDate { get; set; } = string.Empty;
        public string? PdfGeneratedDate { get; set; }
        public string? LastCommunicationDate { get; set; }
        public List<SalesOrderEmailHistoryDto>? EmailHistory { get; set; }
        public int? AuditCount { get; set; }
        public string? LastModified { get; set; }
    }

    public class SalesOrderListItemDto
    {
        public int Id { get; set; }
        public string SalesOrderNumber { get; set; } = string.Empty;
        public string QuotationNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string OrderDate { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class SalesOrderCreateRequestDto
    {
        public string? SalesOrderNumber { get; set; }
        public int? QuotationId { get; set; }
        public string? QuotationNumber { get; set; }
        public string? SourceType { get; set; }
        public SalesOrderCustomerDto Customer { get; set; } = new();
        public string? SalesPerson { get; set; }
        public string? Notes { get; set; }
        public string OrderDate { get; set; } = string.Empty;
        public string? ExpectedDeliveryDate { get; set; }
        public string PaymentTerms { get; set; } = string.Empty;
        public string DeliveryTerms { get; set; } = string.Empty;
        public List<SalesOrderItemDto> Items { get; set; } = new();
        public string? Status { get; set; }
    }

    public class SalesOrderUpdateRequestDto
    {
        public string? SalesOrderNumber { get; set; }
        public int? QuotationId { get; set; }
        public string? QuotationNumber { get; set; }
        public string? SourceType { get; set; }
        public SalesOrderCustomerDto Customer { get; set; } = new();
        public string? SalesPerson { get; set; }
        public string? Notes { get; set; }
        public string OrderDate { get; set; } = string.Empty;
        public string? ExpectedDeliveryDate { get; set; }
        public string PaymentTerms { get; set; } = string.Empty;
        public string DeliveryTerms { get; set; } = string.Empty;
        public List<SalesOrderItemDto> Items { get; set; } = new();
    }

    public class SalesOrderStatusUpdateRequestDto
    {
        public string Status { get; set; } = string.Empty;
        public string? Remarks { get; set; }
    }

    public class SalesOrderCancelRequestDto
    {
        public string? Remarks { get; set; }
    }

    public class SalesOrderListQueryDto
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? DateFrom { get; set; }
        public string? DateTo { get; set; }
    }
}
