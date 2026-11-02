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

            builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired().HasDefaultValue("TENANT-DEFAULT");
            builder.Property(x => x.EntryNumber).HasMaxLength(64).IsRequired().HasDefaultValue("");
            builder.Property(x => x.VoucherNumber).HasMaxLength(64).IsRequired();
            builder.Property(x => x.VoucherType).HasMaxLength(32).IsRequired();
            builder.Property(x => x.ReferenceDocType).HasMaxLength(32);
            builder.Property(x => x.ReferenceDocNumber).HasMaxLength(128);

            builder.Property(x => x.Currency).HasMaxLength(3).IsRequired().HasDefaultValue("INR");
            builder.Property(x => x.ExchangeRate).HasPrecision(18, 6).HasDefaultValue(1.000000m);
            builder.Property(x => x.ForeignDebitAmount).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.ForeignCreditAmount).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.DebitAmount).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.CreditAmount).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.RunningBalance).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.SettledAmount).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.OutstandingAmount).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.SettlementStatus).HasMaxLength(32).HasDefaultValue("Unsettled");

            builder.Property(x => x.RowVersion).IsRowVersion(); // xmin token in PostgreSQL

            builder.HasOne(x => x.Vendor)
                .WithMany()
                .HasForeignKey(x => x.VendorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.PurchaseBill)
                .WithMany()
                .HasForeignKey(x => x.PurchaseBillId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(x => new { x.TenantId, x.VendorId, x.PostingDate, x.Id })
                .HasDatabaseName("idx_vendor_ledger_vendor_date");

            builder.HasIndex(x => new { x.TenantId, x.VendorId, x.SettlementStatus })
                .HasDatabaseName("idx_vendor_ledger_unsettled");
        }
    }

    public class VendorBillSettlementConfiguration : IEntityTypeConfiguration<VendorBillSettlement>
    {
        public void Configure(EntityTypeBuilder<VendorBillSettlement> builder)
        {
            builder.ToTable("vendor_bill_settlements");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired().HasDefaultValue("TENANT-DEFAULT");
            builder.Property(x => x.AllocatedAmount).HasPrecision(18, 4);
            builder.Property(x => x.RealizedFxGainLoss).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.AllocationNotes).HasMaxLength(256);

            builder.HasOne(x => x.PurchaseBill)
                .WithMany()
                .HasForeignKey(x => x.PurchaseBillId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.PaymentVoucher)
                .WithMany()
                .HasForeignKey(x => x.PaymentVoucherId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.TenantId, x.PurchaseBillId, x.PaymentVoucherId })
                .HasDatabaseName("idx_bill_settlement_lookup");
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
