using System;
using System.Collections.Generic;

namespace ERP.Domain.Accounting
{
    public class BankReconciliation
    {
        public int Id { get; set; }
        public string ReconciliationNumber { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public DateTime StatementDate { get; set; } = DateTime.UtcNow;

        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public decimal BookClosingBalance { get; set; }
        public decimal Difference { get; set; }

        public int MatchedCount { get; set; }
        public int UnmatchedCount { get; set; }
        public BankReconStatus Status { get; set; } = BankReconStatus.Draft;

        public string PaymentIdsJson { get; set; } = "[]";
        public string ReceiptIdsJson { get; set; } = "[]";
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

    public class BankReconDocumentSequence
    {
        public int Id { get; set; }
        public string Prefix { get; set; } = string.Empty;
        public int LastSequence { get; set; }
    }
}
