using System;
using System.Collections.Generic;

namespace ERP.Domain.Accounting
{
    public class PaymentEntry
    {
        public int Id { get; set; }
        public string PaymentNumber { get; set; } = string.Empty;
        public int VendorId { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public int PurchaseBillId { get; set; }
        public string PurchaseBillNumber { get; set; } = string.Empty;

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        public PaymentMode PaymentMode { get; set; } = PaymentMode.BankTransfer;
        public string BankName { get; set; } = string.Empty;
        public string BankAccount { get; set; } = string.Empty;
        public string? ChequeNumber { get; set; }
        public string? ReferenceNumber { get; set; }

        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public decimal Tds { get; set; }
        public decimal NetAmount { get; set; }
        public PaymentEntryStatus Status { get; set; } = PaymentEntryStatus.Draft;
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

    public class PaymentDocumentSequence
    {
        public int Id { get; set; }
        public string Prefix { get; set; } = string.Empty;
        public int LastSequence { get; set; }
    }
}
