using ERP.Domain.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class IncomingInspectionConfiguration : IEntityTypeConfiguration<IncomingInspection>
    {
        public void Configure(EntityTypeBuilder<IncomingInspection> builder)
        {
            builder.ToTable("incoming_inspections");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.InspectionNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.InspectionNumber).IsUnique();

            builder.Property(x => x.SupplierName).HasMaxLength(256);
            builder.Property(x => x.VendorName).HasMaxLength(256);
            builder.Property(x => x.PurchaseOrderNumber).HasMaxLength(64);
            builder.Property(x => x.GRNNumber).HasMaxLength(64);
            builder.Property(x => x.MaterialCode).HasMaxLength(64);
            builder.Property(x => x.MaterialName).HasMaxLength(256);
            builder.Property(x => x.BatchNumber).HasMaxLength(128);
            builder.Property(x => x.WarehouseName).HasMaxLength(256);
            builder.Property(x => x.Inspector).HasMaxLength(256);

            builder.Property(x => x.InspectionType).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.InspectionMethod).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.InspectionResult).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64);

            builder.Property(x => x.SamplingQuantity).HasPrecision(18, 4);
            builder.Property(x => x.AcceptedQuantity).HasPrecision(18, 4);
            builder.Property(x => x.RejectedQuantity).HasPrecision(18, 4);
            builder.Property(x => x.PendingQuantity).HasPrecision(18, 4);

            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.PurchaseOrderId);
            builder.HasIndex(x => x.GRNId);

            builder.HasMany(x => x.Checklist)
                .WithOne(x => x.IncomingInspection)
                .HasForeignKey(x => x.IncomingInspectionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class IncomingChecklistItemConfiguration : IEntityTypeConfiguration<IncomingChecklistItem>
    {
        public void Configure(EntityTypeBuilder<IncomingChecklistItem> builder)
        {
            builder.ToTable("incoming_checklist_items");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Parameter).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Specification).HasMaxLength(256);
            builder.Property(x => x.ActualValue).HasMaxLength(256);
            builder.Property(x => x.Result).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.Remarks).HasMaxLength(2000);

            builder.HasIndex(x => x.IncomingInspectionId);
        }
    }

    public class InProcessCheckConfiguration : IEntityTypeConfiguration<InProcessCheck>
    {
        public void Configure(EntityTypeBuilder<InProcessCheck> builder)
        {
            builder.ToTable("in_process_checks");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.QCNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.QCNumber).IsUnique();

            builder.Property(x => x.ProductionEntryNumber).HasMaxLength(64);
            builder.Property(x => x.WorkOrderNumber).HasMaxLength(64);
            builder.Property(x => x.MachineCode).HasMaxLength(64);
            builder.Property(x => x.MachineName).HasMaxLength(256);
            builder.Property(x => x.Operator).HasMaxLength(256);
            builder.Property(x => x.Shift).HasConversion<string>().HasMaxLength(32);
            builder.Property(x => x.Stage).HasMaxLength(128);
            builder.Property(x => x.ProductCode).HasMaxLength(64);
            builder.Property(x => x.ProductName).HasMaxLength(256);
            builder.Property(x => x.BatchNumber).HasMaxLength(128);
            builder.Property(x => x.Parameter).HasMaxLength(256);
            builder.Property(x => x.Tolerance).HasMaxLength(128);
            builder.Property(x => x.ExpectedValue).HasMaxLength(128);
            builder.Property(x => x.ActualValue).HasMaxLength(128);
            builder.Property(x => x.Result).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64);

            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            builder.HasIndex(x => x.Status);
        }
    }

    public class FinalInspectionConfiguration : IEntityTypeConfiguration<FinalInspection>
    {
        public void Configure(EntityTypeBuilder<FinalInspection> builder)
        {
            builder.ToTable("final_inspections");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.InspectionNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.InspectionNumber).IsUnique();

            builder.Property(x => x.FinishedProductCode).HasMaxLength(64);
            builder.Property(x => x.FinishedProductName).HasMaxLength(256);
            builder.Property(x => x.ProductionEntryNumber).HasMaxLength(64);
            builder.Property(x => x.ProductionBatch).HasMaxLength(128);
            builder.Property(x => x.Inspector).HasMaxLength(256);

            builder.Property(x => x.Dimension).HasMaxLength(128);
            builder.Property(x => x.Weight).HasMaxLength(128);
            builder.Property(x => x.Strength).HasMaxLength(128);
            builder.Property(x => x.SurfaceFinish).HasMaxLength(128);
            builder.Property(x => x.VisualCheck).HasMaxLength(128);

            builder.Property(x => x.AcceptedQuantity).HasPrecision(18, 4);
            builder.Property(x => x.RejectedQuantity).HasPrecision(18, 4);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64);

            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            builder.HasIndex(x => x.Status);

            builder.HasMany(x => x.Parameters)
                .WithOne(x => x.FinalInspection)
                .HasForeignKey(x => x.FinalInspectionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class FinalInspectionParameterConfiguration : IEntityTypeConfiguration<FinalInspectionParameter>
    {
        public void Configure(EntityTypeBuilder<FinalInspectionParameter> builder)
        {
            builder.ToTable("final_inspection_parameters");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Expected).HasMaxLength(256);
            builder.Property(x => x.Actual).HasMaxLength(256);
            builder.Property(x => x.Result).HasConversion<string>().HasMaxLength(64);

            builder.HasIndex(x => x.FinalInspectionId);
        }
    }

    public class LoadTestReportConfiguration : IEntityTypeConfiguration<LoadTestReport>
    {
        public void Configure(EntityTypeBuilder<LoadTestReport> builder)
        {
            builder.ToTable("load_test_reports");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ReportNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.ReportNumber).IsUnique();

            builder.Property(x => x.ProductCode).HasMaxLength(64);
            builder.Property(x => x.ProductName).HasMaxLength(256);
            builder.Property(x => x.FinalInspectionNumber).HasMaxLength(64);
            builder.Property(x => x.MachineCode).HasMaxLength(64);
            builder.Property(x => x.MachineName).HasMaxLength(256);

            builder.Property(x => x.LoadCapacity).HasPrecision(18, 4);
            builder.Property(x => x.AppliedLoad).HasPrecision(18, 4);
            builder.Property(x => x.Result).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.PassFail).HasMaxLength(32);

            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            builder.HasIndex(x => x.FinalInspectionId);
        }
    }

    public class TestCertificateConfiguration : IEntityTypeConfiguration<TestCertificate>
    {
        public void Configure(EntityTypeBuilder<TestCertificate> builder)
        {
            builder.ToTable("test_certificates");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.CertificateNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.CertificateNumber).IsUnique();

            builder.Property(x => x.CustomerName).HasMaxLength(256);
            builder.Property(x => x.ProductCode).HasMaxLength(64);
            builder.Property(x => x.ProductName).HasMaxLength(256);
            builder.Property(x => x.FinalInspectionNumber).HasMaxLength(64);
            builder.Property(x => x.LoadTestNumber).HasMaxLength(64);
            builder.Property(x => x.BatchNumber).HasMaxLength(128);
            builder.Property(x => x.IssuedBy).HasMaxLength(256);
            builder.Property(x => x.ApprovedBy).HasMaxLength(256);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64);

            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.FinalInspectionId);
        }
    }

    public class RejectionAnalysisConfiguration : IEntityTypeConfiguration<RejectionAnalysis>
    {
        public void Configure(EntityTypeBuilder<RejectionAnalysis> builder)
        {
            builder.ToTable("rejection_analyses");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.RejectionNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.RejectionNumber).IsUnique();

            builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.SourceRecordNumber).HasMaxLength(64);
            builder.Property(x => x.MaterialCode).HasMaxLength(64);
            builder.Property(x => x.MaterialName).HasMaxLength(256);
            builder.Property(x => x.ProductCode).HasMaxLength(64);
            builder.Property(x => x.ProductName).HasMaxLength(256);
            builder.Property(x => x.BatchNumber).HasMaxLength(128);

            builder.Property(x => x.Quantity).HasPrecision(18, 4);
            builder.Property(x => x.Reason).HasMaxLength(500);
            builder.Property(x => x.RootCause).HasMaxLength(1000);
            builder.Property(x => x.Department).HasMaxLength(256);
            builder.Property(x => x.Operator).HasMaxLength(256);
            builder.Property(x => x.MachineCode).HasMaxLength(64);
            builder.Property(x => x.MachineName).HasMaxLength(256);
            builder.Property(x => x.SupplierName).HasMaxLength(256);
            builder.Property(x => x.CorrectiveAction).HasMaxLength(1000);
            builder.Property(x => x.PreventiveAction).HasMaxLength(1000);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64);

            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.Source);
        }
    }

    public class QualityControlDocumentSequenceConfiguration : IEntityTypeConfiguration<QualityControlDocumentSequence>
    {
        public void Configure(EntityTypeBuilder<QualityControlDocumentSequence> builder)
        {
            builder.ToTable("quality_control_document_sequences");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.Prefix).IsUnique();
        }
    }
}
