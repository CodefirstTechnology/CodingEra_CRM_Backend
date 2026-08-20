using ERP.Domain.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class CustomerLedgerEntryConfiguration : IEntityTypeConfiguration<CustomerLedgerEntry>
    {
        public void Configure(EntityTypeBuilder<CustomerLedgerEntry> builder)
        {
            builder.ToTable("customer_ledger_entries");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.LedgerNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.LedgerNumber).IsUnique();

            builder.Property(x => x.CustomerName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.OpeningBalance).HasPrecision(18, 4);
            builder.Property(x => x.Debit).HasPrecision(18, 4);
            builder.Property(x => x.Credit).HasPrecision(18, 4);
            builder.Property(x => x.RunningBalance).HasPrecision(18, 4);
            builder.Property(x => x.Outstanding).HasPrecision(18, 4);

            builder.Property(x => x.InvoiceNumber).HasMaxLength(64);
            builder.Property(x => x.ReceiptNumber).HasMaxLength(64);
            builder.Property(x => x.SalesOrderNumber).HasMaxLength(64);
            builder.Property(x => x.ProformaInvoiceNumber).HasMaxLength(64);
            builder.Property(x => x.AgeingBucket).HasMaxLength(32);
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            builder.Property(x => x.EntryType).HasConversion<string>().HasMaxLength(64);

            builder.HasIndex(x => x.CustomerId);
            builder.HasIndex(x => x.TransactionDate);
            builder.HasIndex(x => x.EntryType);

            builder.HasMany(x => x.Notes)
                .WithOne()
                .HasForeignKey(x => x.CustomerLedgerEntryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Attachments)
                .WithOne()
                .HasForeignKey(x => x.CustomerLedgerEntryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Timeline)
                .WithOne()
                .HasForeignKey(x => x.CustomerLedgerEntryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class CustomerLedgerDocumentSequenceConfiguration : IEntityTypeConfiguration<CustomerLedgerDocumentSequence>
    {
        public void Configure(EntityTypeBuilder<CustomerLedgerDocumentSequence> builder)
        {
            builder.ToTable("customer_ledger_document_sequences");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Prefix).HasMaxLength(32).IsRequired();
            builder.HasIndex(x => x.Prefix).IsUnique();
        }
    }

    public class GstTransactionConfiguration : IEntityTypeConfiguration<GstTransaction>
    {
        public void Configure(EntityTypeBuilder<GstTransaction> builder)
        {
            builder.ToTable("gst_transactions");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.GstNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.GstNumber).IsUnique();

            builder.Property(x => x.InvoiceNumber).HasMaxLength(64).IsRequired();
            builder.Property(x => x.CustomerName).HasMaxLength(256);
            builder.Property(x => x.VendorName).HasMaxLength(256);
            builder.Property(x => x.ReturnPeriod).HasMaxLength(16).IsRequired();

            builder.Property(x => x.TaxableValue).HasPrecision(18, 4);
            builder.Property(x => x.Cgst).HasPrecision(18, 4);
            builder.Property(x => x.Sgst).HasPrecision(18, 4);
            builder.Property(x => x.Igst).HasPrecision(18, 4);
            builder.Property(x => x.Cess).HasPrecision(18, 4);
            builder.Property(x => x.TaxAmount).HasPrecision(18, 4);

            builder.Property(x => x.TxnType).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64);

            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            builder.HasIndex(x => x.ReturnPeriod);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.TxnType);

            builder.HasMany(x => x.Notes)
                .WithOne()
                .HasForeignKey(x => x.GstTransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Attachments)
                .WithOne()
                .HasForeignKey(x => x.GstTransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Timeline)
                .WithOne()
                .HasForeignKey(x => x.GstTransactionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class GstReturnConfiguration : IEntityTypeConfiguration<GstReturn>
    {
        public void Configure(EntityTypeBuilder<GstReturn> builder)
        {
            builder.ToTable("gst_returns");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ReturnPeriod).HasMaxLength(16).IsRequired();
            builder.HasIndex(x => x.ReturnPeriod).IsUnique();

            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.GstCollected).HasPrecision(18, 4);
            builder.Property(x => x.GstPaid).HasPrecision(18, 4);
            builder.Property(x => x.NetPayable).HasPrecision(18, 4);

            builder.Property(x => x.FiledBy).HasMaxLength(256);
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            builder.HasMany(x => x.Timeline)
                .WithOne()
                .HasForeignKey(x => x.GstReturnId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class GstDocumentSequenceConfiguration : IEntityTypeConfiguration<GstDocumentSequence>
    {
        public void Configure(EntityTypeBuilder<GstDocumentSequence> builder)
        {
            builder.ToTable("gst_document_sequences");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Prefix).HasMaxLength(32).IsRequired();
            builder.HasIndex(x => x.Prefix).IsUnique();
        }
    }

    public class PaymentEntryConfiguration : IEntityTypeConfiguration<PaymentEntry>
    {
        public void Configure(EntityTypeBuilder<PaymentEntry> builder)
        {
            builder.ToTable("payment_entries");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.PaymentNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.PaymentNumber).IsUnique();

            builder.Property(x => x.VendorName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.PurchaseBillNumber).HasMaxLength(64).IsRequired();
            builder.Property(x => x.BankName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.BankAccount).HasMaxLength(128).IsRequired();
            builder.Property(x => x.ChequeNumber).HasMaxLength(64);
            builder.Property(x => x.ReferenceNumber).HasMaxLength(128);
            builder.Property(x => x.Currency).HasMaxLength(16);

            builder.Property(x => x.Amount).HasPrecision(18, 4);
            builder.Property(x => x.Tds).HasPrecision(18, 4);
            builder.Property(x => x.NetAmount).HasPrecision(18, 4);

            builder.Property(x => x.PaymentMode).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64);

            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            builder.HasIndex(x => x.VendorId);
            builder.HasIndex(x => x.PurchaseBillId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.PaymentDate);

            builder.HasMany(x => x.Notes)
                .WithOne()
                .HasForeignKey(x => x.PaymentEntryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Attachments)
                .WithOne()
                .HasForeignKey(x => x.PaymentEntryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Timeline)
                .WithOne()
                .HasForeignKey(x => x.PaymentEntryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class PaymentDocumentSequenceConfiguration : IEntityTypeConfiguration<PaymentDocumentSequence>
    {
        public void Configure(EntityTypeBuilder<PaymentDocumentSequence> builder)
        {
            builder.ToTable("payment_document_sequences");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Prefix).HasMaxLength(32).IsRequired();
            builder.HasIndex(x => x.Prefix).IsUnique();
        }
    }

    public class ReceiptEntryConfiguration : IEntityTypeConfiguration<ReceiptEntry>
    {
        public void Configure(EntityTypeBuilder<ReceiptEntry> builder)
        {
            builder.ToTable("receipt_entries");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ReceiptNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.ReceiptNumber).IsUnique();

            builder.Property(x => x.CustomerName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.InvoiceNumber).HasMaxLength(64).IsRequired();
            builder.Property(x => x.SalesOrderNumber).HasMaxLength(64);
            builder.Property(x => x.BankName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.BankAccount).HasMaxLength(128).IsRequired();
            builder.Property(x => x.ReferenceNumber).HasMaxLength(128);
            builder.Property(x => x.Currency).HasMaxLength(16);

            builder.Property(x => x.Amount).HasPrecision(18, 4);
            builder.Property(x => x.Tds).HasPrecision(18, 4);
            builder.Property(x => x.NetAmount).HasPrecision(18, 4);

            builder.Property(x => x.ReceiptMode).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64);

            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            builder.HasIndex(x => x.CustomerId);
            builder.HasIndex(x => x.InvoiceId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.ReceiptDate);

            builder.HasMany(x => x.Notes)
                .WithOne()
                .HasForeignKey(x => x.ReceiptEntryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Attachments)
                .WithOne()
                .HasForeignKey(x => x.ReceiptEntryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Timeline)
                .WithOne()
                .HasForeignKey(x => x.ReceiptEntryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class ReceiptDocumentSequenceConfiguration : IEntityTypeConfiguration<ReceiptDocumentSequence>
    {
        public void Configure(EntityTypeBuilder<ReceiptDocumentSequence> builder)
        {
            builder.ToTable("receipt_document_sequences");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Prefix).HasMaxLength(32).IsRequired();
            builder.HasIndex(x => x.Prefix).IsUnique();
        }
    }

    public class OutstandingRecordConfiguration : IEntityTypeConfiguration<OutstandingRecord>
    {
        public void Configure(EntityTypeBuilder<OutstandingRecord> builder)
        {
            builder.ToTable("outstanding_records");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.PartyType).HasConversion<string>().HasMaxLength(32);
            builder.Property(x => x.PartyName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.DocumentNumber).HasMaxLength(64).IsRequired();

            builder.Property(x => x.OriginalAmount).HasPrecision(18, 4);
            builder.Property(x => x.PaidAmount).HasPrecision(18, 4);
            builder.Property(x => x.Outstanding).HasPrecision(18, 4);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);

            builder.Property(x => x.Bucket0To30).HasPrecision(18, 4);
            builder.Property(x => x.Bucket31To60).HasPrecision(18, 4);
            builder.Property(x => x.Bucket61To90).HasPrecision(18, 4);
            builder.Property(x => x.Bucket90Plus).HasPrecision(18, 4);

            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            builder.HasIndex(x => new { x.PartyType, x.PartyId });
            builder.HasIndex(x => x.DocumentNumber);
            builder.HasIndex(x => x.Status);
        }
    }

    public class BankReconciliationConfiguration : IEntityTypeConfiguration<BankReconciliation>
    {
        public void Configure(EntityTypeBuilder<BankReconciliation> builder)
        {
            builder.ToTable("bank_reconciliations");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ReconciliationNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.ReconciliationNumber).IsUnique();

            builder.Property(x => x.BankName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.AccountNumber).HasMaxLength(128).IsRequired();

            builder.Property(x => x.OpeningBalance).HasPrecision(18, 4);
            builder.Property(x => x.ClosingBalance).HasPrecision(18, 4);
            builder.Property(x => x.BookClosingBalance).HasPrecision(18, 4);
            builder.Property(x => x.Difference).HasPrecision(18, 4);

            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.PaymentIdsJson).HasMaxLength(4000);
            builder.Property(x => x.ReceiptIdsJson).HasMaxLength(4000);
            builder.Property(x => x.Remarks).HasMaxLength(2000);

            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.StatementDate);

            builder.HasMany(x => x.Notes)
                .WithOne()
                .HasForeignKey(x => x.BankReconciliationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Attachments)
                .WithOne()
                .HasForeignKey(x => x.BankReconciliationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Timeline)
                .WithOne()
                .HasForeignKey(x => x.BankReconciliationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class BankReconDocumentSequenceConfiguration : IEntityTypeConfiguration<BankReconDocumentSequence>
    {
        public void Configure(EntityTypeBuilder<BankReconDocumentSequence> builder)
        {
            builder.ToTable("bank_recon_document_sequences");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Prefix).HasMaxLength(32).IsRequired();
            builder.HasIndex(x => x.Prefix).IsUnique();
        }
    }

    public class AccountingNoteConfiguration : IEntityTypeConfiguration<AccountingNote>
    {
        public void Configure(EntityTypeBuilder<AccountingNote> builder)
        {
            builder.ToTable("accounting_notes");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Text).HasMaxLength(2000).IsRequired();
            builder.Property(x => x.CreatedBy).HasMaxLength(256);
        }
    }

    public class AccountingAttachmentConfiguration : IEntityTypeConfiguration<AccountingAttachment>
    {
        public void Configure(EntityTypeBuilder<AccountingAttachment> builder)
        {
            builder.ToTable("accounting_attachments");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
            builder.Property(x => x.UploadedBy).HasMaxLength(256);
        }
    }

    public class AccountingTimelineEventConfiguration : IEntityTypeConfiguration<AccountingTimelineEvent>
    {
        public void Configure(EntityTypeBuilder<AccountingTimelineEvent> builder)
        {
            builder.ToTable("accounting_timeline_events");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Action).HasMaxLength(256).IsRequired();
            builder.Property(x => x.User).HasMaxLength(256);
            builder.Property(x => x.FromStatus).HasMaxLength(64);
            builder.Property(x => x.ToStatus).HasMaxLength(64);
            builder.Property(x => x.Remarks).HasMaxLength(2000);
        }
    }
}
