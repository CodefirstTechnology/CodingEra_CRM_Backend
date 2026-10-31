using ERP.Domain.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class VendorConfiguration : IEntityTypeConfiguration<Vendor>
    {
        public void Configure(EntityTypeBuilder<Vendor> builder)
        {
            builder.ToTable("vendors");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired().HasDefaultValue("default");
            builder.Property(x => x.VendorCode).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
            builder.Property(x => x.LegalName).HasMaxLength(256);
            builder.Property(x => x.TradeName).HasMaxLength(256);
            builder.Property(x => x.DisplayName).HasMaxLength(256);
            builder.Property(x => x.VendorType).HasConversion<string>().HasMaxLength(64).IsRequired();

            builder.Property(x => x.GSTIN).HasColumnName("GSTIN").HasMaxLength(32);
            builder.Property(x => x.PAN).HasColumnName("PAN").HasMaxLength(32);
            builder.Ignore(x => x.Gstin);
            builder.Ignore(x => x.Pan);
            builder.Property(x => x.Tan).HasMaxLength(32);
            builder.Property(x => x.Cin).HasMaxLength(64);
            builder.Property(x => x.TaxIdentificationNumber).HasMaxLength(64);

            builder.Property(x => x.MsmeRegistrationNumber).HasMaxLength(64);
            builder.Property(x => x.MsmeCategory).HasConversion<string>().HasMaxLength(32).IsRequired();

            builder.Property(x => x.Email).HasMaxLength(256);
            builder.Property(x => x.Phone).HasMaxLength(64);
            builder.Property(x => x.AlternatePhone).HasMaxLength(64);
            builder.Property(x => x.Website).HasMaxLength(256);

            builder.Property(x => x.PaymentTermsDays).HasDefaultValue(30);
            builder.Property(x => x.Currency).HasMaxLength(16).IsRequired().HasDefaultValue("INR");
            builder.Property(x => x.CreditLimit).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.TotalOpenCommitment).HasPrecision(18, 2).HasDefaultValue(0.00m);
            builder.Property(x => x.Incoterms).HasMaxLength(32);

            builder.Property(x => x.RatingScore).HasPrecision(7, 2).HasDefaultValue(100.00m);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.ApprovalToken).HasMaxLength(128);
            builder.Property(x => x.Notes).HasMaxLength(2000);

            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            // PostgreSQL Concurrency Token mapped to system xmin
            builder.Property(x => x.ConcurrencyVersion).IsRowVersion();

            // Indexes
            builder.HasIndex(x => new { x.TenantId, x.VendorCode })
                   .IsUnique()
                   .HasDatabaseName("ix_vendors_tenant_code");

            builder.HasIndex(x => new { x.TenantId, x.GSTIN })
                   .HasDatabaseName("ix_vendors_tenant_gstin");

            builder.HasIndex(x => new { x.TenantId, x.PAN })
                   .HasDatabaseName("ix_vendors_tenant_pan");

            builder.HasIndex(x => new { x.TenantId, x.Status, x.RatingScore })
                   .HasDatabaseName("ix_vendors_tenant_status_rating");

            builder.HasIndex(x => new { x.TenantId, x.MsmeCategory, x.IsMsmeRegistered })
                   .HasDatabaseName("ix_vendors_tenant_msme_compliance");

            // Relationships
            builder.HasOne(x => x.PaymentTerm)
                   .WithMany()
                   .HasForeignKey(x => x.PaymentTermId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Contacts)
                   .WithOne(x => x.Vendor)
                   .HasForeignKey(x => x.VendorId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Addresses)
                   .WithOne(x => x.Vendor)
                   .HasForeignKey(x => x.VendorId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.BankAccounts)
                   .WithOne(x => x.Vendor)
                   .HasForeignKey(x => x.VendorId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.ItemCatalogs)
                   .WithOne(x => x.Vendor)
                   .HasForeignKey(x => x.VendorId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.PerformanceScores)
                   .WithOne(x => x.Vendor)
                   .HasForeignKey(x => x.VendorId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.ApprovalHistories)
                   .WithOne(x => x.Vendor)
                   .HasForeignKey(x => x.VendorId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Compliance)
                   .WithOne(x => x.Vendor)
                   .HasForeignKey<VendorCompliance>(x => x.VendorId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.StatusHistory)
                   .WithOne(x => x.Vendor)
                   .HasForeignKey(x => x.VendorId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class VendorContactConfiguration : IEntityTypeConfiguration<VendorContact>
    {
        public void Configure(EntityTypeBuilder<VendorContact> builder)
        {
            builder.ToTable("vendor_contacts");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired().HasDefaultValue("default");
            builder.Property(x => x.FirstName).HasMaxLength(128).IsRequired();
            builder.Property(x => x.LastName).HasMaxLength(128);
            builder.Property(x => x.Department).HasMaxLength(128);
            builder.Property(x => x.Designation).HasMaxLength(128);
            builder.Property(x => x.Email).HasMaxLength(256);
            builder.Property(x => x.Phone).HasMaxLength(64);
            builder.Property(x => x.AlternatePhone).HasMaxLength(64);
            builder.Property(x => x.Mobile).HasMaxLength(64);

            builder.HasIndex(x => new { x.TenantId, x.VendorId });
        }
    }

    public class VendorAddressConfiguration : IEntityTypeConfiguration<VendorAddress>
    {
        public void Configure(EntityTypeBuilder<VendorAddress> builder)
        {
            builder.ToTable("vendor_addresses");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.AddressType).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.AddressLine1).HasMaxLength(256).IsRequired();
            builder.Property(x => x.AddressLine2).HasMaxLength(256);
            builder.Property(x => x.City).HasMaxLength(128);
            builder.Property(x => x.State).HasMaxLength(128);
            builder.Property(x => x.PostalCode).HasMaxLength(32);
            builder.Property(x => x.Country).HasMaxLength(128).HasDefaultValue("India");

            builder.HasIndex(x => x.VendorId);
        }
    }

    public class VendorComplianceConfiguration : IEntityTypeConfiguration<VendorCompliance>
    {
        public void Configure(EntityTypeBuilder<VendorCompliance> builder)
        {
            builder.ToTable("vendor_compliances");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.GSTIN).HasColumnName("GSTIN").HasMaxLength(32);
            builder.Property(x => x.PAN).HasColumnName("PAN").HasMaxLength(32);
            builder.Property(x => x.TaxIdentificationNumber).HasMaxLength(64);
            builder.Property(x => x.MSMENumber).HasMaxLength(64);
            builder.Property(x => x.CertificateNumber).HasMaxLength(128);
            builder.Property(x => x.ComplianceStatus).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Remarks).HasMaxLength(1000);

            builder.HasIndex(x => x.VendorId).IsUnique();
        }
    }

    public class VendorPaymentTermConfiguration : IEntityTypeConfiguration<VendorPaymentTerm>
    {
        public void Configure(EntityTypeBuilder<VendorPaymentTerm> builder)
        {
            builder.ToTable("vendor_payment_terms");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.Code).IsUnique();

            builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(500);
            builder.Property(x => x.AdvancePercentage).HasPrecision(5, 2).HasDefaultValue(0m);
        }
    }

    public class VendorStatusHistoryConfiguration : IEntityTypeConfiguration<VendorStatusHistory>
    {
        public void Configure(EntityTypeBuilder<VendorStatusHistory> builder)
        {
            builder.ToTable("vendor_status_histories");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.PreviousStatus).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.User).HasMaxLength(256);
            builder.Property(x => x.Remarks).HasMaxLength(2000);

            builder.HasIndex(x => new { x.VendorId, x.Date });
        }
    }

    public class VendorDocumentSequenceConfiguration : IEntityTypeConfiguration<VendorDocumentSequence>
    {
        public void Configure(EntityTypeBuilder<VendorDocumentSequence> builder)
        {
            builder.ToTable("vendor_document_sequences");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.Prefix).IsUnique();
        }
    }

    public class VendorBankAccountConfiguration : IEntityTypeConfiguration<VendorBankAccount>
    {
        public void Configure(EntityTypeBuilder<VendorBankAccount> builder)
        {
            builder.ToTable("vendor_bank_accounts");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired().HasDefaultValue("default");
            builder.Property(x => x.AccountNumber).HasMaxLength(64).IsRequired();
            builder.Property(x => x.IfscCode).HasMaxLength(32).IsRequired();
            builder.Property(x => x.BankName).HasMaxLength(128).IsRequired();
            builder.Property(x => x.Branch).HasMaxLength(128).IsRequired();
            builder.Property(x => x.BeneficiaryName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.AccountType).HasMaxLength(32).HasDefaultValue("Current");
            builder.Property(x => x.VerificationStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
            builder.Property(x => x.CancelledChequeUrl).HasMaxLength(1024);
            builder.Property(x => x.VerifiedBy).HasMaxLength(128);

            builder.HasIndex(x => new { x.TenantId, x.VendorId, x.AccountNumber })
                   .IsUnique()
                   .HasDatabaseName("ix_vendor_banks_tenant_vendor_account");
        }
    }

    public class VendorItemCatalogConfiguration : IEntityTypeConfiguration<VendorItemCatalog>
    {
        public void Configure(EntityTypeBuilder<VendorItemCatalog> builder)
        {
            builder.ToTable("vendor_item_catalogs");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired().HasDefaultValue("default");
            builder.Property(x => x.ItemType).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ItemCode).HasMaxLength(64).IsRequired();
            builder.Property(x => x.SupplierPartNumber).HasMaxLength(128);
            builder.Property(x => x.StandardLeadTimeDays).IsRequired();
            builder.Property(x => x.MinimumOrderQuantity).HasPrecision(18, 4).HasDefaultValue(1.0000m);
            builder.Property(x => x.OrderMultiple).HasPrecision(18, 4).HasDefaultValue(1.0000m);
            builder.Property(x => x.PackagingUnit).HasMaxLength(32).IsRequired();
            builder.Property(x => x.BaseUomConversionMultiplier).HasPrecision(18, 6).HasDefaultValue(1.000000m);
            builder.Property(x => x.UnitPriceSnapshot).HasPrecision(18, 4).IsRequired();
            builder.Property(x => x.Currency).HasMaxLength(16).IsRequired().HasDefaultValue("INR");
            builder.Property(x => x.TechnicalRemarks).HasMaxLength(1000);

            builder.HasIndex(x => new { x.TenantId, x.VendorId, x.ItemCode, x.EffectiveFrom })
                   .IsUnique()
                   .HasDatabaseName("ix_vendor_catalog_tenant_vendor_item_effective");

            builder.HasIndex(x => new { x.TenantId, x.ItemCode, x.IsApproved })
                   .HasDatabaseName("ix_vendor_catalog_tenant_avl_lookup");
        }
    }

    public class VendorPerformanceScoreConfiguration : IEntityTypeConfiguration<VendorPerformanceScore>
    {
        public void Configure(EntityTypeBuilder<VendorPerformanceScore> builder)
        {
            builder.ToTable("vendor_performance_scores");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired().HasDefaultValue("default");
            builder.Property(x => x.PeriodKey).HasMaxLength(32).IsRequired();
            builder.Property(x => x.OtifPercentage).HasPrecision(7, 2);
            builder.Property(x => x.QualityPassRate).HasPrecision(7, 2);
            builder.Property(x => x.PriceVarianceScore).HasPrecision(7, 2);
            builder.Property(x => x.ComplianceScore).HasPrecision(7, 2);
            builder.Property(x => x.BlendedRatingScore).HasPrecision(7, 2);
            builder.Property(x => x.DefectPartsPerMillion).HasPrecision(12, 2);

            builder.HasIndex(x => new { x.TenantId, x.VendorId, x.PeriodKey })
                   .IsUnique()
                   .HasDatabaseName("ix_vendor_perf_tenant_vendor_period");
        }
    }

    public class VendorApprovalHistoryConfiguration : IEntityTypeConfiguration<VendorApprovalHistory>
    {
        public void Configure(EntityTypeBuilder<VendorApprovalHistory> builder)
        {
            builder.ToTable("vendor_approval_histories");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired().HasDefaultValue("default");
            builder.Property(x => x.PreviousStatus).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.NewStatus).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Action).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Comments).HasMaxLength(2000);
            builder.Property(x => x.ActionedByUserId).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ActionedByUserName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.ApprovalToken).HasMaxLength(128);

            builder.HasIndex(x => new { x.TenantId, x.VendorId, x.ActionedAt })
                   .HasDatabaseName("ix_vendor_approvals_tenant_vendor_date");
        }
    }
}
