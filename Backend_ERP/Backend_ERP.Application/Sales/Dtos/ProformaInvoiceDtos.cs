namespace ERP.Application.Sales.Dtos
{
    public class ProformaInvoiceCustomerDto
    {
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerGstin { get; set; } = string.Empty;
        public string CustomerCategory { get; set; } = "Standard";
        public string PlaceOfSupplyStateCode { get; set; } = "27";
        public string ContactPerson { get; set; } = string.Empty;
        public string BillingAddress { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
    }

    public class ProformaInvoiceItemDto
    {
        public string Id { get; set; } = string.Empty;
        public int? SalesOrderItemId { get; set; }
        public int? QuotationItemId { get; set; }
        public string LineKey { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string HsnCode { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "Nos";
        public decimal Rate { get; set; }
        public decimal Discount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Gst { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Amount { get; set; }

        // Frozen Snapshots
        public decimal OriginalSellingRate { get; set; }
        public decimal FloorPriceSnapshot { get; set; }
        public decimal MaxDiscountPctSnapshot { get; set; }
        public decimal TaxSlabPercentSnapshot { get; set; }
        public string PlaceOfSupplyStateCode { get; set; } = "27";

        /// <summary>How much of this line has already been converted to a Sales Invoice.</summary>
        public decimal ConvertedQuantity { get; set; } = 0m;
        public decimal RemainingQuantity { get; set; }
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
        public string? ApproverUserId { get; set; }
        public string? ApprovalToken { get; set; }
        public string? IpAddress { get; set; }
        public string PerformedBy { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
    }

    public class ProformaInvoiceSettlementAllocationDto
    {
        public int Id { get; set; }
        public int ProformaInvoiceId { get; set; }
        public int AdvancePaymentId { get; set; }
        public decimal AllocatedAmount { get; set; }
        public decimal ExchangeRateAtSettlement { get; set; }
        public decimal BaseCurrencyAllocatedAmount { get; set; }
        public string ReceiptVoucherNumber { get; set; } = string.Empty;
        public string AllocatedOn { get; set; } = string.Empty;
        public string AllocatedBy { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public bool IsReversed { get; set; }
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
        public string? ExchangeRateDate { get; set; }
        public decimal BaseCurrencyEquivalentAmount { get; set; }
        public string Incoterm { get; set; } = "EXW";
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
        public decimal AdvanceReceivedAmount { get; set; }
        public string PaymentStatus { get; set; } = "Unpaid";
        public string BillingType { get; set; } = "Full";
        public decimal? MilestonePercentage { get; set; }
        public bool RequiresFinanceCreditReview { get; set; }
        public string? CreditReviewReason { get; set; }
        public string? ApprovalToken { get; set; }
        public string? ApprovedBy { get; set; }
        public string? ApprovedDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public List<ProformaInvoiceStatusHistoryDto> StatusHistory { get; set; } = new();
        public List<ProformaInvoiceApprovalHistoryDto> ApprovalHistory { get; set; } = new();
        public List<ProformaInvoiceSettlementAllocationDto> SettlementAllocations { get; set; } = new();
        public ProformaConversionDto? Conversion { get; set; }

        // Two-Tier Release Gates
        public bool IsProductionReleased { get; set; }
        public string? ProductionReleasedOn { get; set; }
        public string? ProductionReleasedBy { get; set; }
        public bool IsDispatchReleased { get; set; }
        public string? DispatchReleasedOn { get; set; }
        public string? DispatchReleasedBy { get; set; }

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
        public decimal AdvanceReceivedAmount { get; set; }
        public string PaymentStatus { get; set; } = "Unpaid";
        public string BillingType { get; set; } = "Full";
        public decimal? MilestonePercentage { get; set; }
        public bool RequiresFinanceCreditReview { get; set; }
        public bool IsProductionReleased { get; set; }
        public bool IsDispatchReleased { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PlaceOfSupplyStateCode { get; set; } = "27";
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
        public string? Incoterm { get; set; }
        public int? QuotationId { get; set; }
        public string? QuotationNumber { get; set; }
        public int? SalesOrderId { get; set; }
        public string? SalesOrderNumber { get; set; }
        public string PaymentTerms { get; set; } = string.Empty;
        public string DeliveryTerms { get; set; } = string.Empty;
        public string? CustomerNotes { get; set; }
        public string? InternalNotes { get; set; }
        public string BillingType { get; set; } = "Full";
        public decimal? MilestonePercentage { get; set; }
        public List<ProformaInvoiceItemDto> Items { get; set; } = new();
        public string? Status { get; set; }
    }

    public class CreateProformaInvoiceFromSoDto
    {
        public int SalesOrderId { get; set; }
        public string BillingType { get; set; } = "Full";
        public decimal? MilestonePercentage { get; set; }
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
        public string? Incoterm { get; set; }
        public int? QuotationId { get; set; }
        public string? QuotationNumber { get; set; }
        public int? SalesOrderId { get; set; }
        public string? SalesOrderNumber { get; set; }
        public string PaymentTerms { get; set; } = string.Empty;
        public string DeliveryTerms { get; set; } = string.Empty;
        public string? CustomerNotes { get; set; }
        public string? InternalNotes { get; set; }
        public string BillingType { get; set; } = "Full";
        public decimal? MilestonePercentage { get; set; }
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

    public class ProformaSettlementRequestDto
    {
        public int AdvancePaymentId { get; set; }
        public decimal AllocatedAmount { get; set; }
        public decimal ExchangeRate { get; set; } = 1.0000m;
        public string? ReceiptVoucherNumber { get; set; }
        public string? Remarks { get; set; }
    }

    public class ProformaSettlementResultDto
    {
        public int ProformaInvoiceId { get; set; }
        public string PiNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal AdvanceReceivedAmount { get; set; }
        public decimal RemainingUnsettledAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public bool IsProductionReleased { get; set; }
        public bool IsDispatchReleased { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ProformaInvoiceConvertRequestDto
    {
        public string? Remarks { get; set; }
        public string TargetType { get; set; } = "TaxInvoice";
        public List<ProformaInvoiceConversionItemDto>? Items { get; set; }
    }

    public record ProformaInvoiceConversionItemDto(
        int ItemId,
        decimal ConvertQuantity
    );

    public record ConvertProformaInvoiceRequestDto(
        string TargetType,
        List<ProformaInvoiceConversionItemDto>? Items
    );

    public record ProformaInvoiceUpiDetailsDto(
        string UpiIntentString,
        string PayeeVpa,
        string PayeeName,
        decimal NetPayableAmount,
        string ReferenceNumber
    );

    public record ConversionResultDto(
        int Id,
        string Status,
        string ConvertedInvoiceNumber
    );

    public class GenerateProformaInvoiceFromSoRequestDto
    {
        public string BillingType { get; set; } = "Full";
        public decimal? MilestonePercentage { get; set; }
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

    public class ProformaAgingBucketsDto
    {
        public decimal Days0To7 { get; set; }
        public decimal Days8To15 { get; set; }
        public decimal Days16To30 { get; set; }
        public decimal DaysOver30 { get; set; }
        public int Count0To7 { get; set; }
        public int Count8To15 { get; set; }
        public int Count16To30 { get; set; }
        public int CountOver30 { get; set; }
    }

    public class ProformaStatusDistributionDto
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class ProformaDashboardTelemetryDto
    {
        public decimal TotalIssuedValue { get; set; }
        public int TotalIssuedCount { get; set; }
        public decimal PendingAdvanceValue { get; set; }
        public int PendingAdvanceCount { get; set; }
        public decimal ConversionRatePercentage { get; set; }
        public decimal AverageAgingDays { get; set; }
        public ProformaAgingBucketsDto Aging { get; set; } = new();
        public List<ProformaStatusDistributionDto> StatusDistribution { get; set; } = new();
        public List<ProformaInvoiceListItemDto> RecentInvoices { get; set; } = new();
    }

    public class ProformaTaxVarianceReportDto
    {
        public int ProformaInvoiceId { get; set; }
        public string PiNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? SalesOrderNumber { get; set; }
        public string? ConvertedInvoiceNumber { get; set; }
        public decimal ProformaGrandTotal { get; set; }
        public decimal ActualInvoicedTotal { get; set; }
        public decimal VarianceAmount { get; set; }
        public decimal VariancePercentage { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class ProformaStateTaxProjectionReportDto
    {
        public string StateCode { get; set; } = string.Empty;
        public string StateName { get; set; } = string.Empty;
        public int ProformaCount { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal ProjectedCgst { get; set; }
        public decimal ProjectedSgst { get; set; }
        public decimal ProjectedIgst { get; set; }
        public decimal TotalProjectedTax { get; set; }
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

    public class ProformaApprovalMetricsDto
    {
        public int PendingApprovals { get; set; }
        public int ApprovedToday { get; set; }
        public int RejectedTotal { get; set; }
        public decimal TotalPendingValue { get; set; }
        public double AverageTurnaroundHours { get; set; }
    }

    public class ProformaApprovalQueueRowDto
    {
        public int Id { get; set; }
        public string PiNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string CurrentLevel { get; set; } = string.Empty;
        public string CurrentLevelName { get; set; } = string.Empty;
        public string NextLevel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string SubmittedBy { get; set; } = string.Empty;
        public string SubmittedAt { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public int DaysPending { get; set; }
        public string? QuotationNumber { get; set; }
        public string? SalesOrderNumber { get; set; }
    }

    public class ProformaApprovalDashboardDto
    {
        public ProformaApprovalMetricsDto Metrics { get; set; } = new();
        public List<ProformaApprovalQueueRowDto> Rows { get; set; } = new();
    }
}
