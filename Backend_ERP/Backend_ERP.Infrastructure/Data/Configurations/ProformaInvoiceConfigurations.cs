using ERP.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class ProformaInvoiceConfiguration : IEntityTypeConfiguration<ProformaInvoice>
    {
        public void Configure(EntityTypeBuilder<ProformaInvoice> builder)
        {
            builder.ToTable("proforma_invoices");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.PiNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.PiNumber).IsUnique();

            builder.Property(x => x.CustomerId).HasMaxLength(64);
            builder.Property(x => x.CustomerName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.ContactPerson).HasMaxLength(256);
            builder.Property(x => x.BillingAddress).HasMaxLength(2000);
            builder.Property(x => x.ShippingAddress).HasMaxLength(2000);
            builder.Property(x => x.SalesPerson).HasMaxLength(256);
            builder.Property(x => x.Currency).HasMaxLength(8).IsRequired();
            builder.Property(x => x.ExchangeRate).HasPrecision(18, 6);
            builder.Property(x => x.QuotationNumber).HasMaxLength(64);
            builder.Property(x => x.SalesOrderNumber).HasMaxLength(64);
            builder.Property(x => x.PaymentTerms).HasMaxLength(128);
            builder.Property(x => x.DeliveryTerms).HasMaxLength(128);
            builder.Property(x => x.CustomerNotes).HasMaxLength(4000);
            builder.Property(x => x.InternalNotes).HasMaxLength(4000);
            builder.Property(x => x.Status).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.CreatedBy).HasMaxLength(64);
            builder.Property(x => x.UpdatedBy).HasMaxLength(64);
            builder.Property(x => x.ConvertedInvoiceNumber).HasMaxLength(64);

            builder.Property(x => x.Subtotal).HasPrecision(18, 2);
            builder.Property(x => x.DiscountTotal).HasPrecision(18, 2);
            builder.Property(x => x.TaxTotal).HasPrecision(18, 2);
            builder.Property(x => x.GrandTotal).HasPrecision(18, 2);

            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.InvoiceDate);
            builder.HasIndex(x => x.CustomerName);
            builder.HasIndex(x => x.IsDeleted);

            builder.HasOne(x => x.SalesOrder)
                .WithMany()
                .HasForeignKey(x => x.SalesOrderId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(x => x.Items)
                .WithOne(x => x.ProformaInvoice)
                .HasForeignKey(x => x.ProformaInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.StatusHistory)
                .WithOne(x => x.ProformaInvoice)
                .HasForeignKey(x => x.ProformaInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.ApprovalHistory)
                .WithOne(x => x.ProformaInvoice)
                .HasForeignKey(x => x.ProformaInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class ProformaInvoiceItemConfiguration : IEntityTypeConfiguration<ProformaInvoiceItem>
    {
        public void Configure(EntityTypeBuilder<ProformaInvoiceItem> builder)
        {
            builder.ToTable("proforma_invoice_items");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.LineKey).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ItemName).HasMaxLength(512).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(2000);
            builder.Property(x => x.Unit).HasMaxLength(32);
            builder.Property(x => x.Quantity).HasPrecision(18, 4);
            builder.Property(x => x.Rate).HasPrecision(18, 4);
            builder.Property(x => x.Discount).HasPrecision(9, 4);
            builder.Property(x => x.Gst).HasPrecision(9, 4);
            builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
            builder.Property(x => x.Amount).HasPrecision(18, 2);
            builder.HasIndex(x => new { x.ProformaInvoiceId, x.SortOrder });
        }
    }

    public class ProformaInvoiceStatusHistoryConfiguration : IEntityTypeConfiguration<ProformaInvoiceStatusHistory>
    {
        public void Configure(EntityTypeBuilder<ProformaInvoiceStatusHistory> builder)
        {
            builder.ToTable("proforma_invoice_status_histories");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.EntryKey).HasMaxLength(64).IsRequired();
            builder.Property(x => x.OldStatus).HasMaxLength(64);
            builder.Property(x => x.NewStatus).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.ChangedBy).HasMaxLength(64);
            builder.HasIndex(x => new { x.ProformaInvoiceId, x.ChangedOn });
        }
    }

    public class ProformaInvoiceApprovalHistoryConfiguration : IEntityTypeConfiguration<ProformaInvoiceApprovalHistory>
    {
        public void Configure(EntityTypeBuilder<ProformaInvoiceApprovalHistory> builder)
        {
            builder.ToTable("proforma_invoice_approval_histories");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.EntryKey).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ApprovalLevel).HasMaxLength(64);
            builder.Property(x => x.Decision).HasMaxLength(64);
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.ApprovedBy).HasMaxLength(64);
            builder.HasIndex(x => new { x.ProformaInvoiceId, x.ApprovedOn });
        }
    }

    public class ProformaInvoiceDocumentSequenceConfiguration : IEntityTypeConfiguration<ProformaInvoiceDocumentSequence>
    {
        public void Configure(EntityTypeBuilder<ProformaInvoiceDocumentSequence> builder)
        {
            builder.ToTable("proforma_invoice_document_sequences");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.Year).IsUnique();
        }
    }
}
