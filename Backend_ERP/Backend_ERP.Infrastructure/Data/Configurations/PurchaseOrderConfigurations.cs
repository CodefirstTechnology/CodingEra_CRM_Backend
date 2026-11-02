using ERP.Domain.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
    {
        public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
        {
            builder.ToTable("purchase_orders");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TenantId).HasDefaultValue(1);
            builder.Property(x => x.PurchaseOrderNumber).HasMaxLength(64).IsRequired();
            builder.Property(x => x.RevisionNumber).HasDefaultValue(0);
            builder.Property(x => x.IsCurrentRevision).HasDefaultValue(true);
            builder.Property(x => x.ReferenceNumber).HasMaxLength(128);

            // Vendor Snapshot
            builder.Property(x => x.VendorCode).HasMaxLength(32).HasDefaultValue(string.Empty);
            builder.Property(x => x.VendorName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.VendorGstin).HasMaxLength(32).HasDefaultValue(string.Empty);
            builder.Property(x => x.VendorStateCode).HasMaxLength(8).HasDefaultValue(string.Empty);
            builder.Property(x => x.VendorPan).HasMaxLength(16).HasDefaultValue(string.Empty);
            builder.Property(x => x.VendorContact).HasMaxLength(256);
            builder.Property(x => x.VendorEmail).HasMaxLength(256);
            builder.Property(x => x.VendorPhone).HasMaxLength(64);
            builder.Property(x => x.BillingAddress).HasMaxLength(2000);
            builder.Property(x => x.ShippingAddress).HasMaxLength(2000);

            // Terms & Multi-Currency
            builder.Property(x => x.PaymentTermsDays).HasDefaultValue(30);
            builder.Property(x => x.PaymentTerms).HasMaxLength(500);
            builder.Property(x => x.Incoterms).HasMaxLength(64).HasDefaultValue("FOR Destination");
            builder.Property(x => x.DeliveryTerms).HasMaxLength(500);
            builder.Property(x => x.Currency).HasMaxLength(10).HasDefaultValue("INR");
            builder.Property(x => x.ExchangeRate).HasPrecision(18, 6).HasDefaultValue(1.000000m);

            // Financial Totals (precision 18,2)
            builder.Property(x => x.Subtotal).HasPrecision(18, 2).HasDefaultValue(0m);
            builder.Property(x => x.DiscountTotal).HasPrecision(18, 2).HasDefaultValue(0m);
            builder.Property(x => x.TotalCgstAmount).HasPrecision(18, 2).HasDefaultValue(0m);
            builder.Property(x => x.TotalSgstAmount).HasPrecision(18, 2).HasDefaultValue(0m);
            builder.Property(x => x.TotalIgstAmount).HasPrecision(18, 2).HasDefaultValue(0m);
            builder.Property(x => x.TaxTotal).HasPrecision(18, 2).HasDefaultValue(0m);
            builder.Property(x => x.FreightAmount).HasPrecision(18, 2).HasDefaultValue(0m);
            builder.Property(x => x.InsuranceAmount).HasPrecision(18, 2).HasDefaultValue(0m);
            builder.Property(x => x.TotalAmount).HasPrecision(18, 2).HasDefaultValue(0m);
            builder.Property(x => x.GrandTotalBaseCurrency).HasPrecision(18, 2).HasDefaultValue(0m);

            // Governance & JSONB
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Priority).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.SourceType).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.RequiredApprovalTier).HasConversion<int>().IsRequired();
            builder.Property(x => x.CurrentApprovalTier).HasConversion<int>().IsRequired();
            builder.Property(x => x.ExceptionReasonsJson).HasColumnType("jsonb").HasDefaultValue("[]");
            builder.Property(x => x.ApprovalToken).HasMaxLength(256);
            builder.Property(x => x.ApprovedByUserId).HasMaxLength(128);
            builder.Property(x => x.AmendmentReason).HasMaxLength(1000);
            builder.Property(x => x.AmendmentType).HasConversion<string>().HasMaxLength(64);

            // Concurrency Version
            builder.Property(x => x.ConcurrencyVersion).IsRowVersion();

            // Self-referencing revision hierarchy
            builder.HasOne(x => x.ParentPO)
                .WithMany()
                .HasForeignKey(x => x.ParentPOId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Vendor)
                .WithMany()
                .HasForeignKey(x => x.VendorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.PurchaseRequisition)
                .WithMany()
                .HasForeignKey(x => x.PurchaseRequisitionId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.RFQ)
                .WithMany()
                .HasForeignKey(x => x.RFQId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.VendorComparison)
                .WithMany()
                .HasForeignKey(x => x.VendorComparisonId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(x => x.Lines)
                .WithOne(x => x.PurchaseOrder)
                .HasForeignKey(x => x.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Approvals)
                .WithOne(x => x.PurchaseOrder)
                .HasForeignKey(x => x.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.AmendmentHistories)
                .WithOne(x => x.PurchaseOrder)
                .HasForeignKey(x => x.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.History)
                .WithOne(x => x.PurchaseOrder)
                .HasForeignKey(x => x.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.ApprovalHistory)
                .WithOne(x => x.PurchaseOrder)
                .HasForeignKey(x => x.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.Priority);
            builder.HasIndex(x => x.VendorId);
            builder.HasIndex(x => new { x.TenantId, x.Status, x.OrderDate });
            builder.HasIndex(x => new { x.TenantId, x.Status, x.RequiredApprovalTier, x.CurrentApprovalTier });
        }
    }

    public class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
    {
        public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
        {
            builder.ToTable("purchase_order_lines");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TenantId).HasDefaultValue(1);
            builder.Property(x => x.LineNumber).IsRequired();
            builder.Property(x => x.ItemType).HasConversion<string>().HasMaxLength(32).IsRequired();
            builder.Property(x => x.ItemCode).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ItemName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(2000);
            builder.Property(x => x.HsnCode).HasMaxLength(16).HasDefaultValue(string.Empty);
            builder.Property(x => x.Unit).HasMaxLength(32).HasDefaultValue("Nos");

            // Precision 18,4 for quantities
            builder.Property(x => x.Quantity).HasPrecision(18, 4).IsRequired();
            builder.Property(x => x.ReceivedQuantity).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.RejectedQuantity).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.InvoicedQuantity).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.CancelledQuantity).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.OverDeliveryTolerancePercent).HasPrecision(5, 2).HasDefaultValue(0m);

            // Monetary values
            builder.Property(x => x.Rate).HasPrecision(18, 4).IsRequired();
            builder.Property(x => x.Discount).HasPrecision(5, 2).HasDefaultValue(0m);
            builder.Property(x => x.DiscountAmount).HasPrecision(18, 2).HasDefaultValue(0m);
            builder.Property(x => x.TaxableAmount).HasPrecision(18, 2).HasDefaultValue(0m);
            builder.Property(x => x.GstRatePercentage).HasPrecision(5, 2).HasDefaultValue(18m);
            builder.Property(x => x.CgstAmount).HasPrecision(18, 2).HasDefaultValue(0m);
            builder.Property(x => x.SgstAmount).HasPrecision(18, 2).HasDefaultValue(0m);
            builder.Property(x => x.IgstAmount).HasPrecision(18, 2).HasDefaultValue(0m);
            builder.Property(x => x.Tax).HasPrecision(18, 2).HasDefaultValue(0m);
            builder.Property(x => x.Amount).HasPrecision(18, 2).HasDefaultValue(0m);
            builder.Property(x => x.IsItcEligible).HasDefaultValue(true);

            // Logistics & JSONB
            builder.Property(x => x.DeliveryScheduleJson).HasColumnType("jsonb").HasDefaultValue("[]");
            builder.Property(x => x.LineStatus).HasConversion<string>().HasMaxLength(64).IsRequired();

            builder.Property(x => x.ConcurrencyVersion).IsRowVersion();

            builder.HasOne(x => x.PurchaseRequisitionLine)
                .WithMany()
                .HasForeignKey(x => x.PurchaseRequisitionLineId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.RFQLine)
                .WithMany()
                .HasForeignKey(x => x.RFQLineId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.VendorQuotationLine)
                .WithMany()
                .HasForeignKey(x => x.VendorQuotationLineId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(x => x.PurchaseOrderId);
            builder.HasIndex(x => x.ItemId);
            builder.HasIndex(x => new { x.PurchaseOrderId, x.LineStatus });
        }
    }

    public class PurchaseOrderApprovalConfiguration : IEntityTypeConfiguration<PurchaseOrderApproval>
    {
        public void Configure(EntityTypeBuilder<PurchaseOrderApproval> builder)
        {
            builder.ToTable("purchase_order_approvals");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TenantId).HasDefaultValue(1);
            builder.Property(x => x.TierLevel).HasConversion<int>().IsRequired();
            builder.Property(x => x.ApproverRole).HasMaxLength(128).IsRequired();
            builder.Property(x => x.ApproverUserId).HasMaxLength(128).IsRequired();
            builder.Property(x => x.ApproverName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Decision).HasConversion<string>().HasMaxLength(32).IsRequired();
            builder.Property(x => x.Comments).HasMaxLength(2000);
            builder.Property(x => x.HmacSignatureToken).HasMaxLength(256);

            builder.HasIndex(x => new { x.PurchaseOrderId, x.TierLevel });
        }
    }

    public class PurchaseOrderAmendmentHistoryConfiguration : IEntityTypeConfiguration<PurchaseOrderAmendmentHistory>
    {
        public void Configure(EntityTypeBuilder<PurchaseOrderAmendmentHistory> builder)
        {
            builder.ToTable("purchase_order_amendment_histories");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TenantId).HasDefaultValue(1);
            builder.Property(x => x.RevisionNumber).IsRequired();
            builder.Property(x => x.ChangedField).HasMaxLength(128).IsRequired();
            builder.Property(x => x.AmendmentReason).HasMaxLength(1000).IsRequired();
            builder.Property(x => x.ActionedByUserId).HasMaxLength(128).IsRequired();

            builder.HasIndex(x => new { x.PurchaseOrderId, x.RevisionNumber });
        }
    }

    public class PurchaseOrderStatusHistoryConfiguration : IEntityTypeConfiguration<PurchaseOrderStatusHistory>
    {
        public void Configure(EntityTypeBuilder<PurchaseOrderStatusHistory> builder)
        {
            builder.ToTable("purchase_order_status_histories");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.PreviousStatus).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.User).HasMaxLength(256);
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.Label).HasMaxLength(256);

            builder.HasIndex(x => new { x.PurchaseOrderId, x.Date });
        }
    }

    public class PurchaseOrderApprovalHistoryConfiguration : IEntityTypeConfiguration<PurchaseOrderApprovalHistory>
    {
        public void Configure(EntityTypeBuilder<PurchaseOrderApprovalHistory> builder)
        {
            builder.ToTable("purchase_order_approval_histories");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.EventKind).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Decision).HasMaxLength(128).IsRequired();
            builder.Property(x => x.Approver).HasMaxLength(256);
            builder.Property(x => x.Role).HasMaxLength(128);
            builder.Property(x => x.Remarks).HasMaxLength(2000);

            builder.HasIndex(x => new { x.PurchaseOrderId, x.DecisionDate });
        }
    }

    public class PurchaseOrderDocumentSequenceConfiguration : IEntityTypeConfiguration<PurchaseOrderDocumentSequence>
    {
        public void Configure(EntityTypeBuilder<PurchaseOrderDocumentSequence> builder)
        {
            builder.ToTable("purchase_order_document_sequences");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.Prefix).IsUnique();
        }
    }
}
