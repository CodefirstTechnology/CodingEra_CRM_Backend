using System;
using System.Collections.Generic;

namespace ERP.Application.Accounting.Dtos
{
    public class AccountingNoteDto
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class AccountingAttachmentDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int SizeKb { get; set; }
        public string UploadedBy { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
    }

    public class AccountingTimelineEventDto
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string? FromStatus { get; set; }
        public string? ToStatus { get; set; }
        public string? Remarks { get; set; }
    }

    public class ListQueryDto
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public int? CustomerId { get; set; }
        public int? VendorId { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
        public string? ReturnPeriod { get; set; }
        public string? PartyType { get; set; }
        public string? Branch { get; set; }
        public string? FinancialYear { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; }
        public string? SortDir { get; set; }
    }

    public class StatusActionRequestDto
    {
        public string? Remarks { get; set; }
        public string? Notes { get; set; }
    }

    // Customer Ledger
    public class CustomerLedgerEntryDto
    {
        public int Id { get; set; }
        public string LedgerNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal RunningBalance { get; set; }
        public decimal Outstanding { get; set; }
        public int? InvoiceId { get; set; }
        public string? InvoiceNumber { get; set; }
        public int? ReceiptId { get; set; }
        public string? ReceiptNumber { get; set; }
        public int? SalesOrderId { get; set; }
        public string? SalesOrderNumber { get; set; }
        public int? ProformaInvoiceId { get; set; }
        public string? ProformaInvoiceNumber { get; set; }
        public string EntryType { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public DateTime? DueDate { get; set; }
        public int AgeingDays { get; set; }
        public string AgeingBucket { get; set; } = "0-30";
        public string? Remarks { get; set; }
        public List<AccountingNoteDto> Notes { get; set; } = new();
        public List<AccountingAttachmentDto> Attachments { get; set; } = new();
        public List<AccountingTimelineEventDto> Timeline { get; set; } = new();
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }

    public class CustomerLedgerListItemDto
    {
        public int Id { get; set; }
        public string LedgerNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal RunningBalance { get; set; }
        public decimal Outstanding { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? ReceiptNumber { get; set; }
        public string? SalesOrderNumber { get; set; }
        public string? ProformaInvoiceNumber { get; set; }
        public string EntryType { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public DateTime? DueDate { get; set; }
        public int AgeingDays { get; set; }
        public string AgeingBucket { get; set; } = "0-30";
        public string? Remarks { get; set; }
    }

    public class CustomerLedgerDashboardDto
    {
        public int TotalCustomers { get; set; }
        public decimal Outstanding { get; set; }
        public decimal ReceivedToday { get; set; }
        public decimal Overdue { get; set; }
    }

    public class CustomerStatementSummaryDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string LedgerNumber { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal ClosingBalance { get; set; }
        public decimal Outstanding { get; set; }
        public decimal Overdue { get; set; }
    }

    // GST
    public class GstTransactionDto
    {
        public int Id { get; set; }
        public string GstNumber { get; set; } = string.Empty;
        public int? InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? VendorId { get; set; }
        public string? VendorName { get; set; }
        public string TxnType { get; set; } = string.Empty;
        public decimal TaxableValue { get; set; }
        public decimal Cgst { get; set; }
        public decimal Sgst { get; set; }
        public decimal Igst { get; set; }
        public decimal Cess { get; set; }
        public decimal TaxAmount { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string ReturnPeriod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public List<AccountingNoteDto> Notes { get; set; } = new();
        public List<AccountingAttachmentDto> Attachments { get; set; } = new();
        public List<AccountingTimelineEventDto> Timeline { get; set; } = new();
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }

    public class GstTransactionListItemDto
    {
        public int Id { get; set; }
        public string GstNumber { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string? VendorName { get; set; }
        public string TxnType { get; set; } = string.Empty;
        public decimal TaxableValue { get; set; }
        public decimal Cgst { get; set; }
        public decimal Sgst { get; set; }
        public decimal Igst { get; set; }
        public decimal Cess { get; set; }
        public decimal TaxAmount { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string ReturnPeriod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class GstReturnDto
    {
        public int Id { get; set; }
        public string ReturnPeriod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal GstCollected { get; set; }
        public decimal GstPaid { get; set; }
        public decimal NetPayable { get; set; }
        public int TransactionCount { get; set; }
        public DateTime? FiledAt { get; set; }
        public string? FiledBy { get; set; }
        public string? Remarks { get; set; }
        public List<AccountingTimelineEventDto> Timeline { get; set; } = new();
    }

    public class GstDashboardDto
    {
        public decimal GstCollected { get; set; }
        public decimal GstPaid { get; set; }
        public int PendingReturns { get; set; }
        public int FiledReturns { get; set; }
    }

    public class GstSummaryDto
    {
        public string ReturnPeriod { get; set; } = string.Empty;
        public decimal TaxableValue { get; set; }
        public decimal Cgst { get; set; }
        public decimal Sgst { get; set; }
        public decimal Igst { get; set; }
        public decimal Cess { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Collected { get; set; }
        public decimal Paid { get; set; }
        public decimal Net { get; set; }
    }

    // Payment Entry
    public class PaymentEntryDto
    {
        public int Id { get; set; }
        public string PaymentNumber { get; set; } = string.Empty;
        public int VendorId { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public int PurchaseBillId { get; set; }
        public string PurchaseBillNumber { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public string PaymentMode { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string BankAccount { get; set; } = string.Empty;
        public string? ChequeNumber { get; set; }
        public string? ReferenceNumber { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public decimal Tds { get; set; }
        public decimal NetAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public List<AccountingNoteDto> Notes { get; set; } = new();
        public List<AccountingAttachmentDto> Attachments { get; set; } = new();
        public List<AccountingTimelineEventDto> Timeline { get; set; } = new();
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }

    public class PaymentListItemDto
    {
        public int Id { get; set; }
        public string PaymentNumber { get; set; } = string.Empty;
        public int VendorId { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public string PurchaseBillNumber { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public string PaymentMode { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public decimal Tds { get; set; }
        public decimal NetAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class PaymentDashboardDto
    {
        public decimal TodaysPayments { get; set; }
        public int PendingApproval { get; set; }
        public int Posted { get; set; }
        public int Paid { get; set; }
    }

    public class PaymentCreateRequestDto
    {
        public int VendorId { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public int PurchaseBillId { get; set; }
        public string PurchaseBillNumber { get; set; } = string.Empty;
        public string PaymentDate { get; set; } = string.Empty;
        public string PaymentMode { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string BankAccount { get; set; } = string.Empty;
        public string? ChequeNumber { get; set; }
        public string? ReferenceNumber { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public decimal Tds { get; set; }
        public string? Remarks { get; set; }
        public string? Notes { get; set; }
    }

    public class PaymentUpdateRequestDto
    {
        public int? VendorId { get; set; }
        public string? VendorName { get; set; }
        public int? PurchaseBillId { get; set; }
        public string? PurchaseBillNumber { get; set; }
        public string? PaymentDate { get; set; }
        public string? PaymentMode { get; set; }
        public string? BankName { get; set; }
        public string? BankAccount { get; set; }
        public string? ChequeNumber { get; set; }
        public string? ReferenceNumber { get; set; }
        public decimal? Amount { get; set; }
        public string? Currency { get; set; }
        public decimal? Tds { get; set; }
        public string? Remarks { get; set; }
        public string? Notes { get; set; }
    }

    // Receipt Entry
    public class ReceiptEntryDto
    {
        public int Id { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int? SalesOrderId { get; set; }
        public string? SalesOrderNumber { get; set; }
        public DateTime ReceiptDate { get; set; }
        public string ReceiptMode { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string BankAccount { get; set; } = string.Empty;
        public string? ReferenceNumber { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public decimal Tds { get; set; }
        public decimal NetAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public List<AccountingNoteDto> Notes { get; set; } = new();
        public List<AccountingAttachmentDto> Attachments { get; set; } = new();
        public List<AccountingTimelineEventDto> Timeline { get; set; } = new();
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }

    public class ReceiptListItemDto
    {
        public int Id { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public string? SalesOrderNumber { get; set; }
        public DateTime ReceiptDate { get; set; }
        public string ReceiptMode { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public decimal Tds { get; set; }
        public decimal NetAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class ReceiptDashboardDto
    {
        public decimal TodaysReceipts { get; set; }
        public int Pending { get; set; }
        public int Received { get; set; }
        public decimal Outstanding { get; set; }
    }

    public class ReceiptCreateRequestDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int? SalesOrderId { get; set; }
        public string? SalesOrderNumber { get; set; }
        public string ReceiptDate { get; set; } = string.Empty;
        public string ReceiptMode { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string BankAccount { get; set; } = string.Empty;
        public string? ReferenceNumber { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public decimal Tds { get; set; }
        public string? Remarks { get; set; }
        public string? Notes { get; set; }
    }

    public class ReceiptUpdateRequestDto
    {
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? InvoiceId { get; set; }
        public string? InvoiceNumber { get; set; }
        public int? SalesOrderId { get; set; }
        public string? SalesOrderNumber { get; set; }
        public string? ReceiptDate { get; set; }
        public string? ReceiptMode { get; set; }
        public string? BankName { get; set; }
        public string? BankAccount { get; set; }
        public string? ReferenceNumber { get; set; }
        public decimal? Amount { get; set; }
        public string? Currency { get; set; }
        public decimal? Tds { get; set; }
        public string? Remarks { get; set; }
        public string? Notes { get; set; }
    }

    // Outstanding
    public class OutstandingRowDto
    {
        public int Id { get; set; }
        public string PartyType { get; set; } = string.Empty;
        public int PartyId { get; set; }
        public string PartyName { get; set; } = string.Empty;
        public int DocumentId { get; set; }
        public string DocumentNumber { get; set; } = string.Empty;
        public DateTime DocumentDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal OriginalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Outstanding { get; set; }
        public string Status { get; set; } = string.Empty;
        public int AgeingDays { get; set; }
        public decimal Bucket0to30 { get; set; }
        public decimal Bucket31to60 { get; set; }
        public decimal Bucket61to90 { get; set; }
        public decimal Bucket90plus { get; set; }
    }

    public class OutstandingDashboardDto
    {
        public decimal TotalOutstanding { get; set; }
        public decimal CustomerOutstanding { get; set; }
        public decimal VendorOutstanding { get; set; }
        public decimal Overdue { get; set; }
    }

    public class AgeingReportDto
    {
        public string PartyType { get; set; } = "All";
        public decimal Bucket0to30 { get; set; }
        public decimal Bucket31to60 { get; set; }
        public decimal Bucket61to90 { get; set; }
        public decimal Bucket90plus { get; set; }
        public decimal Total { get; set; }
        public List<OutstandingRowDto> Rows { get; set; } = new();
    }

    // Bank Reconciliation
    public class BankReconciliationDto
    {
        public int Id { get; set; }
        public string ReconciliationNumber { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public DateTime StatementDate { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public decimal BookClosingBalance { get; set; }
        public decimal Difference { get; set; }
        public int MatchedCount { get; set; }
        public int UnmatchedCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<int> PaymentIds { get; set; } = new();
        public List<int> ReceiptIds { get; set; } = new();
        public string? Remarks { get; set; }
        public List<AccountingNoteDto> Notes { get; set; } = new();
        public List<AccountingAttachmentDto> Attachments { get; set; } = new();
        public List<AccountingTimelineEventDto> Timeline { get; set; } = new();
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }

    public class BankReconListItemDto
    {
        public int Id { get; set; }
        public string ReconciliationNumber { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public DateTime StatementDate { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public decimal Difference { get; set; }
        public int MatchedCount { get; set; }
        public int UnmatchedCount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class BankReconDashboardDto
    {
        public int Matched { get; set; }
        public int Pending { get; set; }
        public decimal Difference { get; set; }
    }

    public class BankReconCreateRequestDto
    {
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string StatementDate { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public decimal BookClosingBalance { get; set; }
        public List<int>? PaymentIds { get; set; }
        public List<int>? ReceiptIds { get; set; }
        public string? Remarks { get; set; }
    }

    public class BankReconUpdateRequestDto
    {
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? StatementDate { get; set; }
        public decimal? OpeningBalance { get; set; }
        public decimal? ClosingBalance { get; set; }
        public decimal? BookClosingBalance { get; set; }
        public List<int>? PaymentIds { get; set; }
        public List<int>? ReceiptIds { get; set; }
        public string? Remarks { get; set; }
    }

    // Financial Reports
    public class ReportLineItemDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Section { get; set; }
    }

    public class TrialBalanceLineDto
    {
        public string Account { get; set; } = string.Empty;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }

    public class ProfitLossReportDto
    {
        public string FinancialYear { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public string FromDate { get; set; } = string.Empty;
        public string ToDate { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal Expenses { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal NetProfit { get; set; }
        public List<ReportLineItemDto> RevenueLines { get; set; } = new();
        public List<ReportLineItemDto> ExpenseLines { get; set; } = new();
    }

    public class BalanceSheetReportDto
    {
        public string FinancialYear { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public string AsOfDate { get; set; } = string.Empty;
        public decimal Assets { get; set; }
        public decimal Liabilities { get; set; }
        public decimal Equity { get; set; }
        public List<ReportLineItemDto> AssetLines { get; set; } = new();
        public List<ReportLineItemDto> LiabilityLines { get; set; } = new();
        public List<ReportLineItemDto> EquityLines { get; set; } = new();
    }

    public class TrialBalanceReportDto
    {
        public string FinancialYear { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public string AsOfDate { get; set; } = string.Empty;
        public List<TrialBalanceLineDto> Lines { get; set; } = new();
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
    }

    public class CashFlowReportDto
    {
        public string FinancialYear { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public string FromDate { get; set; } = string.Empty;
        public string ToDate { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public decimal Operating { get; set; }
        public decimal Investing { get; set; }
        public decimal Financing { get; set; }
        public List<ReportLineItemDto> Lines { get; set; } = new();
    }

    public class FinancialDashboardDto
    {
        public decimal Revenue { get; set; }
        public decimal Expenses { get; set; }
        public decimal Profit { get; set; }
        public decimal Cash { get; set; }
    }

    public class FinancialReportFilterDto
    {
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
        public string? Branch { get; set; }
        public string? FinancialYear { get; set; }
    }
}
