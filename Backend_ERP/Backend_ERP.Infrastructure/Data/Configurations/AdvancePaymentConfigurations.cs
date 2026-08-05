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

            builder.HasOne(x => x.SalesOrder)
                .WithMany()
                .HasForeignKey(x => x.SalesOrderId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(x => x.Applications)
                .WithOne(x => x.AdvancePayment)
                .HasForeignKey(x => x.AdvancePaymentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Timeline)
                .WithOne(x => x.AdvancePayment)
                .HasForeignKey(x => x.AdvancePaymentId)
                .OnDelete(DeleteBehavior.Cascade);
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
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.AppliedBy).HasMaxLength(64);

            builder.HasIndex(x => new { x.AdvancePaymentId, x.SalesOrderId }).IsUnique();
            builder.HasIndex(x => x.SalesOrderId);
            builder.HasIndex(x => x.AppliedOn);
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
