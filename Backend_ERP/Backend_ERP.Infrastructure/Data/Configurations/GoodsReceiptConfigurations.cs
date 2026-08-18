using ERP.Domain.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class GoodsReceiptConfiguration : IEntityTypeConfiguration<GoodsReceipt>
    {
        public void Configure(EntityTypeBuilder<GoodsReceipt> builder)
        {
            builder.ToTable("goods_receipts");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.GRNNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.GRNNumber).IsUnique();

            builder.Property(x => x.PurchaseOrderNumber).HasMaxLength(64).IsRequired();
            builder.Property(x => x.VendorName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Warehouse).HasMaxLength(256).HasDefaultValue("Main Store — Sanand");
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.Notes).HasMaxLength(2000);

            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.PurchaseOrderId);

            builder.HasOne(x => x.PurchaseOrder)
                .WithMany()
                .HasForeignKey(x => x.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Items)
                .WithOne(x => x.GoodsReceipt)
                .HasForeignKey(x => x.GoodsReceiptId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.History)
                .WithOne(x => x.GoodsReceipt)
                .HasForeignKey(x => x.GoodsReceiptId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class GoodsReceiptItemConfiguration : IEntityTypeConfiguration<GoodsReceiptItem>
    {
        public void Configure(EntityTypeBuilder<GoodsReceiptItem> builder)
        {
            builder.ToTable("goods_receipt_items");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ItemName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(1000);
            builder.Property(x => x.Unit).HasMaxLength(64).HasDefaultValue("Nos");

            builder.Property(x => x.OrderedQuantity).HasPrecision(18, 4).IsRequired();
            builder.Property(x => x.PreviouslyReceivedQuantity).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.RemainingQuantity).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.ReceivedQuantity).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.RejectedQuantity).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.Remarks).HasMaxLength(2000);

            builder.HasIndex(x => x.GoodsReceiptId);
            builder.HasIndex(x => x.PurchaseOrderLineId);
        }
    }

    public class GoodsReceiptStatusHistoryConfiguration : IEntityTypeConfiguration<GoodsReceiptStatusHistory>
    {
        public void Configure(EntityTypeBuilder<GoodsReceiptStatusHistory> builder)
        {
            builder.ToTable("goods_receipt_status_histories");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.PreviousStatus).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.User).HasMaxLength(256);
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.Label).HasMaxLength(256);

            builder.HasIndex(x => new { x.GoodsReceiptId, x.Date });
        }
    }

    public class GoodsReceiptDocumentSequenceConfiguration : IEntityTypeConfiguration<GoodsReceiptDocumentSequence>
    {
        public void Configure(EntityTypeBuilder<GoodsReceiptDocumentSequence> builder)
        {
            builder.ToTable("goods_receipt_document_sequences");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.Prefix).IsUnique();
        }
    }
}
