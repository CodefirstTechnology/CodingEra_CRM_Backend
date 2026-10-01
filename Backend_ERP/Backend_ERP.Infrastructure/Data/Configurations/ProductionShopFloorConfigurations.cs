using Backend_ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend_ERP.Infrastructure.Data.Configurations
{
    public class ProductionEntryConfiguration : IEntityTypeConfiguration<ProductionEntryEntity>
    {
        public void Configure(EntityTypeBuilder<ProductionEntryEntity> builder)
        {
            builder.ToTable("shop_floor_production_entries");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.EntryNumber).HasColumnName("entry_number").HasMaxLength(50).IsRequired();
            builder.Property(x => x.WorkOrderOperationId).HasColumnName("work_order_operation_id").IsRequired();
            builder.Property(x => x.ShiftId).HasColumnName("shift_id").HasMaxLength(50).IsRequired();
            builder.Property(x => x.OperatorId).HasColumnName("operator_id").HasMaxLength(50).IsRequired();
            builder.Property(x => x.MachineId).HasColumnName("machine_id").IsRequired();
            builder.Property(x => x.GoodQuantity).HasColumnName("good_quantity").HasColumnType("numeric(18,4)");
            builder.Property(x => x.ScrappedQuantity).HasColumnName("scrapped_quantity").HasColumnType("numeric(18,4)");
            builder.Property(x => x.StartTime).HasColumnName("start_time").IsRequired();
            builder.Property(x => x.EndTime).HasColumnName("end_time").IsRequired();
            builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("SUBMITTED");
            builder.Property(x => x.SupervisorApprovedBy).HasColumnName("supervisor_approved_by").HasMaxLength(100);
            builder.Property(x => x.SupervisorApprovedAt).HasColumnName("supervisor_approved_at");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");

            builder.Property(x => x.RowVersion)
                .HasColumnName("xmin")
                .HasColumnType("uint")
                .ValueGeneratedOnAddOrUpdate()
                .IsRowVersion();

            builder.HasMany(x => x.MaterialLogs)
                .WithOne(x => x.ProductionEntry)
                .HasForeignKey(x => x.ProductionEntryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.RejectionLogs)
                .WithOne(x => x.ProductionEntry)
                .HasForeignKey(x => x.ProductionEntryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class MaterialConsumptionLogConfiguration : IEntityTypeConfiguration<MaterialConsumptionLogEntity>
    {
        public void Configure(EntityTypeBuilder<MaterialConsumptionLogEntity> builder)
        {
            builder.ToTable("material_consumption_logs");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.ProductionEntryId).HasColumnName("production_entry_id").IsRequired();
            builder.Property(x => x.RawMaterialItemId).HasColumnName("raw_material_item_id").IsRequired();
            builder.Property(x => x.BatchId).HasColumnName("batch_id");
            builder.Property(x => x.StandardBomQuantity).HasColumnName("standard_bom_quantity").HasColumnType("numeric(18,6)");
            builder.Property(x => x.ActualConsumedQuantity).HasColumnName("actual_consumed_quantity").HasColumnType("numeric(18,6)");
            builder.Property(x => x.VarianceQuantity).HasColumnName("variance_quantity").HasColumnType("numeric(18,6)");
            builder.Property(x => x.ConsumptionType).HasColumnName("consumption_type").HasMaxLength(30).HasDefaultValue("BACKFLUSH");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        }
    }

    public class RejectionTrackingLogConfiguration : IEntityTypeConfiguration<RejectionTrackingLogEntity>
    {
        public void Configure(EntityTypeBuilder<RejectionTrackingLogEntity> builder)
        {
            builder.ToTable("rejection_tracking_logs");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.ProductionEntryId).HasColumnName("production_entry_id").IsRequired();
            builder.Property(x => x.WorkOrderId).HasColumnName("work_order_id").IsRequired();
            builder.Property(x => x.DefectCategory).HasColumnName("defect_category").HasMaxLength(30).HasDefaultValue("OPERATOR_ERROR");
            builder.Property(x => x.DefectReasonCode).HasColumnName("defect_reason_code").HasMaxLength(50).IsRequired();
            builder.Property(x => x.RejectedQuantity).HasColumnName("rejected_quantity").HasColumnType("numeric(18,4)");
            builder.Property(x => x.Disposition).HasColumnName("disposition").HasMaxLength(30).HasDefaultValue("SCRAP");
            builder.Property(x => x.UnitScrapCost).HasColumnName("unit_scrap_cost").HasColumnType("numeric(18,4)");
            builder.Property(x => x.TotalLossCost).HasColumnName("total_loss_cost").HasColumnType("numeric(18,4)");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        }
    }

    public class DailyProductionReportConfiguration : IEntityTypeConfiguration<DailyProductionReportEntity>
    {
        public void Configure(EntityTypeBuilder<DailyProductionReportEntity> builder)
        {
            builder.ToTable("daily_production_reports");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.ReportDate).HasColumnName("report_date").IsRequired();
            builder.Property(x => x.ShiftId).HasColumnName("shift_id").HasMaxLength(50).IsRequired();
            builder.Property(x => x.WorkCenterId).HasColumnName("work_center_id").IsRequired();
            builder.Property(x => x.TotalPlannedQty).HasColumnName("total_planned_qty").HasColumnType("numeric(18,4)");
            builder.Property(x => x.TotalGoodQty).HasColumnName("total_good_qty").HasColumnType("numeric(18,4)");
            builder.Property(x => x.TotalScrapQty).HasColumnName("total_scrap_qty").HasColumnType("numeric(18,4)");
            builder.Property(x => x.TotalDowntimeMinutes).HasColumnName("total_downtime_minutes").HasColumnType("numeric(10,2)");
            builder.Property(x => x.OeePercentage).HasColumnName("oee_percentage").HasColumnType("numeric(5,2)");
            builder.Property(x => x.AvailabilityPercentage).HasColumnName("availability_percentage").HasColumnType("numeric(5,2)");
            builder.Property(x => x.PerformancePercentage).HasColumnName("performance_percentage").HasColumnType("numeric(5,2)");
            builder.Property(x => x.QualityPercentage).HasColumnName("quality_percentage").HasColumnType("numeric(5,2)");
            builder.Property(x => x.IsFinalized).HasColumnName("is_finalized").HasDefaultValue(false);
            builder.Property(x => x.FinalizedBy).HasColumnName("finalized_by").HasMaxLength(100);
            builder.Property(x => x.FinalizedAt).HasColumnName("finalized_at");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.Property(x => x.RowVersion)
                .HasColumnName("xmin")
                .HasColumnType("uint")
                .ValueGeneratedOnAddOrUpdate()
                .IsRowVersion();
        }
    }

    public class DailyProductionReportEntryJunctionConfiguration : IEntityTypeConfiguration<DailyProductionReportEntryJunctionEntity>
    {
        public void Configure(EntityTypeBuilder<DailyProductionReportEntryJunctionEntity> builder)
        {
            builder.ToTable("daily_production_report_entries");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.DailyProductionReportId).HasColumnName("daily_production_report_id").IsRequired();
            builder.Property(x => x.ProductionEntryId).HasColumnName("production_entry_id").IsRequired();
            builder.Property(x => x.LinkedAt).HasColumnName("linked_at");

            builder.HasOne(x => x.Report)
                .WithMany(r => r.ReportEntries)
                .HasForeignKey(x => x.DailyProductionReportId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.ProductionEntry)
                .WithMany()
                .HasForeignKey(x => x.ProductionEntryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
