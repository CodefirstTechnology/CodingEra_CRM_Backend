using System;
using System.Collections.Generic;

namespace ERP.Domain.Accounting
{
    public class ReceiptEntry
    {
        public int Id { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int? SalesOrderId { get; set; }
        public string? SalesOrderNumber { get; set; }

        public DateTime ReceiptDate { get; set; } = DateTime.UtcNow;
        public PaymentMode ReceiptMode { get; set; } = PaymentMode.BankTransfer;
        public string BankName { get; set; } = string.Empty;
        public string BankAccount { get; set; } = string.Empty;
        public string? ReferenceNumber { get; set; }

        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public decimal Tds { get; set; }
        public decimal NetAmount { get; set; }
        public ReceiptEntryStatus Status { get; set; } = ReceiptEntryStatus.Draft;
        public string? Remarks { get; set; }

        public List<AccountingNote> Notes { get; set; } = new();
        public List<AccountingAttachment> Attachments { get; set; } = new();
        public List<AccountingTimelineEvent> Timeline { get; set; } = new();

        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string UpdatedBy { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; }
    }

    public class ReceiptDocumentSequence
    {
        public int Id { get; set; }
        public string Prefix { get; set; } = string.Empty;
        public int LastSequence { get; set; }
    }
}
