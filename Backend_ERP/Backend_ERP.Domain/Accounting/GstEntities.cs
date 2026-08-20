using System;
using System.Collections.Generic;

namespace ERP.Domain.Accounting
{
    public class GstTransaction
    {
        public int Id { get; set; }
        public string GstNumber { get; set; } = string.Empty;
        public int? InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? VendorId { get; set; }
        public string? VendorName { get; set; }
        public GstTxnType TxnType { get; set; } = GstTxnType.SalesInvoice;

        public decimal TaxableValue { get; set; }
        public decimal Cgst { get; set; }
        public decimal Sgst { get; set; }
        public decimal Igst { get; set; }
        public decimal Cess { get; set; }
        public decimal TaxAmount { get; set; }

        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
        public string ReturnPeriod { get; set; } = string.Empty;
        public GstReturnStatus Status { get; set; } = GstReturnStatus.Draft;
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

    public class GstReturn
    {
        public int Id { get; set; }
        public string ReturnPeriod { get; set; } = string.Empty;
        public GstReturnStatus Status { get; set; } = GstReturnStatus.Draft;
        public decimal GstCollected { get; set; }
        public decimal GstPaid { get; set; }
        public decimal NetPayable { get; set; }
        public int TransactionCount { get; set; }

        public DateTime? FiledAt { get; set; }
        public string? FiledBy { get; set; }
        public string? Remarks { get; set; }

        public List<AccountingTimelineEvent> Timeline { get; set; } = new();

        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string UpdatedBy { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class GstDocumentSequence
    {
        public int Id { get; set; }
        public string Prefix { get; set; } = string.Empty;
        public int LastSequence { get; set; }
    }
}
