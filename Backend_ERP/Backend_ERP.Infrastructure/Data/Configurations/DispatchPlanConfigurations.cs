using ERP.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class DispatchPlanConfiguration : IEntityTypeConfiguration<DispatchPlan>
    {
        public void Configure(EntityTypeBuilder<DispatchPlan> builder)
        {
            builder.ToTable("DispatchPlans");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.DispatchNumber).IsRequired().HasMaxLength(64);
            builder.HasIndex(x => x.DispatchNumber).IsUnique();

            builder.Property(x => x.CustomerName).IsRequired().HasMaxLength(256);
            builder.Property(x => x.SalesOrderNumber).IsRequired().HasMaxLength(64);
            builder.Property(x => x.DeliveryAddress).IsRequired().HasMaxLength(1000);
            builder.Property(x => x.WarehouseName).IsRequired().HasMaxLength(256);
            builder.Property(x => x.Remarks).HasMaxLength(1000);
            builder.Property(x => x.Notes).HasMaxLength(1000);
            builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(128);
            builder.Property(x => x.UpdatedBy).IsRequired().HasMaxLength(128);

            builder.Property(x => x.Priority).HasConversion<string>().HasMaxLength(32);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);

            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.CustomerId);
            builder.HasIndex(x => x.SalesOrderId);
            builder.HasIndex(x => x.WarehouseId);
            builder.HasIndex(x => x.DispatchDate);

            builder.HasMany(x => x.Items)
                .WithOne(x => x.DispatchPlan)
                .HasForeignKey(x => x.DispatchPlanId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Attachments)
                .WithOne(x => x.DispatchPlan)
                .HasForeignKey(x => x.DispatchPlanId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Timeline)
                .WithOne(x => x.DispatchPlan)
                .HasForeignKey(x => x.DispatchPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class DispatchPlanItemConfiguration : IEntityTypeConfiguration<DispatchPlanItem>
    {
        public void Configure(EntityTypeBuilder<DispatchPlanItem> builder)
        {
            builder.ToTable("DispatchPlanItems");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.FinishedGoodCode).IsRequired().HasMaxLength(64);
            builder.Property(x => x.FinishedGoodName).IsRequired().HasMaxLength(256);
            builder.Property(x => x.BatchNumber).IsRequired().HasMaxLength(64);
            builder.Property(x => x.Quantity).HasPrecision(18, 4);
            builder.Property(x => x.Uom).IsRequired().HasMaxLength(32);
            builder.Property(x => x.WarehouseName).IsRequired().HasMaxLength(256);
            builder.Property(x => x.FinalInspectionNumber).HasMaxLength(64);
            builder.Property(x => x.TestCertificateNumber).HasMaxLength(64);

            builder.HasIndex(x => x.DispatchPlanId);
            builder.HasIndex(x => x.FinishedGoodId);
        }
    }

    public class DispatchPlanAttachmentConfiguration : IEntityTypeConfiguration<DispatchPlanAttachment>
    {
        public void Configure(EntityTypeBuilder<DispatchPlanAttachment> builder)
        {
            builder.ToTable("DispatchPlanAttachments");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.AttachmentId).IsRequired().HasMaxLength(64);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(256);
            builder.Property(x => x.UploadedBy).IsRequired().HasMaxLength(128);
            builder.Property(x => x.Kind).HasMaxLength(32);

            builder.HasIndex(x => x.DispatchPlanId);
        }
    }

    public class DispatchPlanTimelineEventConfiguration : IEntityTypeConfiguration<DispatchPlanTimelineEvent>
    {
        public void Configure(EntityTypeBuilder<DispatchPlanTimelineEvent> builder)
        {
            builder.ToTable("DispatchPlanTimelineEvents");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.EventId).IsRequired().HasMaxLength(64);
            builder.Property(x => x.User).IsRequired().HasMaxLength(128);
            builder.Property(x => x.Action).IsRequired().HasMaxLength(256);
            builder.Property(x => x.FromStatus).HasMaxLength(32);
            builder.Property(x => x.ToStatus).HasMaxLength(32);
            builder.Property(x => x.Remarks).HasMaxLength(1000);

            builder.HasIndex(x => x.DispatchPlanId);
        }
    }

    public class DispatchDocumentSequenceConfiguration : IEntityTypeConfiguration<DispatchDocumentSequence>
    {
        public void Configure(EntityTypeBuilder<DispatchDocumentSequence> builder)
        {
            builder.ToTable("DispatchDocumentSequences");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Prefix).IsRequired().HasMaxLength(32);
            builder.HasIndex(x => x.Prefix).IsUnique();
        }
    }
}
