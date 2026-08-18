using ERP.Domain.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class PurchaseBillConfiguration : IEntityTypeConfiguration<PurchaseBill>
    {
        public void Configure(EntityTypeBuilder<PurchaseBill> builder)
        {
            builder.ToTable("purchase_bills");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.BillNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.BillNumber).IsUnique();

            builder.Property(x => x.InvoiceNumber).HasMaxLength(128).IsRequired();
            builder.Property(x => x.VendorName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.PurchaseOrderNumber).HasMaxLength(64);
            builder.Property(x => x.GRNNumber).HasMaxLength(64);
            builder.Property(x => x.Currency).HasMaxLength(16);
            builder.Property(x => x.PaymentTerms).HasMaxLength(128);

            builder.Property(x => x.SubTotal).HasPrecision(18, 4);
            builder.Property(x => x.DiscountTotal).HasPrecision(18, 4);
            builder.Property(x => x.TaxTotal).HasPrecision(18, 4);
            builder.Property(x => x.RoundOff).HasPrecision(18, 4);
            builder.Property(x => x.GrandTotal).HasPrecision(18, 4);
            builder.Property(x => x.PaidAmount).HasPrecision(18, 4);
            builder.Property(x => x.BalanceAmount).HasPrecision(18, 4);

            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.PaymentStatus);
            builder.HasIndex(x => x.GRNId);
            builder.HasIndex(x => x.PurchaseOrderId);

            builder.HasMany(x => x.Lines)
                .WithOne(x => x.PurchaseBill)
                .HasForeignKey(x => x.PurchaseBillId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.History)
                .WithOne(x => x.PurchaseBill)
                .HasForeignKey(x => x.PurchaseBillId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class PurchaseBillLineConfiguration : IEntityTypeConfiguration<PurchaseBillLine>
    {
        public void Configure(EntityTypeBuilder<PurchaseBillLine> builder)
        {
            builder.ToTable("purchase_bill_lines");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ItemName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(500);

            builder.Property(x => x.Quantity).HasPrecision(18, 4);
            builder.Property(x => x.UnitPrice).HasPrecision(18, 4);
            builder.Property(x => x.DiscountAmount).HasPrecision(18, 4);
            builder.Property(x => x.TaxPercent).HasPrecision(18, 4);
            builder.Property(x => x.TaxAmount).HasPrecision(18, 4);
            builder.Property(x => x.TotalAmount).HasPrecision(18, 4);

            builder.HasIndex(x => x.PurchaseBillId);
        }
    }

    public class PurchaseBillHistoryConfiguration : IEntityTypeConfiguration<PurchaseBillHistory>
    {
        public void Configure(EntityTypeBuilder<PurchaseBillHistory> builder)
        {
            builder.ToTable("purchase_bill_histories");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.User).HasMaxLength(256);
            builder.Property(x => x.Remarks).HasMaxLength(2000);

            builder.HasIndex(x => x.PurchaseBillId);
        }
    }

    public class PurchaseBillDocumentSequenceConfiguration : IEntityTypeConfiguration<PurchaseBillDocumentSequence>
    {
        public void Configure(EntityTypeBuilder<PurchaseBillDocumentSequence> builder)
        {
            builder.ToTable("purchase_bill_document_sequences");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.Prefix).IsUnique();
        }
    }
}
