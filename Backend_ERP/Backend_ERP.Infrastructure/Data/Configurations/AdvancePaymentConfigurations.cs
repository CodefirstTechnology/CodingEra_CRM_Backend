using ERP.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class AdvancePaymentConfiguration : IEntityTypeConfiguration<AdvancePayment>
    {
        public void Configure(EntityTypeBuilder<AdvancePayment> builder)
        {
            builder.ToTable("advance_payments");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.PaymentNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.PaymentNumber).IsUnique();

            builder.Property(x => x.CustomerId).HasMaxLength(64).IsRequired();
            builder.Property(x => x.CustomerName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.SalesOrderNumber).HasMaxLength(64);
            builder.Property(x => x.QuotationNumber).HasMaxLength(64);
            builder.Property(x => x.PaymentMode).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ReferenceNumber).HasMaxLength(128).IsRequired();
            builder.Property(x => x.Currency).HasMaxLength(8).IsRequired();
            builder.Property(x => x.ExchangeRate).HasPrecision(18, 6);
            builder.Property(x => x.AdvanceAmount).HasPrecision(18, 2);
            builder.Property(x => x.AppliedAmount).HasPrecision(18, 2);
            builder.Property(x => x.RemainingAmount).HasPrecision(18, 2);
            builder.Property(x => x.RefundedAmount).HasPrecision(18, 2).HasDefaultValue(0m);
            builder.Property(x => x.ForfeitedAmount).HasPrecision(18, 2).HasDefaultValue(0m);
            builder.Property(x => x.RefundReferenceNumber).HasMaxLength(64);
            builder.Property(x => x.RefundProcessedBy).HasMaxLength(100);
            builder.Property(x => x.ReconciliationStatus).HasMaxLength(32).HasDefaultValue("Unreconciled");
            builder.Property(x => x.BankStatementReference).HasMaxLength(100);
            builder.Property(x => x.PlaceOfSupply).HasMaxLength(50).HasDefaultValue("Maharashtra");
            builder.Property(x => x.Status).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.AttachmentName).HasMaxLength(512);
            builder.Property(x => x.VerifiedBy).HasMaxLength(64);
            builder.Property(x => x.ReceivedBy).HasMaxLength(64);
            builder.Property(x => x.CreatedBy).HasMaxLength(64);
            builder.Property(x => x.UpdatedBy).HasMaxLength(64);

            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.PaymentDate);
            builder.HasIndex(x => x.CustomerName);
            builder.HasIndex(x => x.IsDeleted);
            builder.HasIndex(x => x.SalesOrderId);
            builder.HasIndex(x => x.BankAccountId);

            builder.HasOne(x => x.SalesOrder)
                .WithMany()
                .HasForeignKey(x => x.SalesOrderId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.BankAccount)
                .WithMany()
                .HasForeignKey(x => x.BankAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Applications)
                .WithOne(x => x.AdvancePayment)
                .HasForeignKey(x => x.AdvancePaymentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Timeline)
                .WithOne(x => x.AdvancePayment)
                .HasForeignKey(x => x.AdvancePaymentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.ReceiptVouchers)
                .WithOne(x => x.AdvancePayment)
                .HasForeignKey(x => x.AdvancePaymentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.RefundVouchers)
                .WithOne(x => x.AdvancePayment)
                .HasForeignKey(x => x.AdvancePaymentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class AdvancePaymentApplicationConfiguration : IEntityTypeConfiguration<AdvancePaymentApplication>
    {
        public void Configure(EntityTypeBuilder<AdvancePaymentApplication> builder)
        {
            builder.ToTable("advance_payment_applications");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.SalesOrderNumber).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ApplyAmount).HasPrecision(18, 2);
            builder.Property(x => x.ExchangeRateAtAllocation).HasPrecision(18, 4).HasDefaultValue(1.0000m);
            builder.Property(x => x.RealizedFxGainLoss).HasPrecision(18, 2).HasDefaultValue(0.00m);
            builder.Property(x => x.IsReversal).HasDefaultValue(false);
            builder.Property(x => x.ReversalReason).HasMaxLength(2000);
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.AppliedBy).HasMaxLength(64);

            builder.HasIndex(x => new { x.AdvancePaymentId, x.SalesOrderId });
            builder.HasIndex(x => x.SalesOrderId);
            builder.HasIndex(x => x.AppliedOn);
            builder.HasIndex(x => x.OriginalApplicationId);

            builder.HasOne(x => x.OriginalApplication)
                .WithMany()
                .HasForeignKey(x => x.OriginalApplicationId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class AdvancePaymentReceiptVoucherConfiguration : IEntityTypeConfiguration<AdvancePaymentReceiptVoucher>
    {
        public void Configure(EntityTypeBuilder<AdvancePaymentReceiptVoucher> builder)
        {
            builder.ToTable("advance_payment_receipt_vouchers");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.VoucherNumber).HasMaxLength(32).IsRequired();
            builder.HasIndex(x => x.VoucherNumber).IsUnique();

            builder.Property(x => x.CustomerName).HasMaxLength(200);
            builder.Property(x => x.PlaceOfSupply).HasMaxLength(50).IsRequired();
            builder.Property(x => x.TaxableAmount).HasPrecision(18, 2);
            builder.Property(x => x.CgstRate).HasPrecision(5, 2);
            builder.Property(x => x.CgstAmount).HasPrecision(18, 2);
            builder.Property(x => x.SgstRate).HasPrecision(5, 2);
            builder.Property(x => x.SgstAmount).HasPrecision(18, 2);
            builder.Property(x => x.IgstRate).HasPrecision(5, 2);
            builder.Property(x => x.IgstAmount).HasPrecision(18, 2);
            builder.Property(x => x.TotalVoucherAmount).HasPrecision(18, 2);
            builder.Property(x => x.CreatedBy).HasMaxLength(100).IsRequired();

            builder.HasIndex(x => x.AdvancePaymentId);
        }
    }

    public class AdvancePaymentRefundVoucherConfiguration : IEntityTypeConfiguration<AdvancePaymentRefundVoucher>
    {
        public void Configure(EntityTypeBuilder<AdvancePaymentRefundVoucher> builder)
        {
            builder.ToTable("advance_payment_refund_vouchers");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.RefundVoucherNumber).HasMaxLength(32).IsRequired();
            builder.HasIndex(x => x.RefundVoucherNumber).IsUnique();

            builder.Property(x => x.RefundAmount).HasPrecision(18, 2);
            builder.Property(x => x.TaxRefundedAmount).HasPrecision(18, 2);
            builder.Property(x => x.BankReferenceNumber).HasMaxLength(64).IsRequired();
            builder.Property(x => x.CreatedBy).HasMaxLength(100).IsRequired();

            builder.HasIndex(x => x.AdvancePaymentId);
            builder.HasIndex(x => x.ReceiptVoucherId);

            builder.HasOne(x => x.ReceiptVoucher)
                .WithMany()
                .HasForeignKey(x => x.ReceiptVoucherId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class BankAccountConfiguration : IEntityTypeConfiguration<ERP.Domain.Accounting.BankAccount>
    {
        public void Configure(EntityTypeBuilder<ERP.Domain.Accounting.BankAccount> builder)
        {
            builder.ToTable("bank_accounts");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.AccountName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.AccountNumber).HasMaxLength(50).IsRequired();
            builder.Property(x => x.BankName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Branch).HasMaxLength(100);
            builder.Property(x => x.IfscCode).HasMaxLength(20);
            builder.Property(x => x.AccountType).HasMaxLength(30);
            builder.Property(x => x.Currency).HasMaxLength(10);
        }
    }

    public class AdvancePaymentTimelineConfiguration : IEntityTypeConfiguration<AdvancePaymentTimeline>
    {
        public void Configure(EntityTypeBuilder<AdvancePaymentTimeline> builder)
        {
            builder.ToTable("advance_payment_timeline");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Action).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.PerformedBy).HasMaxLength(64);

            builder.HasIndex(x => new { x.AdvancePaymentId, x.PerformedOn });
        }
    }

    public class AdvancePaymentDocumentSequenceConfiguration : IEntityTypeConfiguration<AdvancePaymentDocumentSequence>
    {
        public void Configure(EntityTypeBuilder<AdvancePaymentDocumentSequence> builder)
        {
            builder.ToTable("advance_payment_document_sequences");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Prefix).HasMaxLength(16).IsRequired();
            builder.HasIndex(x => new { x.FinancialYear, x.Prefix }).IsUnique();
        }
    }
}
