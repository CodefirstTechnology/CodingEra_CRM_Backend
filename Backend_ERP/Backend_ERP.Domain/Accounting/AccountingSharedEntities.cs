using System;

namespace ERP.Domain.Accounting
{
    public class AccountingNote
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? CustomerLedgerEntryId { get; set; }
        public int? GstTransactionId { get; set; }
        public int? PaymentEntryId { get; set; }
        public int? ReceiptEntryId { get; set; }
        public int? BankReconciliationId { get; set; }
    }

    public class AccountingAttachment
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int SizeKb { get; set; }
        public string UploadedBy { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public int? CustomerLedgerEntryId { get; set; }
        public int? GstTransactionId { get; set; }
        public int? PaymentEntryId { get; set; }
        public int? ReceiptEntryId { get; set; }
        public int? BankReconciliationId { get; set; }
    }

    public class AccountingTimelineEvent
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string? FromStatus { get; set; }
        public string? ToStatus { get; set; }
        public string? Remarks { get; set; }

        public int? CustomerLedgerEntryId { get; set; }
        public int? GstTransactionId { get; set; }
        public int? GstReturnId { get; set; }
        public int? PaymentEntryId { get; set; }
        public int? ReceiptEntryId { get; set; }
        public int? BankReconciliationId { get; set; }
    }
}
