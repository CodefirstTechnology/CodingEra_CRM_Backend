using ERP.Domain.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class PurchaseRequisitionConfiguration : IEntityTypeConfiguration<PurchaseRequisition>
    {
        public void Configure(EntityTypeBuilder<PurchaseRequisition> builder)
        {
            builder.ToTable("purchase_requisitions");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.PRNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.PRNumber).IsUnique();

            builder.Property(x => x.Department).HasMaxLength(128).IsRequired();
            builder.Property(x => x.Requestor).HasMaxLength(128).IsRequired();
            builder.Property(x => x.Priority).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();

            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.InternalNotes).HasMaxLength(2000);
            builder.Property(x => x.TotalEstimatedAmount).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.RFQNumber).HasMaxLength(64);

            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            builder.HasIndex(x => x.Department);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.Priority);

            builder.HasMany(x => x.Lines)
                .WithOne(x => x.PurchaseRequisition)
                .HasForeignKey(x => x.PurchaseRequisitionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.History)
                .WithOne(x => x.PurchaseRequisition)
                .HasForeignKey(x => x.PurchaseRequisitionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class PurchaseRequisitionLineConfiguration : IEntityTypeConfiguration<PurchaseRequisitionLine>
    {
        public void Configure(EntityTypeBuilder<PurchaseRequisitionLine> builder)
        {
            builder.ToTable("purchase_requisition_lines");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ItemName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(1000);
            builder.Property(x => x.Quantity).HasPrecision(18, 4).IsRequired();
            builder.Property(x => x.Uom).HasMaxLength(64).HasDefaultValue("PCS");
            builder.Property(x => x.EstimatedPrice).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.TotalAmount).HasPrecision(18, 4).HasDefaultValue(0m);

            builder.HasIndex(x => x.PurchaseRequisitionId);
        }
    }

    public class PurchaseRequisitionStatusHistoryConfiguration : IEntityTypeConfiguration<PurchaseRequisitionStatusHistory>
    {
        public void Configure(EntityTypeBuilder<PurchaseRequisitionStatusHistory> builder)
        {
            builder.ToTable("purchase_requisition_status_histories");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.PreviousStatus).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.User).HasMaxLength(256);
            builder.Property(x => x.Remarks).HasMaxLength(2000);

            builder.HasIndex(x => new { x.PurchaseRequisitionId, x.Date });
        }
    }

    public class PurchaseRequisitionDocumentSequenceConfiguration : IEntityTypeConfiguration<PurchaseRequisitionDocumentSequence>
    {
        public void Configure(EntityTypeBuilder<PurchaseRequisitionDocumentSequence> builder)
        {
            builder.ToTable("purchase_requisition_document_sequences");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.Prefix).IsUnique();
        }
    }
}
