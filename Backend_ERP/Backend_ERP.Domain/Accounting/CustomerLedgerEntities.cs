using System;
using System.Collections.Generic;

namespace ERP.Domain.Accounting
{
    public class CustomerLedgerEntry
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

        public LedgerEntryType EntryType { get; set; } = LedgerEntryType.Invoice;
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        public DateTime? DueDate { get; set; }
        public int AgeingDays { get; set; }
        public string AgeingBucket { get; set; } = "0-30";
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

    public class CustomerLedgerDocumentSequence
    {
        public int Id { get; set; }
        public string Prefix { get; set; } = string.Empty;
        public int LastSequence { get; set; }
    }
}
