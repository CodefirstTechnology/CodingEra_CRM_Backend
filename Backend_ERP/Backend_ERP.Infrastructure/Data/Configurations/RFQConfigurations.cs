using ERP.Domain.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class RequestForQuotationConfiguration : IEntityTypeConfiguration<RequestForQuotation>
    {
        public void Configure(EntityTypeBuilder<RequestForQuotation> builder)
        {
            builder.ToTable("request_for_quotations");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TenantId).HasDefaultValue(1);
            builder.Property(x => x.RFQNumber).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Title).HasMaxLength(256).HasDefaultValue(string.Empty);
            builder.Property(x => x.PurchaseRequisitionNumber).HasMaxLength(64);
            builder.Property(x => x.SourcingType).HasConversion<string>().HasMaxLength(64).HasDefaultValue(RFQSourcingType.OpenCompetitive);
            builder.Property(x => x.IsSealedBid).HasDefaultValue(true);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.DeliveryTerms).HasMaxLength(1000);
            builder.Property(x => x.PaymentTerms).HasMaxLength(1000);
            builder.Property(x => x.VendorNotes).HasMaxLength(2000);

            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            builder.Property(x => x.ConcurrencyVersion).IsRowVersion();

            builder.HasIndex(x => new { x.TenantId, x.RFQNumber }).IsUnique().HasDatabaseName("ix_rfq_tenant_number");
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.PurchaseRequisitionId);

            builder.HasOne(x => x.PurchaseRequisition)
                .WithMany()
                .HasForeignKey(x => x.PurchaseRequisitionId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(x => x.Lines)
                .WithOne(x => x.RequestForQuotation)
                .HasForeignKey(x => x.RequestForQuotationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Vendors)
                .WithOne(x => x.RequestForQuotation)
                .HasForeignKey(x => x.RequestForQuotationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.History)
                .WithOne(x => x.RequestForQuotation)
                .HasForeignKey(x => x.RequestForQuotationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class RequestForQuotationLineConfiguration : IEntityTypeConfiguration<RequestForQuotationLine>
    {
        public void Configure(EntityTypeBuilder<RequestForQuotationLine> builder)
        {
            builder.ToTable("rfq_lines");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TenantId).HasDefaultValue(1);
            builder.Property(x => x.ItemCode).HasMaxLength(64).HasDefaultValue(string.Empty);
            builder.Property(x => x.ItemName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(1000);
            builder.Property(x => x.Quantity).HasPrecision(18, 4).IsRequired();
            builder.Property(x => x.Uom).HasMaxLength(64).HasDefaultValue("PCS");
            builder.Property(x => x.TargetUnitPrice).HasPrecision(18, 4);
            builder.Property(x => x.TechnicalSpecsJson).HasColumnType("jsonb").HasDefaultValue("{}");

            builder.HasIndex(x => x.RequestForQuotationId);

            builder.HasMany(x => x.ConsolidatedRequisitionLines)
                .WithOne(x => x.RequestForQuotationLine)
                .HasForeignKey(x => x.RequestForQuotationLineId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class RFQItemRequisitionLineConfiguration : IEntityTypeConfiguration<RFQItemRequisitionLine>
    {
        public void Configure(EntityTypeBuilder<RFQItemRequisitionLine> builder)
        {
            builder.ToTable("rfq_item_requisition_lines");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TenantId).HasDefaultValue(1);
            builder.Property(x => x.AllocatedQuantity).HasPrecision(18, 4).IsRequired();

            builder.HasIndex(x => x.RequestForQuotationLineId);
            builder.HasIndex(x => x.PurchaseRequisitionLineId);

            builder.HasOne(x => x.RequestForQuotationLine)
                .WithMany(x => x.ConsolidatedRequisitionLines)
                .HasForeignKey(x => x.RequestForQuotationLineId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.PurchaseRequisitionLine)
                .WithMany()
                .HasForeignKey(x => x.PurchaseRequisitionLineId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class RequestForQuotationVendorConfiguration : IEntityTypeConfiguration<RequestForQuotationVendor>
    {
        public void Configure(EntityTypeBuilder<RequestForQuotationVendor> builder)
        {
            builder.ToTable("rfq_vendors");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TenantId).HasDefaultValue(1);
            builder.Property(x => x.VendorName).HasMaxLength(256);
            builder.Property(x => x.ContactEmail).HasMaxLength(256);
            builder.Property(x => x.ContactPhone).HasMaxLength(64);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.AccessToken).HasMaxLength(128).HasDefaultValue(string.Empty);
            builder.Property(x => x.DeclineReason).HasMaxLength(1000).HasDefaultValue(string.Empty);

            builder.HasIndex(x => x.RequestForQuotationId);
            builder.HasIndex(x => x.VendorId);
            builder.HasIndex(x => x.AccessToken);

            builder.HasOne(x => x.Vendor)
                .WithMany()
                .HasForeignKey(x => x.VendorId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class RequestForQuotationStatusHistoryConfiguration : IEntityTypeConfiguration<RequestForQuotationStatusHistory>
    {
        public void Configure(EntityTypeBuilder<RequestForQuotationStatusHistory> builder)
        {
            builder.ToTable("rfq_status_histories");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.PreviousStatus).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.User).HasMaxLength(256);
            builder.Property(x => x.Remarks).HasMaxLength(2000);

            builder.HasIndex(x => new { x.RequestForQuotationId, x.Date });
        }
    }

    public class RFQDocumentSequenceConfiguration : IEntityTypeConfiguration<RFQDocumentSequence>
    {
        public void Configure(EntityTypeBuilder<RFQDocumentSequence> builder)
        {
            builder.ToTable("rfq_document_sequences");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.Prefix).IsUnique();
        }
    }
}
