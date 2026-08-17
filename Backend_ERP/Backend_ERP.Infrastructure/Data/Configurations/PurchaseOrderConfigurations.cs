using ERP.Domain.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
    {
        public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
        {
            builder.ToTable("purchase_orders");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.PurchaseOrderNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.PurchaseOrderNumber).IsUnique();

            builder.Property(x => x.ReferenceNumber).HasMaxLength(128);
            builder.Property(x => x.VendorName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.VendorContact).HasMaxLength(256);
            builder.Property(x => x.BillingAddress).HasMaxLength(1000);
            builder.Property(x => x.ShippingAddress).HasMaxLength(1000);
            builder.Property(x => x.VendorEmail).HasMaxLength(256);
            builder.Property(x => x.VendorPhone).HasMaxLength(64);
            builder.Property(x => x.GstNumber).HasMaxLength(64);
            builder.Property(x => x.PaymentTerms).HasMaxLength(500);
            builder.Property(x => x.DeliveryTerms).HasMaxLength(500);
            builder.Property(x => x.BuyerName).HasMaxLength(256);
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.SalesOrderNumber).HasMaxLength(64);
            builder.Property(x => x.Currency).HasMaxLength(10).HasDefaultValue("INR");

            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Priority).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.SourceType).HasConversion<string>().HasMaxLength(64).IsRequired();

            builder.Property(x => x.Subtotal).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.DiscountTotal).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.TaxTotal).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.TotalAmount).HasPrecision(18, 4).HasDefaultValue(0m);

            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.Priority);
            builder.HasIndex(x => x.VendorId);

            builder.HasOne(x => x.Vendor)
                .WithMany()
                .HasForeignKey(x => x.VendorId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(x => x.Lines)
                .WithOne(x => x.PurchaseOrder)
                .HasForeignKey(x => x.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.History)
                .WithOne(x => x.PurchaseOrder)
                .HasForeignKey(x => x.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.ApprovalHistory)
                .WithOne(x => x.PurchaseOrder)
                .HasForeignKey(x => x.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
    {
        public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
        {
            builder.ToTable("purchase_order_lines");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ItemName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(1000);
            builder.Property(x => x.Quantity).HasPrecision(18, 4).IsRequired();
            builder.Property(x => x.Unit).HasMaxLength(64).HasDefaultValue("Nos");
            builder.Property(x => x.Rate).HasPrecision(18, 4).IsRequired();
            builder.Property(x => x.Discount).HasPrecision(5, 2).HasDefaultValue(0m);
            builder.Property(x => x.Tax).HasPrecision(5, 2).HasDefaultValue(0m);
            builder.Property(x => x.Amount).HasPrecision(18, 4).HasDefaultValue(0m);

            builder.HasIndex(x => x.PurchaseOrderId);
        }
    }

    public class PurchaseOrderStatusHistoryConfiguration : IEntityTypeConfiguration<PurchaseOrderStatusHistory>
    {
        public void Configure(EntityTypeBuilder<PurchaseOrderStatusHistory> builder)
        {
            builder.ToTable("purchase_order_status_histories");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.PreviousStatus).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.User).HasMaxLength(256);
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.Label).HasMaxLength(256);

            builder.HasIndex(x => new { x.PurchaseOrderId, x.Date });
        }
    }

    public class PurchaseOrderApprovalHistoryConfiguration : IEntityTypeConfiguration<PurchaseOrderApprovalHistory>
    {
        public void Configure(EntityTypeBuilder<PurchaseOrderApprovalHistory> builder)
        {
            builder.ToTable("purchase_order_approval_histories");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.EventKind).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Decision).HasMaxLength(128).IsRequired();
            builder.Property(x => x.Approver).HasMaxLength(256);
            builder.Property(x => x.Role).HasMaxLength(128);
            builder.Property(x => x.Remarks).HasMaxLength(2000);

            builder.HasIndex(x => new { x.PurchaseOrderId, x.DecisionDate });
        }
    }

    public class PurchaseOrderDocumentSequenceConfiguration : IEntityTypeConfiguration<PurchaseOrderDocumentSequence>
    {
        public void Configure(EntityTypeBuilder<PurchaseOrderDocumentSequence> builder)
        {
            builder.ToTable("purchase_order_document_sequences");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.Prefix).IsUnique();
        }
    }
}
