namespace ERP.Application.Sales.Dtos
{
    public class ProformaInvoiceCustomerDto
    {
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string BillingAddress { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
    }

    public class ProformaInvoiceItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "Nos";
        public decimal Rate { get; set; }
        public decimal Discount { get; set; }
        public decimal Gst { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Amount { get; set; }
    }

    public class ProformaInvoiceStatusHistoryDto
    {
        public string Id { get; set; } = string.Empty;
        public string? OldStatus { get; set; }
        public string NewStatus { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
    }

    public class ProformaInvoiceApprovalHistoryDto
    {
        public string Id { get; set; } = string.Empty;
        public string ApprovalLevel { get; set; } = string.Empty;
        public string Decision { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string ApprovedBy { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
    }

    public class ProformaConversionDto
    {
        public string? SalesInvoiceNumber { get; set; }
        public string? ConvertedOn { get; set; }
        public string? Remarks { get; set; }
    }

    public class ProformaInvoiceDto
    {
        public int Id { get; set; }
        public string PiNumber { get; set; } = string.Empty;
        public string InvoiceDate { get; set; } = string.Empty;
        public string ValidUntil { get; set; } = string.Empty;
        public ProformaInvoiceCustomerDto Customer { get; set; } = new();
        public string SalesPersonId { get; set; } = string.Empty;
        public string SalesPerson { get; set; } = string.Empty;
        public string Currency { get; set; } = "INR";
        public decimal ExchangeRate { get; set; } = 1m;
        public int? QuotationId { get; set; }
        public string QuotationNumber { get; set; } = string.Empty;
        public int? SalesOrderId { get; set; }
        public string SalesOrderNumber { get; set; } = string.Empty;
        public string PaymentTerms { get; set; } = string.Empty;
        public string DeliveryTerms { get; set; } = string.Empty;
        public string CustomerNotes { get; set; } = string.Empty;
        public string InternalNotes { get; set; } = string.Empty;
        public List<ProformaInvoiceItemDto> Items { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal TaxTotal { get; set; }
        public decimal GrandTotal { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public List<ProformaInvoiceStatusHistoryDto> StatusHistory { get; set; } = new();
        public List<ProformaInvoiceApprovalHistoryDto> ApprovalHistory { get; set; } = new();
        public ProformaConversionDto? Conversion { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedDate { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public string UpdatedDate { get; set; } = string.Empty;
    }

    public class ProformaInvoiceListItemDto
    {
        public int Id { get; set; }
        public string PiNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string QuotationNumber { get; set; } = string.Empty;
        public string SalesOrderNumber { get; set; } = string.Empty;
        public string InvoiceDate { get; set; } = string.Empty;
        public string ValidUntil { get; set; } = string.Empty;
        public string SalesPerson { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public decimal GrandTotal { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class ProformaInvoiceCreateRequestDto
    {
        public string? PiNumber { get; set; }
        public string InvoiceDate { get; set; } = string.Empty;
        public string ValidUntil { get; set; } = string.Empty;
        public ProformaInvoiceCustomerDto Customer { get; set; } = new();
        public string? SalesPersonId { get; set; }
        public string? SalesPerson { get; set; }
        public string Currency { get; set; } = "INR";
        public decimal? ExchangeRate { get; set; }
        public int? QuotationId { get; set; }
        public string? QuotationNumber { get; set; }
        public int? SalesOrderId { get; set; }
        public string? SalesOrderNumber { get; set; }
        public string PaymentTerms { get; set; } = string.Empty;
        public string DeliveryTerms { get; set; } = string.Empty;
        public string? CustomerNotes { get; set; }
        public string? InternalNotes { get; set; }
        public List<ProformaInvoiceItemDto> Items { get; set; } = new();
        public string? Status { get; set; }
    }

    public class ProformaInvoiceUpdateRequestDto
    {
        public string? PiNumber { get; set; }
        public string InvoiceDate { get; set; } = string.Empty;
        public string ValidUntil { get; set; } = string.Empty;
        public ProformaInvoiceCustomerDto Customer { get; set; } = new();
        public string? SalesPersonId { get; set; }
        public string? SalesPerson { get; set; }
        public string Currency { get; set; } = "INR";
        public decimal? ExchangeRate { get; set; }
        public int? QuotationId { get; set; }
        public string? QuotationNumber { get; set; }
        public int? SalesOrderId { get; set; }
        public string? SalesOrderNumber { get; set; }
        public string PaymentTerms { get; set; } = string.Empty;
        public string DeliveryTerms { get; set; } = string.Empty;
        public string? CustomerNotes { get; set; }
        public string? InternalNotes { get; set; }
        public List<ProformaInvoiceItemDto> Items { get; set; } = new();
    }

    public class ProformaInvoiceStatusUpdateRequestDto
    {
        public string Status { get; set; } = string.Empty;
        public string? Remarks { get; set; }
    }

    public class ProformaInvoiceApprovalRequestDto
    {
        public string Kind { get; set; } = string.Empty;
        public string? Decision { get; set; }
        public string? ApprovalLevel { get; set; }
        public string Remarks { get; set; } = string.Empty;
    }

    public class ProformaInvoiceConvertRequestDto
    {
        public string? Remarks { get; set; }
    }

    public class ProformaInvoiceListQueryDto
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? Customer { get; set; }
        public string? SalesPerson { get; set; }
        public string? DateFrom { get; set; }
        public string? DateTo { get; set; }
    }

    public class ProformaDashboardDto
    {
        public int TotalCount { get; set; }
        public int DraftCount { get; set; }
        public int PendingApprovalCount { get; set; }
        public int ApprovedCount { get; set; }
        public int SentCount { get; set; }
        public int ConvertedCount { get; set; }
        public decimal TotalValue { get; set; }
        public List<ProformaInvoiceListItemDto> Recent { get; set; } = new();
    }

    public class ProformaReportResultDto
    {
        public string GeneratedOn { get; set; } = string.Empty;
        public int RowCount { get; set; }
        public List<ProformaInvoiceListItemDto> Rows { get; set; } = new();
    }

    public class ProformaExportRequestDto
    {
        public string Format { get; set; } = "csv";
        public string? Status { get; set; }
        public string? DateFrom { get; set; }
        public string? DateTo { get; set; }
    }

    public class ProformaExportMetadataDto
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string GeneratedOn { get; set; } = string.Empty;
    }

    public class ProformaLookupCustomerDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
    }

    public class ProformaLookupSalesOrderDto
    {
        public int Id { get; set; }
        public string SalesOrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class ProformaLookupQuotationDto
    {
        public int Id { get; set; }
        public string QuotationNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
    }

    public class ProformaPdfResultDto
    {
        public string FileName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string GeneratedOn { get; set; } = string.Empty;
    }

    public class ProformaEmailResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
