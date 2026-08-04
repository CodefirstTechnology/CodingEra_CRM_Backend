using ERP.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
    {
        public void Configure(EntityTypeBuilder<SalesOrder> builder)
        {
            builder.ToTable("sales_orders");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.SalesOrderNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.SalesOrderNumber).IsUnique();

            builder.Property(x => x.QuotationNumber).HasMaxLength(64);
            builder.Property(x => x.SourceType).HasMaxLength(32).IsRequired();
            builder.Property(x => x.CustomerName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.ContactPerson).HasMaxLength(256);
            builder.Property(x => x.BillingAddress).HasMaxLength(2000);
            builder.Property(x => x.ShippingAddress).HasMaxLength(2000);
            builder.Property(x => x.CustomerEmail).HasMaxLength(256);
            builder.Property(x => x.CustomerPhone).HasMaxLength(64);
            builder.Property(x => x.SalesPerson).HasMaxLength(256);
            builder.Property(x => x.Notes).HasMaxLength(4000);
            builder.Property(x => x.PaymentTerms).HasMaxLength(128);
            builder.Property(x => x.DeliveryTerms).HasMaxLength(128);
            builder.Property(x => x.Status).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            builder.Property(x => x.Subtotal).HasPrecision(18, 2);
            builder.Property(x => x.DiscountTotal).HasPrecision(18, 2);
            builder.Property(x => x.GstTotal).HasPrecision(18, 2);
            builder.Property(x => x.GrandTotal).HasPrecision(18, 2);

            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.OrderDate);
            builder.HasIndex(x => x.CustomerName);

            builder.HasMany(x => x.Items)
                .WithOne(x => x.SalesOrder)
                .HasForeignKey(x => x.SalesOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.StatusHistory)
                .WithOne(x => x.SalesOrder)
                .HasForeignKey(x => x.SalesOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.EmailHistory)
                .WithOne(x => x.SalesOrder)
                .HasForeignKey(x => x.SalesOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class SalesOrderItemConfiguration : IEntityTypeConfiguration<SalesOrderItem>
    {
        public void Configure(EntityTypeBuilder<SalesOrderItem> builder)
        {
            builder.ToTable("sales_order_items");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.LineKey).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ItemName).HasMaxLength(512).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(2000);
            builder.Property(x => x.Unit).HasMaxLength(32);
            builder.Property(x => x.Quantity).HasPrecision(18, 4);
            builder.Property(x => x.Rate).HasPrecision(18, 4);
            builder.Property(x => x.Discount).HasPrecision(9, 4);
            builder.Property(x => x.Gst).HasPrecision(9, 4);
            builder.Property(x => x.Amount).HasPrecision(18, 2);

            builder.HasIndex(x => new { x.SalesOrderId, x.SortIndex });
        }
    }

    public class SalesOrderStatusHistoryConfiguration : IEntityTypeConfiguration<SalesOrderStatusHistory>
    {
        public void Configure(EntityTypeBuilder<SalesOrderStatusHistory> builder)
        {
            builder.ToTable("sales_order_status_histories");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.EntryKey).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Status).HasMaxLength(64).IsRequired();
            builder.Property(x => x.User).HasMaxLength(256);
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.Label).HasMaxLength(128);

            builder.HasIndex(x => new { x.SalesOrderId, x.Date });
        }
    }

    public class SalesOrderEmailHistoryConfiguration : IEntityTypeConfiguration<SalesOrderEmailHistory>
    {
        public void Configure(EntityTypeBuilder<SalesOrderEmailHistory> builder)
        {
            builder.ToTable("sales_order_email_histories");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.EntryKey).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Recipient).HasMaxLength(256);
            builder.Property(x => x.Action).HasMaxLength(128);
            builder.Property(x => x.Status).HasMaxLength(64);
        }
    }

    public class SalesOrderDocumentSequenceConfiguration : IEntityTypeConfiguration<SalesOrderDocumentSequence>
    {
        public void Configure(EntityTypeBuilder<SalesOrderDocumentSequence> builder)
        {
            builder.ToTable("sales_order_document_sequences");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.Year).IsUnique();
        }
    }
}
