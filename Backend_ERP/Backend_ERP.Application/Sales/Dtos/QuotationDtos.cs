namespace ERP.Application.Sales.Dtos
{
    public class QuotationDto
    {
        public int Id { get; set; }
        public string QuotationNumber { get; set; } = string.Empty;
        public int RevisionNumber { get; set; } = 1;
        public bool IsCurrentRevision { get; set; } = true;
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public string BillingAddress { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string SalesPerson { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string QuotationDate { get; set; } = string.Empty;
        public string ValidUntil { get; set; } = string.Empty;
        public string Currency { get; set; } = "INR";
        public decimal ExchangeRate { get; set; } = 1.0m;
        public decimal Subtotal { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal TaxTotal { get; set; }
        public decimal FreightAmount { get; set; }
        public decimal PackagingAmount { get; set; }
        public decimal RoundOff { get; set; }
        public decimal GrandTotal { get; set; }
        public string PaymentTerms { get; set; } = string.Empty;
        public string DeliveryTerms { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string? ClientPoNumber { get; set; }
        public string? ClientAcceptedAt { get; set; }
        public string? ClientPoAttachmentUrl { get; set; }
        public int? ConvertedSalesOrderId { get; set; }
        public string? ConvertedSalesOrderNumber { get; set; }
        public string? ConvertedOn { get; set; }
        public int? DiscountApprovalId { get; set; }
        public string DiscountApprovalStatus { get; set; } = "None";
        public string CreatedBy { get; set; } = string.Empty;
        public DateTimeOffset CreatedDate { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
        public DateTimeOffset UpdatedDate { get; set; }
        public List<QuotationItemDto> Items { get; set; } = new();
    }

    public class QuotationItemDto
    {
        public int Id { get; set; }
        public int? ProductId { get; set; }
        public int LineNumber { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "NOS";
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxPercent { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal LineTotal { get; set; }
        public int SortOrder { get; set; }
    }

    public class QuotationCreateRequestDto
    {
        public string? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public string? BillingAddress { get; set; }
        public string? ShippingAddress { get; set; }
        public string? SalesPerson { get; set; }
        public DateOnly? QuotationDate { get; set; }
        public DateOnly? ValidUntil { get; set; }
        public string? Currency { get; set; }
        public decimal? ExchangeRate { get; set; }
        public decimal? FreightAmount { get; set; }
        public decimal? PackagingAmount { get; set; }
        public decimal? RoundOff { get; set; }
        public string? PaymentTerms { get; set; }
        public string? DeliveryTerms { get; set; }
        public string? Notes { get; set; }
        public List<QuotationItemUpsertDto> Items { get; set; } = new();
    }

    public class QuotationUpdateRequestDto : QuotationCreateRequestDto
    {
    }

    public class QuotationItemUpsertDto
    {
        public int? ProductId { get; set; }
        public string? ItemCode { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Quantity { get; set; } = 1;
        public string? Unit { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal TaxPercent { get; set; } = 18;
        public int SortOrder { get; set; } = 1;
    }

    public class QuotationClientAcceptRequestDto
    {
        public string ClientPoNumber { get; set; } = string.Empty;
        public DateTimeOffset? ClientAcceptedAt { get; set; }
        public string? ClientPoAttachmentUrl { get; set; }
        public string? Remarks { get; set; }
    }

    public class QuotationReviseRequestDto
    {
        public string? Reason { get; set; }
    }

    public class QuotationListQueryDto
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? Customer { get; set; }
        public DateOnly? DateFrom { get; set; }
        public DateOnly? DateTo { get; set; }
        public bool CurrentOnly { get; set; } = true;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }
}
