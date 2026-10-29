using System.Text.Json;
using System.Text.Json.Serialization;

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
        public decimal RefundedAmount { get; set; }
        public decimal ForfeitedAmount { get; set; }
        public int? BankAccountId { get; set; }
        public string? BankAccountName { get; set; }
        public string PlaceOfSupply { get; set; } = "Maharashtra";
        public string ReconciliationStatus { get; set; } = "Unreconciled";
        public string? RefundReferenceNumber { get; set; }
        public string? RefundProcessedBy { get; set; }
        public string? RefundProcessedOn { get; set; }
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
        public uint ConcurrencyVersion { get; set; }
        public List<AdvancePaymentApplicationDto> Applications { get; set; } = new();
        public List<AdvancePaymentAdjustmentDto> Adjustments { get; set; } = new();
        public List<AdvancePaymentTimelineDto> Timeline { get; set; } = new();
        public List<ReceiptVoucherDto> ReceiptVouchers { get; set; } = new();
        public List<RefundVoucherDto> RefundVouchers { get; set; } = new();
    }

    public class AdvancePaymentApplicationDto
    {
        public int Id { get; set; }
        public int SalesOrderId { get; set; }
        public string SalesOrderNumber { get; set; } = string.Empty;
        public decimal ApplyAmount { get; set; }
        public decimal ExchangeRateAtAllocation { get; set; } = 1.0000m;
        public decimal RealizedFxGainLoss { get; set; } = 0.00m;
        public bool IsReversal { get; set; }
        public string? ReversalReason { get; set; }
        public int? OriginalApplicationId { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public string AppliedBy { get; set; } = string.Empty;
        public string AppliedOn { get; set; } = string.Empty;
    }

    public class ReceiptVoucherDto
    {
        public int Id { get; set; }
        public string VoucherNumber { get; set; } = string.Empty;
        public int AdvancePaymentId { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string PlaceOfSupply { get; set; } = string.Empty;
        public bool IsInterState { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal CgstRate { get; set; }
        public decimal CgstAmount { get; set; }
        public decimal SgstRate { get; set; }
        public decimal SgstAmount { get; set; }
        public decimal IgstRate { get; set; }
        public decimal IgstAmount { get; set; }
        public decimal TotalVoucherAmount { get; set; }
        public string VoucherDate { get; set; } = string.Empty;
        public string CreatedDate { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class RefundVoucherDto
    {
        public int Id { get; set; }
        public string RefundVoucherNumber { get; set; } = string.Empty;
        public int AdvancePaymentId { get; set; }
        public int ReceiptVoucherId { get; set; }
        public decimal RefundAmount { get; set; }
        public decimal TaxRefundedAmount { get; set; }
        public string RefundVoucherDate { get; set; } = string.Empty;
        public string BankReferenceNumber { get; set; } = string.Empty;
        public string CreatedDate { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class BankAccountLookupDto
    {
        public int Id { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public string IfscCode { get; set; } = string.Empty;
        public string AccountType { get; set; } = "Current";
        public string Currency { get; set; } = "INR";
        public bool IsActive { get; set; } = true;
    }

    public class AdvancePaymentReceiveRequestDto : AdvancePaymentRemarksRequestDto
    {
        public string? PlaceOfSupply { get; set; }
        public bool? IsInterState { get; set; }
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
        public string ReconciliationStatus { get; set; } = "Unreconciled";
        public string ReferenceNumber { get; set; } = string.Empty;
    }

    public class FlexibleStringJsonConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.TryGetInt64(out var l) ? l.ToString() : reader.GetDecimal().ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString() ?? string.Empty;
            }
            if (reader.TokenType == JsonTokenType.Null)
            {
                return string.Empty;
            }
            using var doc = JsonDocument.ParseValue(ref reader);
            return doc.RootElement.GetString() ?? string.Empty;
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }

    public class AdvancePaymentCreateRequestDto
    {
        public string? PaymentNumber { get; set; }
        [JsonConverter(typeof(FlexibleStringJsonConverter))]
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
        public int? BankAccountId { get; set; }
        public string? PlaceOfSupply { get; set; }
        public string? Remarks { get; set; }
        public string? AttachmentName { get; set; }
        public string? Status { get; set; }
    }

    public class AdvancePaymentUpdateRequestDto
    {
        public string? PaymentNumber { get; set; }
        [JsonConverter(typeof(FlexibleStringJsonConverter))]
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
        public int? BankAccountId { get; set; }
        public string? PlaceOfSupply { get; set; }
        public string? Remarks { get; set; }
        public string? AttachmentName { get; set; }
    }

    public class AdvancePaymentApplyLineDto
    {
        public int SalesOrderId { get; set; }
        public string? SalesOrderNumber { get; set; }
        public decimal ApplyAmount { get; set; }
        public decimal? OrderValue { get; set; }
        public decimal? OutstandingBalance { get; set; }
        public string? Remarks { get; set; }
    }

    public class AdvancePaymentApplyRequestDto
    {
        public int AdvancePaymentId { get; set; }
        public int SalesOrderId { get; set; }
        public string? SalesOrderNumber { get; set; }
        public decimal ApplyAmount { get; set; }
        public decimal? ExchangeRateAtAllocation { get; set; }
        public string? Remarks { get; set; }
        public List<AdvancePaymentApplyLineDto>? Lines { get; set; }
    }

    public class AdvancePaymentAvailableSalesOrderDto
    {
        public int Id { get; set; }
        public string SalesOrderNumber { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string OrderDate { get; set; } = string.Empty;
        public decimal OrderValue { get; set; }
        public decimal AlreadyPaid { get; set; }
        public decimal OutstandingBalance { get; set; }
        public string Status { get; set; } = string.Empty;
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
        public string ReconciliationStatus { get; set; } = "Unreconciled";
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

    public class ReverseAllocationRequestDto
    {
        public int ApplicationId { get; set; }
        public int OriginalApplicationId
        {
            get => ApplicationId;
            set => ApplicationId = value > 0 ? value : ApplicationId;
        }
        public decimal ReversalAmount { get; set; } // Must be > 0 and <= Original Application Amount
        public string Reason { get; set; } = string.Empty; // Mandatory
    }

    public class ProcessRefundRequestDto
    {
        public decimal RefundAmount { get; set; } // Must be <= RemainingAmount
        public string RefundReferenceNumber { get; set; } = string.Empty; // UTR or Cheque
        public string Remarks { get; set; } = string.Empty;
    }

    public class ProcessForfeitureRequestDto
    {
        public decimal ForfeitureAmount { get; set; } // Must be <= RemainingAmount
        public string Reason { get; set; } = string.Empty;
    }

    public class AdvancePaymentClearRequestDto
    {
        public string? BankStatementReference { get; set; }
        public string? ClearanceDate { get; set; }
        public string? Remarks { get; set; }
    }

    public class AdvancePaymentDishonorRequestDto
    {
        public string DishonorReason { get; set; } = string.Empty;
        public string? DishonorDate { get; set; }
        public decimal? BounceCharges { get; set; }
    }

    public class AdvancePaymentAdjustmentRequestDto
    {
        public int AdvancePaymentId { get; set; }
        public int? SalesInvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public decimal AdjustedAmount { get; set; }
        public string AdjustmentDate { get; set; } = string.Empty;
        public decimal ExchangeRateAtAdjustment { get; set; } = 1.0000m;
        public string Remarks { get; set; } = string.Empty;
    }

    public class AdvancePaymentAdjustmentDto
    {
        public int Id { get; set; }
        public int AdvancePaymentId { get; set; }
        public int? SalesInvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public decimal AdjustedAmount { get; set; }
        public string AdjustmentDate { get; set; } = string.Empty;
        public decimal ExchangeRateAtAdjustment { get; set; }
        public decimal RealizedFxGainLoss { get; set; }
        public string? Gstr1VoucherRef { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public string CreatedDate { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class AdvancePaymentAgingReportDto
    {
        public string GeneratedOn { get; set; } = string.Empty;
        public decimal TotalUnadjustedFloat { get; set; }
        public decimal Bucket0To15Days { get; set; }
        public decimal Bucket16To30Days { get; set; }
        public decimal Bucket31To60Days { get; set; }
        public decimal Bucket60PlusDays { get; set; }
        public List<AdvancePaymentAgingItemDto> Items { get; set; } = new();
    }

    public class AdvancePaymentAgingItemDto
    {
        public int PaymentId { get; set; }
        public string PaymentNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string PaymentDate { get; set; } = string.Empty;
        public decimal TotalAdvance { get; set; }
        public decimal RemainingAmount { get; set; }
        public int AgeDays { get; set; }
        public string AgingBucket { get; set; } = string.Empty;
    }

    public class Gstr1Table11ReportDto
    {
        public string MonthYear { get; set; } = string.Empty;
        public decimal GrossAdvanceReceived { get; set; }
        public decimal TotalCgst { get; set; }
        public decimal TotalSgst { get; set; }
        public decimal TotalIgst { get; set; }
        public decimal TotalTax { get; set; }
        public decimal AdjustedInInvoices { get; set; }
        public decimal NetTaxPayableOnAdvances { get; set; }
        public List<Gstr1VoucherItemDto> Vouchers { get; set; } = new();
    }

    public class Gstr1VoucherItemDto
    {
        public int VoucherId { get; set; }
        public string VoucherNumber { get; set; } = string.Empty;
        public string PaymentNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string PlaceOfSupply { get; set; } = string.Empty;
        public string PlaceOfSupplyStateCode { get; set; } = string.Empty;
        public bool IsInterState { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal CgstAmount { get; set; }
        public decimal SgstAmount { get; set; }
        public decimal IgstAmount { get; set; }
        public decimal TotalTax { get; set; }
        public bool IsAdjusted { get; set; }
        public string VoucherDate { get; set; } = string.Empty;
    }

    public class AdvancePaymentDashboardKpiDto
    {
        public decimal TotalFloat { get; set; }
        public decimal UnallocatedFloat { get; set; }
        public decimal AllocatedFloat { get; set; }
        public decimal ClearedThisMonth { get; set; }
        public int PendingClearanceCount { get; set; }
        public int ProductionReleasedSoCount { get; set; }
        public int DispatchReleasedSoCount { get; set; }
        public int DishonoredCount { get; set; }
        public List<AdvancePaymentListItemDto> RecentActivity { get; set; } = new();
    }
}
