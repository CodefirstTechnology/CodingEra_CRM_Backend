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

            builder.Property(x => x.VendorCode).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.VendorCode).IsUnique();

            builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
            builder.Property(x => x.LegalName).HasMaxLength(256);
            builder.Property(x => x.DisplayName).HasMaxLength(256);

            builder.Property(x => x.GSTIN).HasMaxLength(32);
            builder.Property(x => x.PAN).HasMaxLength(32);
            builder.Property(x => x.TaxIdentificationNumber).HasMaxLength(64);

            builder.Property(x => x.Email).HasMaxLength(256);
            builder.Property(x => x.Phone).HasMaxLength(64);
            builder.Property(x => x.AlternatePhone).HasMaxLength(64);
            builder.Property(x => x.Website).HasMaxLength(256);

            builder.Property(x => x.CreditLimit).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Notes).HasMaxLength(2000);

            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            builder.HasIndex(x => x.Name);
            builder.HasIndex(x => x.GSTIN);
            builder.HasIndex(x => x.PAN);
            builder.HasIndex(x => x.Status);

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

            builder.Property(x => x.FirstName).HasMaxLength(128).IsRequired();
            builder.Property(x => x.LastName).HasMaxLength(128);
            builder.Property(x => x.Designation).HasMaxLength(128);
            builder.Property(x => x.Email).HasMaxLength(256);
            builder.Property(x => x.Phone).HasMaxLength(64);
            builder.Property(x => x.AlternatePhone).HasMaxLength(64);

            builder.HasIndex(x => x.VendorId);
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

            builder.Property(x => x.GSTIN).HasMaxLength(32);
            builder.Property(x => x.PAN).HasMaxLength(32);
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
}
