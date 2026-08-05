using ERP.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class SalesTargetConfiguration : IEntityTypeConfiguration<SalesTarget>
    {
        public void Configure(EntityTypeBuilder<SalesTarget> builder)
        {
            builder.ToTable("sales_targets");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TargetNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.TargetNumber).IsUnique();

            builder.Property(x => x.TargetName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.TargetType).HasMaxLength(64).IsRequired();
            builder.Property(x => x.TargetCategory).HasMaxLength(64).IsRequired();
            builder.Property(x => x.AssignmentType).HasMaxLength(64).IsRequired();
            builder.Property(x => x.SalesTeam).HasMaxLength(128);
            builder.Property(x => x.Branch).HasMaxLength(128);
            builder.Property(x => x.RegionalManager).HasMaxLength(128);
            builder.Property(x => x.TargetValue).HasPrecision(18, 2);
            builder.Property(x => x.AchievedValue).HasPrecision(18, 2);
            builder.Property(x => x.RemainingValue).HasPrecision(18, 2);
            builder.Property(x => x.AchievementPercentage).HasPrecision(9, 2);
            builder.Property(x => x.Currency).HasMaxLength(8).IsRequired();
            builder.Property(x => x.Status).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.CreatedBy).HasMaxLength(64);
            builder.Property(x => x.UpdatedBy).HasMaxLength(64);

            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.TargetCategory);
            builder.HasIndex(x => x.SalesPersonUserId);
            builder.HasIndex(x => x.FinancialYear);
            builder.HasIndex(x => x.StartDate);
            builder.HasIndex(x => x.EndDate);
            builder.HasIndex(x => x.IsDeleted);

            builder.HasMany(x => x.Assignments)
                .WithOne(x => x.SalesTarget)
                .HasForeignKey(x => x.SalesTargetId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.ProgressHistory)
                .WithOne(x => x.SalesTarget)
                .HasForeignKey(x => x.SalesTargetId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.StatusHistory)
                .WithOne(x => x.SalesTarget)
                .HasForeignKey(x => x.SalesTargetId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class SalesTargetAssignmentConfiguration : IEntityTypeConfiguration<SalesTargetAssignment>
    {
        public void Configure(EntityTypeBuilder<SalesTargetAssignment> builder)
        {
            builder.ToTable("sales_target_assignments");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.AssignedTarget).HasPrecision(18, 2);
            builder.Property(x => x.AchievedValue).HasPrecision(18, 2);
            builder.Property(x => x.AchievementPercentage).HasPrecision(9, 2);
            builder.Property(x => x.Status).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.AssignedBy).HasMaxLength(64);

            builder.HasIndex(x => new { x.SalesTargetId, x.SalesPersonUserId });
            builder.HasIndex(x => x.SalesPersonUserId);
        }
    }

    public class SalesTargetProgressHistoryConfiguration : IEntityTypeConfiguration<SalesTargetProgressHistory>
    {
        public void Configure(EntityTypeBuilder<SalesTargetProgressHistory> builder)
        {
            builder.ToTable("sales_target_progress_histories");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.OldAchievedValue).HasPrecision(18, 2);
            builder.Property(x => x.NewAchievedValue).HasPrecision(18, 2);
            builder.Property(x => x.AchievementPercentage).HasPrecision(9, 2);
            builder.Property(x => x.Action).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.UpdatedBy).HasMaxLength(64);

            builder.HasIndex(x => new { x.SalesTargetId, x.UpdatedOn });
        }
    }

    public class SalesTargetStatusHistoryConfiguration : IEntityTypeConfiguration<SalesTargetStatusHistory>
    {
        public void Configure(EntityTypeBuilder<SalesTargetStatusHistory> builder)
        {
            builder.ToTable("sales_target_status_histories");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.OldStatus).HasMaxLength(64);
            builder.Property(x => x.NewStatus).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.ChangedBy).HasMaxLength(64);

            builder.HasIndex(x => new { x.SalesTargetId, x.ChangedOn });
        }
    }

    public class SalesTargetDocumentSequenceConfiguration : IEntityTypeConfiguration<SalesTargetDocumentSequence>
    {
        public void Configure(EntityTypeBuilder<SalesTargetDocumentSequence> builder)
        {
            builder.ToTable("sales_target_document_sequences");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Prefix).HasMaxLength(16).IsRequired();
            builder.HasIndex(x => new { x.FinancialYear, x.Prefix }).IsUnique();
        }
    }
}
