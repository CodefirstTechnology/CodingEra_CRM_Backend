using ERP.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class PerformanceSnapshotConfiguration : IEntityTypeConfiguration<PerformanceSnapshot>
    {
        public void Configure(EntityTypeBuilder<PerformanceSnapshot> builder)
        {
            builder.ToTable("performance_snapshots");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.PeriodType).HasMaxLength(32).IsRequired();
            builder.Property(x => x.SalesTeam).HasMaxLength(128);
            builder.Property(x => x.Branch).HasMaxLength(128);
            builder.Property(x => x.RegionalManager).HasMaxLength(128);
            builder.Property(x => x.SalesOrderAmount).HasPrecision(18, 2);
            builder.Property(x => x.ProformaAmount).HasPrecision(18, 2);
            builder.Property(x => x.AdvancePaymentReceived).HasPrecision(18, 2);
            builder.Property(x => x.AdvancePaymentApplied).HasPrecision(18, 2);
            builder.Property(x => x.OutstandingAdvance).HasPrecision(18, 2);
            builder.Property(x => x.AssignedTarget).HasPrecision(18, 2);
            builder.Property(x => x.AchievedTarget).HasPrecision(18, 2);
            builder.Property(x => x.AchievementPercentage).HasPrecision(9, 2);
            builder.Property(x => x.CollectionPercentage).HasPrecision(9, 2);
            builder.Property(x => x.ConversionPercentage).HasPrecision(9, 2);
            builder.HasIndex(x => new { x.SnapshotDate, x.PeriodType, x.SalesPersonUserId });
            builder.HasIndex(x => x.SalesPersonUserId);
        }
    }

    public class PerformanceHistoryConfiguration : IEntityTypeConfiguration<PerformanceHistory>
    {
        public void Configure(EntityTypeBuilder<PerformanceHistory> builder)
        {
            builder.ToTable("performance_history");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.MetricName).HasMaxLength(128).IsRequired();
            builder.Property(x => x.OldValue).HasPrecision(18, 2);
            builder.Property(x => x.NewValue).HasPrecision(18, 2);
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.HasIndex(x => new { x.SalesPersonUserId, x.PeriodStart, x.PeriodEnd });
            builder.HasIndex(x => x.RecordedOn);
        }
    }

    public class PerformanceExportHistoryConfiguration : IEntityTypeConfiguration<PerformanceExportHistory>
    {
        public void Configure(EntityTypeBuilder<PerformanceExportHistory> builder)
        {
            builder.ToTable("performance_export_history");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ReportName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.GeneratedBy).HasMaxLength(64).IsRequired();
            builder.Property(x => x.FileName).HasMaxLength(512).IsRequired();
            builder.Property(x => x.ExportType).HasMaxLength(32).IsRequired();
            builder.HasIndex(x => x.GeneratedOn);
            builder.HasIndex(x => x.GeneratedBy);
        }
    }
}
