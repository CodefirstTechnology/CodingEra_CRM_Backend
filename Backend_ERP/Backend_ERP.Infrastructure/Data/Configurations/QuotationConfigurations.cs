using ERP.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class QuotationConfiguration : IEntityTypeConfiguration<Quotation>
    {
        public void Configure(EntityTypeBuilder<Quotation> builder)
        {
            builder.ToTable("quotations");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.QuotationNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.QuotationNumber);

            builder.Property(x => x.CustomerId).HasMaxLength(64);
            builder.Property(x => x.CustomerName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.ContactPerson).HasMaxLength(256);
            builder.Property(x => x.BillingAddress).HasMaxLength(2000);
            builder.Property(x => x.ShippingAddress).HasMaxLength(2000);
            builder.Property(x => x.CustomerEmail).HasMaxLength(256);
            builder.Property(x => x.CustomerPhone).HasMaxLength(64);
            builder.Property(x => x.SalesPerson).HasMaxLength(256);
            builder.Property(x => x.Status).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Notes).HasMaxLength(4000);
            builder.Property(x => x.PaymentTerms).HasMaxLength(128);
            builder.Property(x => x.DeliveryTerms).HasMaxLength(128);
            builder.Property(x => x.Currency).HasMaxLength(16);
            builder.Property(x => x.ExchangeRate).HasPrecision(18, 6);

            builder.Property(x => x.Subtotal).HasPrecision(18, 2);
            builder.Property(x => x.DiscountTotal).HasPrecision(18, 2);
            builder.Property(x => x.TaxTotal).HasPrecision(18, 2);
            builder.Property(x => x.FreightAmount).HasPrecision(18, 2);
            builder.Property(x => x.PackagingAmount).HasPrecision(18, 2);
            builder.Property(x => x.RoundOff).HasPrecision(18, 2);
            builder.Property(x => x.GrandTotal).HasPrecision(18, 2);

            builder.Property(x => x.ClientPoNumber).HasMaxLength(128);
            builder.Property(x => x.ClientPoAttachmentUrl).HasMaxLength(1024);
            builder.Property(x => x.ConvertedSalesOrderNumber).HasMaxLength(64);

            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.QuotationDate);
            builder.HasIndex(x => x.IsCurrentRevision);

            builder.HasIndex(x => x.ConvertedSalesOrderId)
                .IsUnique()
                .HasFilter("\"ConvertedSalesOrderId\" IS NOT NULL")
                .HasDatabaseName("uq_quote_converted_so");

            builder.HasMany(x => x.Items)
                .WithOne(x => x.Quotation)
                .HasForeignKey(x => x.QuotationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class QuotationItemConfiguration : IEntityTypeConfiguration<QuotationItem>
    {
        public void Configure(EntityTypeBuilder<QuotationItem> builder)
        {
            builder.ToTable("quotation_items");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ItemCode).HasMaxLength(64);
            builder.Property(x => x.ItemName).HasMaxLength(512).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(2000);
            builder.Property(x => x.Unit).HasMaxLength(32);

            builder.Property(x => x.Quantity).HasPrecision(18, 4);
            builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
            builder.Property(x => x.DiscountPercent).HasPrecision(9, 4);
            builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            builder.Property(x => x.TaxPercent).HasPrecision(9, 4);
            builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
            builder.Property(x => x.LineTotal).HasPrecision(18, 2);

            builder.HasIndex(x => x.QuotationId);
            builder.HasIndex(x => x.ProductId);
        }
    }
}
