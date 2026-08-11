namespace ERP.Application.Sales.Dtos
{
    public class AdvancePaymentDto
    {
        public int Id { get; set; }
        public string PaymentNumber { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public int? SalesOrderId { get; set; }
        public string SalesOrderNumber { get; set; } = string.Empty;
        public int? QuotationId { get; set; }
        public string QuotationNumber { get; set; } = string.Empty;
        public string PaymentDate { get; set; } = string.Empty;
        public string PaymentMode { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;
        public string Currency { get; set; } = "INR";
        public decimal ExchangeRate { get; set; } = 1m;
        public decimal AdvanceAmount { get; set; }
        public decimal AppliedAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string? AttachmentName { get; set; }
        public string? VerifiedBy { get; set; }
        public string? VerifiedOn { get; set; }
        public string? ReceivedBy { get; set; }
        public string? ReceivedOn { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedDate { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public string UpdatedDate { get; set; } = string.Empty;
        public List<AdvancePaymentApplicationDto> Applications { get; set; } = new();
        public List<AdvancePaymentTimelineDto> Timeline { get; set; } = new();
    }

    public class AdvancePaymentApplicationDto
    {
        public int Id { get; set; }
        public int SalesOrderId { get; set; }
        public string SalesOrderNumber { get; set; } = string.Empty;
        public decimal ApplyAmount { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public string AppliedBy { get; set; } = string.Empty;
        public string AppliedOn { get; set; } = string.Empty;
    }

    public class AdvancePaymentListItemDto
    {
        public int Id { get; set; }
        public string PaymentNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string SalesOrderNumber { get; set; } = string.Empty;
        public string QuotationNumber { get; set; } = string.Empty;
        public string PaymentDate { get; set; } = string.Empty;
        public string PaymentMode { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public decimal AdvanceAmount { get; set; }
        public decimal AppliedAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;
    }

    public class AdvancePaymentCreateRequestDto
    {
        public string? PaymentNumber { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public int? SalesOrderId { get; set; }
        public string? SalesOrderNumber { get; set; }
        public int? QuotationId { get; set; }
        public string? QuotationNumber { get; set; }
        public string PaymentDate { get; set; } = string.Empty;
        public string PaymentMode { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;
        public string Currency { get; set; } = "INR";
        public decimal? ExchangeRate { get; set; }
        public decimal AdvanceAmount { get; set; }
        public string? Remarks { get; set; }
        public string? AttachmentName { get; set; }
        public string? Status { get; set; }
    }

    public class AdvancePaymentUpdateRequestDto
    {
        public string? PaymentNumber { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public int? SalesOrderId { get; set; }
        public string? SalesOrderNumber { get; set; }
        public int? QuotationId { get; set; }
        public string? QuotationNumber { get; set; }
        public string PaymentDate { get; set; } = string.Empty;
        public string PaymentMode { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;
        public string Currency { get; set; } = "INR";
        public decimal? ExchangeRate { get; set; }
        public decimal AdvanceAmount { get; set; }
        public string? Remarks { get; set; }
        public string? AttachmentName { get; set; }
    }

    public class AdvancePaymentApplyRequestDto
    {
        public int AdvancePaymentId { get; set; }
        public int SalesOrderId { get; set; }
        public string? SalesOrderNumber { get; set; }
        public decimal ApplyAmount { get; set; }
        public string? Remarks { get; set; }
    }


    public class AdvancePaymentTimelineDto
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
        public string PerformedOn { get; set; } = string.Empty;
    }

    public class AdvancePaymentDashboardDto
    {
        public int TotalCount { get; set; }
        public int DraftCount { get; set; }
        public int SubmittedCount { get; set; }
        public int FinanceVerificationCount { get; set; }
        public int ReceivedCount { get; set; }
        public int PartiallyAppliedCount { get; set; }
        public int FullyAppliedCount { get; set; }
        public int CancelledCount { get; set; }
        public int RejectedCount { get; set; }
        public decimal TotalAdvanceAmount { get; set; }
        public decimal TotalAppliedAmount { get; set; }
        public decimal TotalRemainingAmount { get; set; }
        public List<AdvancePaymentListItemDto> Recent { get; set; } = new();
    }

    public class AdvancePaymentLedgerDto
    {
        public int Id { get; set; }
        public string PaymentNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string PaymentDate { get; set; } = string.Empty;
        public string PaymentMode { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public decimal AdvanceAmount { get; set; }
        public decimal AppliedAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;
        public string SalesOrderNumber { get; set; } = string.Empty;
        public string QuotationNumber { get; set; } = string.Empty;
    }

    public class AdvancePaymentReportDto
    {
        public string GeneratedOn { get; set; } = string.Empty;
        public int RowCount { get; set; }
        public decimal TotalAdvanceAmount { get; set; }
        public decimal TotalAppliedAmount { get; set; }
        public decimal TotalRemainingAmount { get; set; }
        public List<AdvancePaymentListItemDto> Rows { get; set; } = new();
    }

    public class AdvancePaymentListQueryDto
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? Customer { get; set; }
        public string? PaymentMode { get; set; }
        public string? DateFrom { get; set; }
        public string? DateTo { get; set; }
    }

    public class AdvancePaymentRemarksRequestDto
    {
        public string? Remarks { get; set; }
    }

    public class AdvancePaymentExportRequestDto
    {
        public string Format { get; set; } = "csv";
        public string? Status { get; set; }
        public string? DateFrom { get; set; }
        public string? DateTo { get; set; }
    }

    public class AdvancePaymentExportMetadataDto
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string GeneratedOn { get; set; } = string.Empty;
    }

    public class AdvancePaymentLookupCustomerDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class AdvancePaymentLookupSalesOrderDto
    {
        public int Id { get; set; }
        public string SalesOrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class AdvancePaymentLookupQuotationDto
    {
        public int Id { get; set; }
        public string QuotationNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
    }
}
