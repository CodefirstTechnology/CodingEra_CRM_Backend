using ERP.Domain.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class VendorComparisonConfiguration : IEntityTypeConfiguration<VendorComparison>
    {
        public void Configure(EntityTypeBuilder<VendorComparison> builder)
        {
            builder.ToTable("vendor_comparisons");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TenantId).HasDefaultValue(1);
            builder.Property(x => x.ComparisonNumber).HasMaxLength(64).IsRequired();
            builder.Property(x => x.RFQNumber).HasMaxLength(64);
            builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
            builder.Property(x => x.RecommendationNotes).HasMaxLength(2000);
            builder.Property(x => x.SelectedWinnerVendorName).HasMaxLength(256);
            builder.Property(x => x.PurchaseOrderNumber).HasMaxLength(64);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();

            builder.Property(x => x.IsExecutiveOverride).HasDefaultValue(false);
            builder.Property(x => x.ExecutiveOverrideReasonCode).HasMaxLength(64).HasDefaultValue(string.Empty);
            builder.Property(x => x.ExecutiveOverrideJustification).HasMaxLength(2000).HasDefaultValue(string.Empty);
            builder.Property(x => x.IsSplitAward).HasDefaultValue(false);
            builder.Property(x => x.SplitAllocationJson).HasColumnType("jsonb").HasDefaultValue("[]");
            builder.Property(x => x.EvaluatedByUserId).HasMaxLength(128).HasDefaultValue(string.Empty);

            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            builder.Property(x => x.ConcurrencyVersion).IsRowVersion();

            builder.HasIndex(x => new { x.TenantId, x.ComparisonNumber }).IsUnique().HasDatabaseName("ix_comparison_tenant_number");
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.RFQId);

            builder.HasOne(x => x.RFQ)
                .WithMany()
                .HasForeignKey(x => x.RFQId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(x => x.Entries)
                .WithOne(x => x.VendorComparison)
                .HasForeignKey(x => x.VendorComparisonId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.History)
                .WithOne(x => x.VendorComparison)
                .HasForeignKey(x => x.VendorComparisonId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class VendorComparisonEntryConfiguration : IEntityTypeConfiguration<VendorComparisonEntry>
    {
        public void Configure(EntityTypeBuilder<VendorComparisonEntry> builder)
        {
            builder.ToTable("vendor_comparison_entries");
            builder.HasKey(x => x.Id);
            builder.Ignore(x => x.Lines);

            builder.Property(x => x.TenantId).HasDefaultValue(1);
            builder.Property(x => x.VendorName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.QuotationRef).HasMaxLength(128);
            builder.Property(x => x.LeadTime).HasMaxLength(128);
            builder.Property(x => x.WarrantyPeriod).HasMaxLength(128);
            builder.Property(x => x.PaymentTerms).HasMaxLength(500);

            builder.Property(x => x.UnitPrice).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.TaxPercent).HasPrecision(5, 2).HasDefaultValue(0m);
            builder.Property(x => x.DiscountPercent).HasPrecision(5, 2).HasDefaultValue(0m);
            builder.Property(x => x.TotalCost).HasPrecision(18, 2).HasDefaultValue(0m);
            builder.Property(x => x.TotalLandedCost).HasPrecision(18, 2).HasDefaultValue(0m);

            builder.Property(x => x.LandedCostScore).HasDefaultValue(0.0);
            builder.Property(x => x.LeadTimeScore).HasDefaultValue(0.0);
            builder.Property(x => x.QualityScore).HasDefaultValue(0.0);
            builder.Property(x => x.PaymentTermsScore).HasDefaultValue(0.0);
            builder.Property(x => x.BlendedMCDAScore).HasDefaultValue(0.0);

            builder.Property(x => x.IsAwardee).HasDefaultValue(false);
            builder.Property(x => x.AwardedQuantityPercentage).HasPrecision(5, 2).HasDefaultValue(0m);

            builder.Property(x => x.Remarks).HasMaxLength(2000);

            builder.HasIndex(x => x.VendorComparisonId);
            builder.HasIndex(x => x.VendorId);

            builder.HasOne(x => x.Vendor)
                .WithMany()
                .HasForeignKey(x => x.VendorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.VendorQuotation)
                .WithMany()
                .HasForeignKey(x => x.VendorQuotationId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

    public class VendorComparisonLineConfiguration : IEntityTypeConfiguration<VendorComparisonLine>
    {
        public void Configure(EntityTypeBuilder<VendorComparisonLine> builder)
        {
            builder.ToTable("vendor_comparison_lines");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ItemName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Quantity).HasPrecision(18, 4).IsRequired();
            builder.Property(x => x.Uom).HasMaxLength(64).HasDefaultValue("PCS");
            builder.Property(x => x.LowestUnitPrice).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.LowestVendorName).HasMaxLength(256);
            builder.Property(x => x.Remarks).HasMaxLength(2000);

            builder.HasIndex(x => x.VendorComparisonId);
        }
    }

    public class VendorComparisonStatusHistoryConfiguration : IEntityTypeConfiguration<VendorComparisonStatusHistory>
    {
        public void Configure(EntityTypeBuilder<VendorComparisonStatusHistory> builder)
        {
            builder.ToTable("vendor_comparison_status_histories");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.PreviousStatus).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.User).HasMaxLength(256);
            builder.Property(x => x.Remarks).HasMaxLength(2000);

            builder.HasIndex(x => new { x.VendorComparisonId, x.Date });
        }
    }

    public class VendorComparisonDocumentSequenceConfiguration : IEntityTypeConfiguration<VendorComparisonDocumentSequence>
    {
        public void Configure(EntityTypeBuilder<VendorComparisonDocumentSequence> builder)
        {
            builder.ToTable("vendor_comparison_document_sequences");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.Prefix).IsUnique();
        }
    }
}
