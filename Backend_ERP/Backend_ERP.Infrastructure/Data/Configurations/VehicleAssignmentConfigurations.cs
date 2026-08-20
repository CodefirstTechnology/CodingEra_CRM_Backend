using ERP.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class VehicleAssignmentConfiguration : IEntityTypeConfiguration<VehicleAssignment>
    {
        public void Configure(EntityTypeBuilder<VehicleAssignment> builder)
        {
            builder.ToTable("VehicleAssignments");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.AssignmentNumber).IsRequired().HasMaxLength(64);
            builder.HasIndex(x => x.AssignmentNumber).IsUnique();

            builder.Property(x => x.DispatchNumber).IsRequired().HasMaxLength(64);
            builder.Property(x => x.VehicleName).IsRequired().HasMaxLength(256);
            builder.Property(x => x.VehicleNumber).IsRequired().HasMaxLength(64);
            builder.Property(x => x.DriverName).IsRequired().HasMaxLength(256);
            builder.Property(x => x.DriverContact).IsRequired().HasMaxLength(64);
            builder.Property(x => x.TransportCompanyName).IsRequired().HasMaxLength(256);
            builder.Property(x => x.LoadingTime).IsRequired().HasMaxLength(32);
            builder.Property(x => x.Capacity).HasPrecision(18, 4);
            builder.Property(x => x.AssignedQuantity).HasPrecision(18, 4);
            builder.Property(x => x.Remarks).HasMaxLength(1000);
            builder.Property(x => x.Notes).HasMaxLength(1000);
            builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(128);
            builder.Property(x => x.UpdatedBy).IsRequired().HasMaxLength(128);

            builder.Property(x => x.VehicleType).HasConversion<string>().HasMaxLength(32);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);

            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.DispatchId);
            builder.HasIndex(x => x.VehicleNumber);
            builder.HasIndex(x => x.TransportCompanyId);
            builder.HasIndex(x => x.LoadingDate);

            builder.HasOne(x => x.DispatchPlan)
                .WithMany()
                .HasForeignKey(x => x.DispatchId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Transport)
                .WithMany()
                .HasForeignKey(x => x.TransportId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(x => x.Attachments)
                .WithOne(x => x.VehicleAssignment)
                .HasForeignKey(x => x.VehicleAssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Timeline)
                .WithOne(x => x.VehicleAssignment)
                .HasForeignKey(x => x.VehicleAssignmentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class VehicleAssignmentAttachmentConfiguration : IEntityTypeConfiguration<VehicleAssignmentAttachment>
    {
        public void Configure(EntityTypeBuilder<VehicleAssignmentAttachment> builder)
        {
            builder.ToTable("VehicleAssignmentAttachments");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.AttachmentId).IsRequired().HasMaxLength(64);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(256);
            builder.Property(x => x.UploadedBy).IsRequired().HasMaxLength(128);
            builder.Property(x => x.Kind).HasMaxLength(32);

            builder.HasIndex(x => x.VehicleAssignmentId);
        }
    }

    public class VehicleAssignmentTimelineEventConfiguration : IEntityTypeConfiguration<VehicleAssignmentTimelineEvent>
    {
        public void Configure(EntityTypeBuilder<VehicleAssignmentTimelineEvent> builder)
        {
            builder.ToTable("VehicleAssignmentTimelineEvents");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.EventId).IsRequired().HasMaxLength(64);
            builder.Property(x => x.User).IsRequired().HasMaxLength(128);
            builder.Property(x => x.Action).IsRequired().HasMaxLength(256);
            builder.Property(x => x.FromStatus).HasMaxLength(32);
            builder.Property(x => x.ToStatus).HasMaxLength(32);
            builder.Property(x => x.Remarks).HasMaxLength(1000);

            builder.HasIndex(x => x.VehicleAssignmentId);
        }
    }

    public class VehicleAssignmentDocumentSequenceConfiguration : IEntityTypeConfiguration<VehicleAssignmentDocumentSequence>
    {
        public void Configure(EntityTypeBuilder<VehicleAssignmentDocumentSequence> builder)
        {
            builder.ToTable("VehicleAssignmentDocumentSequences");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Prefix).IsRequired().HasMaxLength(32);
            builder.HasIndex(x => x.Prefix).IsUnique();
        }
    }

    public class TransportDetailConfiguration : IEntityTypeConfiguration<TransportDetail>
    {
        public void Configure(EntityTypeBuilder<TransportDetail> builder)
        {
            builder.ToTable("TransportDetails");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TransportNumber).IsRequired().HasMaxLength(64);
            builder.HasIndex(x => x.TransportNumber).IsUnique();

            builder.Property(x => x.DispatchNumber).IsRequired().HasMaxLength(64);
            builder.Property(x => x.AssignmentNumber).IsRequired().HasMaxLength(64);
            builder.Property(x => x.VehicleNumber).IsRequired().HasMaxLength(64);
            builder.Property(x => x.DriverName).IsRequired().HasMaxLength(256);
            builder.Property(x => x.TransportCompanyName).IsRequired().HasMaxLength(256);
            builder.Property(x => x.Route).IsRequired().HasMaxLength(500);
            builder.Property(x => x.Source).IsRequired().HasMaxLength(256);
            builder.Property(x => x.Destination).IsRequired().HasMaxLength(500);
            builder.Property(x => x.DistanceKm).HasPrecision(18, 2);
            builder.Property(x => x.EstimatedTimeHours).HasPrecision(18, 2);
            builder.Property(x => x.FuelNotes).HasMaxLength(1000);
            builder.Property(x => x.Remarks).HasMaxLength(1000);
            builder.Property(x => x.Notes).HasMaxLength(1000);
            builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(128);
            builder.Property(x => x.UpdatedBy).IsRequired().HasMaxLength(128);

            builder.Property(x => x.Mode).HasConversion<string>().HasMaxLength(32);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);

            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.DispatchId);
            builder.HasIndex(x => x.VehicleAssignmentId);

            builder.HasMany(x => x.Attachments)
                .WithOne(x => x.TransportDetail)
                .HasForeignKey(x => x.TransportDetailId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Timeline)
                .WithOne(x => x.TransportDetail)
                .HasForeignKey(x => x.TransportDetailId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class TransportAttachmentConfiguration : IEntityTypeConfiguration<TransportAttachment>
    {
        public void Configure(EntityTypeBuilder<TransportAttachment> builder)
        {
            builder.ToTable("TransportAttachments");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.AttachmentId).IsRequired().HasMaxLength(64);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(256);
            builder.Property(x => x.UploadedBy).IsRequired().HasMaxLength(128);
            builder.Property(x => x.Kind).HasMaxLength(32);

            builder.HasIndex(x => x.TransportDetailId);
        }
    }

    public class TransportTimelineEventConfiguration : IEntityTypeConfiguration<TransportTimelineEvent>
    {
        public void Configure(EntityTypeBuilder<TransportTimelineEvent> builder)
        {
            builder.ToTable("TransportTimelineEvents");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.EventId).IsRequired().HasMaxLength(64);
            builder.Property(x => x.User).IsRequired().HasMaxLength(128);
            builder.Property(x => x.Action).IsRequired().HasMaxLength(256);
            builder.Property(x => x.FromStatus).HasMaxLength(32);
            builder.Property(x => x.ToStatus).HasMaxLength(32);
            builder.Property(x => x.Remarks).HasMaxLength(1000);

            builder.HasIndex(x => x.TransportDetailId);
        }
    }

    public class TransportDocumentSequenceConfiguration : IEntityTypeConfiguration<TransportDocumentSequence>
    {
        public void Configure(EntityTypeBuilder<TransportDocumentSequence> builder)
        {
            builder.ToTable("TransportDocumentSequences");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Prefix).IsRequired().HasMaxLength(32);
            builder.HasIndex(x => x.Prefix).IsUnique();
        }
    }

    public class LorryReceiptConfiguration : IEntityTypeConfiguration<LorryReceipt>
    {
        public void Configure(EntityTypeBuilder<LorryReceipt> builder)
        {
            builder.ToTable("LorryReceipts");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.LrNumber).IsRequired().HasMaxLength(64);
            builder.HasIndex(x => x.LrNumber).IsUnique();

            builder.Property(x => x.DispatchNumber).IsRequired().HasMaxLength(64);
            builder.Property(x => x.TransportNumber).IsRequired().HasMaxLength(64);
            builder.Property(x => x.VehicleNumber).IsRequired().HasMaxLength(64);
            builder.Property(x => x.CustomerName).IsRequired().HasMaxLength(256);
            builder.Property(x => x.Consignor).IsRequired().HasMaxLength(256);
            builder.Property(x => x.Consignee).IsRequired().HasMaxLength(256);
            builder.Property(x => x.WeightKg).HasPrecision(18, 4);
            builder.Property(x => x.FreightCharges).HasPrecision(18, 2);
            builder.Property(x => x.Remarks).HasMaxLength(1000);
            builder.Property(x => x.Notes).HasMaxLength(1000);
            builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(128);
            builder.Property(x => x.UpdatedBy).IsRequired().HasMaxLength(128);

            builder.Property(x => x.PaymentType).HasConversion<string>().HasMaxLength(32);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);

            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.DispatchId);
            builder.HasIndex(x => x.TransportId);
            builder.HasIndex(x => x.CustomerId);

            builder.HasMany(x => x.Attachments)
                .WithOne(x => x.LorryReceipt)
                .HasForeignKey(x => x.LorryReceiptId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Timeline)
                .WithOne(x => x.LorryReceipt)
                .HasForeignKey(x => x.LorryReceiptId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class LrAttachmentConfiguration : IEntityTypeConfiguration<LrAttachment>
    {
        public void Configure(EntityTypeBuilder<LrAttachment> builder)
        {
            builder.ToTable("LrAttachments");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.AttachmentId).IsRequired().HasMaxLength(64);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(256);
            builder.Property(x => x.UploadedBy).IsRequired().HasMaxLength(128);
            builder.Property(x => x.Kind).HasMaxLength(32);

            builder.HasIndex(x => x.LorryReceiptId);
        }
    }

    public class LrTimelineEventConfiguration : IEntityTypeConfiguration<LrTimelineEvent>
    {
        public void Configure(EntityTypeBuilder<LrTimelineEvent> builder)
        {
            builder.ToTable("LrTimelineEvents");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.EventId).IsRequired().HasMaxLength(64);
            builder.Property(x => x.User).IsRequired().HasMaxLength(128);
            builder.Property(x => x.Action).IsRequired().HasMaxLength(256);
            builder.Property(x => x.FromStatus).HasMaxLength(32);
            builder.Property(x => x.ToStatus).HasMaxLength(32);
            builder.Property(x => x.Remarks).HasMaxLength(1000);

            builder.HasIndex(x => x.LorryReceiptId);
        }
    }

    public class LrDocumentSequenceConfiguration : IEntityTypeConfiguration<LrDocumentSequence>
    {
        public void Configure(EntityTypeBuilder<LrDocumentSequence> builder)
        {
            builder.ToTable("LrDocumentSequences");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Prefix).IsRequired().HasMaxLength(32);
            builder.HasIndex(x => x.Prefix).IsUnique();
        }
    }

    public class EwayBillConfiguration : IEntityTypeConfiguration<EwayBill>
    {
        public void Configure(EntityTypeBuilder<EwayBill> builder)
        {
            builder.ToTable("EwayBills");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.EwayBillNumber).IsRequired().HasMaxLength(64);
            builder.HasIndex(x => x.EwayBillNumber).IsUnique();

            builder.Property(x => x.DispatchNumber).IsRequired().HasMaxLength(64);
            builder.Property(x => x.InvoiceNumber).IsRequired().HasMaxLength(64);
            builder.Property(x => x.CustomerName).IsRequired().HasMaxLength(256);
            builder.Property(x => x.GstNumber).IsRequired().HasMaxLength(32);
            builder.Property(x => x.VehicleNumber).IsRequired().HasMaxLength(64);
            builder.Property(x => x.TransportNumber).IsRequired().HasMaxLength(64);
            builder.Property(x => x.DistanceKm).HasPrecision(18, 2);
            builder.Property(x => x.TotalValue).HasPrecision(18, 2);
            builder.Property(x => x.Remarks).HasMaxLength(1000);
            builder.Property(x => x.Notes).HasMaxLength(1000);
            builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(128);
            builder.Property(x => x.UpdatedBy).IsRequired().HasMaxLength(128);

            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);

            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.DispatchId);
            builder.HasIndex(x => x.TransportId);
            builder.HasIndex(x => x.CustomerId);

            builder.HasMany(x => x.Attachments)
                .WithOne(x => x.EwayBill)
                .HasForeignKey(x => x.EwayBillId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Timeline)
                .WithOne(x => x.EwayBill)
                .HasForeignKey(x => x.EwayBillId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class EwayAttachmentConfiguration : IEntityTypeConfiguration<EwayAttachment>
    {
        public void Configure(EntityTypeBuilder<EwayAttachment> builder)
        {
            builder.ToTable("EwayAttachments");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.AttachmentId).IsRequired().HasMaxLength(64);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(256);
            builder.Property(x => x.UploadedBy).IsRequired().HasMaxLength(128);
            builder.Property(x => x.Kind).HasMaxLength(32);

            builder.HasIndex(x => x.EwayBillId);
        }
    }

    public class EwayTimelineEventConfiguration : IEntityTypeConfiguration<EwayTimelineEvent>
    {
        public void Configure(EntityTypeBuilder<EwayTimelineEvent> builder)
        {
            builder.ToTable("EwayTimelineEvents");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.EventId).IsRequired().HasMaxLength(64);
            builder.Property(x => x.User).IsRequired().HasMaxLength(128);
            builder.Property(x => x.Action).IsRequired().HasMaxLength(256);
            builder.Property(x => x.FromStatus).HasMaxLength(32);
            builder.Property(x => x.ToStatus).HasMaxLength(32);
            builder.Property(x => x.Remarks).HasMaxLength(1000);

            builder.HasIndex(x => x.EwayBillId);
        }
    }

    public class EwayDocumentSequenceConfiguration : IEntityTypeConfiguration<EwayDocumentSequence>
    {
        public void Configure(EntityTypeBuilder<EwayDocumentSequence> builder)
        {
            builder.ToTable("EwayDocumentSequences");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Prefix).IsRequired().HasMaxLength(32);
            builder.HasIndex(x => x.Prefix).IsUnique();
        }
    }

    public class DispatchStatusTrackConfiguration : IEntityTypeConfiguration<DispatchStatusTrack>
    {
        public void Configure(EntityTypeBuilder<DispatchStatusTrack> builder)
        {
            builder.ToTable("DispatchStatusTracks");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.DispatchNumber).IsRequired().HasMaxLength(64);
            builder.Property(x => x.Remarks).HasMaxLength(1000);
            builder.Property(x => x.Notes).HasMaxLength(1000);
            builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(128);
            builder.Property(x => x.UpdatedBy).IsRequired().HasMaxLength(128);

            builder.Property(x => x.CurrentStatus).HasConversion<string>().HasMaxLength(32);
            builder.Property(x => x.WarehouseStatus).HasConversion<string>().HasMaxLength(32);
            builder.Property(x => x.VehicleStatus).HasConversion<string>().HasMaxLength(32);
            builder.Property(x => x.TransportStatus).HasConversion<string>().HasMaxLength(32);

            builder.HasIndex(x => x.DispatchId).IsUnique();
            builder.HasIndex(x => x.CurrentStatus);

            builder.HasMany(x => x.Timeline)
                .WithOne(x => x.DispatchStatusTrack)
                .HasForeignKey(x => x.DispatchStatusTrackId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class DispatchStatusTimelineEventConfiguration : IEntityTypeConfiguration<DispatchStatusTimelineEvent>
    {
        public void Configure(EntityTypeBuilder<DispatchStatusTimelineEvent> builder)
        {
            builder.ToTable("DispatchStatusTimelineEvents");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.EventId).IsRequired().HasMaxLength(64);
            builder.Property(x => x.User).IsRequired().HasMaxLength(128);
            builder.Property(x => x.Action).IsRequired().HasMaxLength(256);
            builder.Property(x => x.FromStatus).HasMaxLength(32);
            builder.Property(x => x.ToStatus).HasMaxLength(32);
            builder.Property(x => x.Remarks).HasMaxLength(1000);

            builder.HasIndex(x => x.DispatchStatusTrackId);
        }
    }

    public class DeliveryConfirmationConfiguration : IEntityTypeConfiguration<DeliveryConfirmation>
    {
        public void Configure(EntityTypeBuilder<DeliveryConfirmation> builder)
        {
            builder.ToTable("DeliveryConfirmations");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.PodNumber).IsRequired().HasMaxLength(64);
            builder.HasIndex(x => x.PodNumber).IsUnique();

            builder.Property(x => x.DispatchNumber).IsRequired().HasMaxLength(64);
            builder.Property(x => x.CustomerName).IsRequired().HasMaxLength(256);
            builder.Property(x => x.ReceiverName).IsRequired().HasMaxLength(256);
            builder.Property(x => x.ReceiverContact).IsRequired().HasMaxLength(64);
            builder.Property(x => x.DeliveryRemarks).HasMaxLength(1000);
            builder.Property(x => x.DamageRemarks).HasMaxLength(1000);
            builder.Property(x => x.Remarks).HasMaxLength(1000);
            builder.Property(x => x.Notes).HasMaxLength(1000);
            builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(128);
            builder.Property(x => x.UpdatedBy).IsRequired().HasMaxLength(128);

            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);

            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.DispatchId);
            builder.HasIndex(x => x.CustomerId);

            builder.HasMany(x => x.Attachments)
                .WithOne(x => x.DeliveryConfirmation)
                .HasForeignKey(x => x.DeliveryConfirmationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Timeline)
                .WithOne(x => x.DeliveryConfirmation)
                .HasForeignKey(x => x.DeliveryConfirmationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class PodAttachmentConfiguration : IEntityTypeConfiguration<PodAttachment>
    {
        public void Configure(EntityTypeBuilder<PodAttachment> builder)
        {
            builder.ToTable("PodAttachments");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.AttachmentId).IsRequired().HasMaxLength(64);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(256);
            builder.Property(x => x.UploadedBy).IsRequired().HasMaxLength(128);
            builder.Property(x => x.Kind).HasMaxLength(32);

            builder.HasIndex(x => x.DeliveryConfirmationId);
        }
    }

    public class PodTimelineEventConfiguration : IEntityTypeConfiguration<PodTimelineEvent>
    {
        public void Configure(EntityTypeBuilder<PodTimelineEvent> builder)
        {
            builder.ToTable("PodTimelineEvents");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.EventId).IsRequired().HasMaxLength(64);
            builder.Property(x => x.User).IsRequired().HasMaxLength(128);
            builder.Property(x => x.Action).IsRequired().HasMaxLength(256);
            builder.Property(x => x.FromStatus).HasMaxLength(32);
            builder.Property(x => x.ToStatus).HasMaxLength(32);
            builder.Property(x => x.Remarks).HasMaxLength(1000);

            builder.HasIndex(x => x.DeliveryConfirmationId);
        }
    }

    public class PodDocumentSequenceConfiguration : IEntityTypeConfiguration<PodDocumentSequence>
    {
        public void Configure(EntityTypeBuilder<PodDocumentSequence> builder)
        {
            builder.ToTable("PodDocumentSequences");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Prefix).IsRequired().HasMaxLength(32);
            builder.HasIndex(x => x.Prefix).IsUnique();
        }
    }
}
