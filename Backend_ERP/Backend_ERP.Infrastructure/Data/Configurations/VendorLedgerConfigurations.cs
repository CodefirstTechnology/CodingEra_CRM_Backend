using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class VendorPaymentConfiguration : IEntityTypeConfiguration<VendorPayment>
    {
        public void Configure(EntityTypeBuilder<VendorPayment> builder)
        {
            builder.ToTable("vendor_payments");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.PaymentNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.PaymentNumber).IsUnique();

            builder.Property(x => x.PaymentMethod).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Status).HasMaxLength(32).HasDefaultValue("Cleared");
            builder.Property(x => x.ReferenceNumber).HasMaxLength(128);

            builder.Property(x => x.TotalAmount).HasPrecision(18, 4);
            builder.Property(x => x.UnallocatedAmount).HasPrecision(18, 4);

            builder.HasMany(x => x.Allocations)
                .WithOne(x => x.VendorPayment)
                .HasForeignKey(x => x.VendorPaymentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class VendorPaymentAllocationConfiguration : IEntityTypeConfiguration<VendorPaymentAllocation>
    {
        public void Configure(EntityTypeBuilder<VendorPaymentAllocation> builder)
        {
            builder.ToTable("vendor_payment_allocations");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.AllocatedAmount).HasPrecision(18, 4);

            builder.HasOne(x => x.VendorPayment)
                .WithMany(x => x.Allocations)
                .HasForeignKey(x => x.VendorPaymentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.PurchaseBill)
                .WithMany()
                .HasForeignKey(x => x.PurchaseBillId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.VendorPaymentId, x.PurchaseBillId }).HasDatabaseName("idx_payment_alloc_lookup");
        }
    }

    public class VendorLedgerEntryConfiguration : IEntityTypeConfiguration<VendorLedgerEntry>
    {
        public void Configure(EntityTypeBuilder<VendorLedgerEntry> builder)
        {
            builder.ToTable("vendor_ledger_entries");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.VoucherNumber).HasMaxLength(64).IsRequired();
            builder.Property(x => x.EntryType).HasMaxLength(32).IsRequired();
            builder.Property(x => x.ReferenceNumber).HasMaxLength(128);

            builder.Property(x => x.DebitAmount).HasPrecision(18, 4);
            builder.Property(x => x.CreditAmount).HasPrecision(18, 4);

            builder.HasIndex(x => new { x.VendorId, x.EntryDate, x.Id }).HasDatabaseName("idx_vendor_ledger_lookup");
        }
    }

    public class VendorAgingSummaryConfiguration : IEntityTypeConfiguration<VendorAgingSummaryDto>
    {
        public void Configure(EntityTypeBuilder<VendorAgingSummaryDto> builder)
        {
            builder.HasNoKey();
            builder.ToView("vw_vendor_aging_analysis");

            builder.Property(x => x.VendorId).HasColumnName("vendor_id");
            builder.Property(x => x.VendorName).HasColumnName("vendor_name");
            builder.Property(x => x.TotalOutstanding).HasColumnName("total_outstanding").HasPrecision(18, 4);
            builder.Property(x => x.CurrentAmount).HasColumnName("current_amount").HasPrecision(18, 4);
            builder.Property(x => x.Aging1To30).HasColumnName("aging_1_30").HasPrecision(18, 4);
            builder.Property(x => x.Aging31To60).HasColumnName("aging_31_60").HasPrecision(18, 4);
            builder.Property(x => x.Aging61To90).HasColumnName("aging_61_90").HasPrecision(18, 4);
            builder.Property(x => x.Aging90Plus).HasColumnName("aging_90_plus").HasPrecision(18, 4);
        }
    }
}
