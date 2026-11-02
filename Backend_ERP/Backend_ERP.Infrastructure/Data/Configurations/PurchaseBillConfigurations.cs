using ERP.Domain.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class PurchaseBillConfiguration : IEntityTypeConfiguration<PurchaseBill>
    {
        public void Configure(EntityTypeBuilder<PurchaseBill> builder)
        {
            builder.ToTable("purchase_bills");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.BillNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.BillNumber).IsUnique();

            builder.Property(x => x.InvoiceNumber).HasMaxLength(128).IsRequired();
            builder.Property(x => x.VendorName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.PurchaseOrderNumber).HasMaxLength(64);
            builder.Property(x => x.GRNNumber).HasMaxLength(64);
            builder.Property(x => x.ConsolidatedGrnIds).HasMaxLength(2000);
            builder.Property(x => x.Currency).HasMaxLength(16);
            builder.Property(x => x.PaymentTerms).HasMaxLength(128);

            builder.Property(x => x.SubTotal).HasPrecision(18, 4);
            builder.Property(x => x.DiscountTotal).HasPrecision(18, 4);
            builder.Property(x => x.TaxTotal).HasPrecision(18, 4);
            builder.Property(x => x.RoundOff).HasPrecision(18, 4);
            builder.Property(x => x.GrandTotal).HasPrecision(18, 4);
            builder.Property(x => x.PaidAmount).HasPrecision(18, 4);
            builder.Property(x => x.BalanceAmount).HasPrecision(18, 4);

            builder.Property(x => x.CgstAmount).HasPrecision(18, 4);
            builder.Property(x => x.SgstAmount).HasPrecision(18, 4);
            builder.Property(x => x.IgstAmount).HasPrecision(18, 4);

            // 3-Way Match Attributes
            builder.Property(x => x.ThreeWayMatchStatus).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.MatchedAmount).HasPrecision(18, 2);
            builder.Property(x => x.VarianceAmount).HasPrecision(18, 2);
            builder.Property(x => x.VarianceOverrideReason).HasMaxLength(2000);
            builder.Property(x => x.VarianceOverriddenBy).HasMaxLength(256);

            // Statutory MSME 43B(h) & TDS 194Q
            builder.Property(x => x.Tds194QPercentage).HasPrecision(5, 2);
            builder.Property(x => x.Tds194QAmount).HasPrecision(18, 2);
            builder.Property(x => x.TdsSection).HasMaxLength(64);
            builder.Property(x => x.TdsPercentage).HasPrecision(5, 2);
            builder.Property(x => x.TdsAmount).HasPrecision(18, 2);

            // GSTR-2B
            builder.Property(x => x.Gstr2BStatus).HasMaxLength(64);
            builder.Property(x => x.Gstr2BRef).HasMaxLength(128);

            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.VarianceReason).HasMaxLength(2000);
            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            builder.Property(x => x.RowVersion).IsRowVersion();

            builder.Ignore(x => x.Vendor);
            builder.Ignore(x => x.VendorInvoiceNumber);
            builder.Ignore(x => x.ExchangeRate);
            builder.Ignore(x => x.PaymentTermsDays);
            builder.Ignore(x => x.TotalAmount);
            builder.Ignore(x => x.OutstandingAmount);

            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.PaymentStatus);
            builder.HasIndex(x => x.ThreeWayMatchStatus);
            builder.HasIndex(x => x.GRNId);
            builder.HasIndex(x => x.PurchaseOrderId);
            builder.HasIndex(x => x.VendorId);
            builder.HasIndex(x => new { x.VendorId, x.Status, x.DueDate });
            builder.HasIndex(x => new { x.PurchaseOrderId, x.GRNId, x.Status });
            builder.HasIndex(x => new { x.IsMsmeSubjectTo43B, x.DueDate, x.PaymentStatus });

            builder.HasMany(x => x.Lines)
                .WithOne(x => x.PurchaseBill)
                .HasForeignKey(x => x.PurchaseBillId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.History)
                .WithOne(x => x.PurchaseBill)
                .HasForeignKey(x => x.PurchaseBillId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class PurchaseBillLineConfiguration : IEntityTypeConfiguration<PurchaseBillLine>
    {
        public void Configure(EntityTypeBuilder<PurchaseBillLine> builder)
        {
            builder.ToTable("purchase_bill_lines");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ItemName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(500);

            builder.Property(x => x.Quantity).HasPrecision(18, 4);
            builder.Property(x => x.UnitPrice).HasPrecision(18, 4);
            builder.Property(x => x.DiscountAmount).HasPrecision(18, 4);
            builder.Property(x => x.TaxPercent).HasPrecision(18, 4);
            builder.Property(x => x.TaxAmount).HasPrecision(18, 4);
            builder.Property(x => x.TotalAmount).HasPrecision(18, 4);

            // 3-Way Match Line fields
            builder.Property(x => x.PoRate).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.GrnAcceptedQuantity).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.RateVariance).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.QuantityVariance).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.TaxVariance).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.LineMatchStatus).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.MatchRemarks).HasMaxLength(1000);

            builder.HasIndex(x => x.PurchaseBillId);
            builder.HasIndex(x => x.GoodsReceiptId);
            builder.HasIndex(x => x.GoodsReceiptItemId);

            builder.HasOne(x => x.GoodsReceipt)
                .WithMany()
                .HasForeignKey(x => x.GoodsReceiptId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.GoodsReceiptItem)
                .WithMany()
                .HasForeignKey(x => x.GoodsReceiptItemId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

    public class ThreeWayMatchAuditLogConfiguration : IEntityTypeConfiguration<ThreeWayMatchAuditLog>
    {
        public void Configure(EntityTypeBuilder<ThreeWayMatchAuditLog> builder)
        {
            builder.ToTable("three_way_match_audit_logs");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.BillNumber).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ItemName).HasMaxLength(256);
            builder.Property(x => x.CheckType).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Status).HasMaxLength(64).IsRequired();

            builder.Property(x => x.PoValue).HasPrecision(18, 4);
            builder.Property(x => x.GrnValue).HasPrecision(18, 4);
            builder.Property(x => x.BillValue).HasPrecision(18, 4);
            builder.Property(x => x.Variance).HasPrecision(18, 4);
            builder.Property(x => x.VariancePercentage).HasPrecision(18, 4);

            builder.Property(x => x.DiscrepancyMessage).HasMaxLength(2000);
            builder.Property(x => x.OverriddenBy).HasMaxLength(256);
            builder.Property(x => x.OverrideReason).HasMaxLength(2000);

            builder.HasIndex(x => new { x.PurchaseBillId, x.CheckType, x.CheckedAt });
            builder.HasIndex(x => x.BillNumber);
        }
    }

    public class PurchaseBillHistoryConfiguration : IEntityTypeConfiguration<PurchaseBillHistory>
    {
        public void Configure(EntityTypeBuilder<PurchaseBillHistory> builder)
        {
            builder.ToTable("purchase_bill_histories");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.User).HasMaxLength(256);
            builder.Property(x => x.Remarks).HasMaxLength(2000);

            builder.HasIndex(x => x.PurchaseBillId);
        }
    }

    public class PurchaseBillDocumentSequenceConfiguration : IEntityTypeConfiguration<PurchaseBillDocumentSequence>
    {
        public void Configure(EntityTypeBuilder<PurchaseBillDocumentSequence> builder)
        {
            builder.ToTable("purchase_bill_document_sequences");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.Prefix).IsUnique();
        }
    }
}
