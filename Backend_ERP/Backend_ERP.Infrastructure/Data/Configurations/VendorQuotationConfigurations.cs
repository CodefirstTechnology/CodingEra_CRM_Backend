using ERP.Domain.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class VendorQuotationConfiguration : IEntityTypeConfiguration<VendorQuotation>
    {
        public void Configure(EntityTypeBuilder<VendorQuotation> builder)
        {
            builder.ToTable("vendor_quotations");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.QuotationNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.QuotationNumber).IsUnique();

            builder.Property(x => x.VendorName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.QuotationRef).HasMaxLength(128);
            builder.Property(x => x.LeadTime).HasMaxLength(128);
            builder.Property(x => x.WarrantyPeriod).HasMaxLength(128);
            builder.Property(x => x.PaymentTerms).HasMaxLength(500);

            builder.Property(x => x.SubTotal).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.DiscountPercent).HasPrecision(5, 2).HasDefaultValue(0m);
            builder.Property(x => x.DiscountAmount).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.TaxPercent).HasPrecision(5, 2).HasDefaultValue(0m);
            builder.Property(x => x.TaxAmount).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.TotalCost).HasPrecision(18, 4).HasDefaultValue(0m);

            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            builder.HasIndex(x => x.RequestForQuotationId);
            builder.HasIndex(x => x.VendorId);

            builder.HasOne(x => x.Vendor)
                .WithMany()
                .HasForeignKey(x => x.VendorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.RequestForQuotation)
                .WithMany()
                .HasForeignKey(x => x.RequestForQuotationId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(x => x.Lines)
                .WithOne(x => x.VendorQuotation)
                .HasForeignKey(x => x.VendorQuotationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class VendorQuotationLineConfiguration : IEntityTypeConfiguration<VendorQuotationLine>
    {
        public void Configure(EntityTypeBuilder<VendorQuotationLine> builder)
        {
            builder.ToTable("vendor_quotation_lines");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ItemName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Quantity).HasPrecision(18, 4).IsRequired();
            builder.Property(x => x.Uom).HasMaxLength(64).HasDefaultValue("PCS");
            builder.Property(x => x.UnitPrice).HasPrecision(18, 4).IsRequired();
            builder.Property(x => x.TaxPercent).HasPrecision(5, 2).HasDefaultValue(0m);
            builder.Property(x => x.DiscountPercent).HasPrecision(5, 2).HasDefaultValue(0m);
            builder.Property(x => x.Amount).HasPrecision(18, 4).HasDefaultValue(0m);

            builder.HasIndex(x => x.VendorQuotationId);
        }
    }

    public class VendorQuotationDocumentSequenceConfiguration : IEntityTypeConfiguration<VendorQuotationDocumentSequence>
    {
        public void Configure(EntityTypeBuilder<VendorQuotationDocumentSequence> builder)
        {
            builder.ToTable("vendor_quotation_document_sequences");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.Prefix).IsUnique();
        }
    }
}
