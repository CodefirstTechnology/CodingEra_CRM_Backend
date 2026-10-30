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

            // Multi-Tenancy & Natural Key
            builder.Property(x => x.TenantId).HasMaxLength(64).HasDefaultValue("default").IsRequired();
            builder.Property(x => x.TargetNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => new { x.TenantId, x.TargetNumber }).IsUnique();

            builder.Property(x => x.TargetName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.TargetPeriod).HasMaxLength(32).IsRequired();
            builder.Property(x => x.PeriodType).HasMaxLength(32).IsRequired();
            builder.Property(x => x.AssignmentType).HasMaxLength(64).IsRequired();
            builder.Property(x => x.TargetCategory).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Branch).HasMaxLength(128);
            builder.Property(x => x.SalesTeam).HasMaxLength(128);
            builder.Property(x => x.RegionalManager).HasMaxLength(128);

            // Monetary & Volume Precisions
            builder.Property(x => x.RevenueTarget).HasPrecision(18, 2);
            builder.Property(x => x.VolumeTarget).HasPrecision(18, 4);
            builder.Property(x => x.AchievedVolume).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.MarginHurdleTarget).HasPrecision(5, 2);
            builder.Property(x => x.RealizedRevenue).HasPrecision(18, 2).HasDefaultValue(0m);
            builder.Property(x => x.RealizedCash).HasPrecision(18, 2).HasDefaultValue(0m);
            builder.Property(x => x.RealizedBilled).HasPrecision(18, 2).HasDefaultValue(0m);
            builder.Property(x => x.RealizedMarginAmount).HasPrecision(18, 2).HasDefaultValue(0m);
            builder.Property(x => x.RealizedMarginPercentage).HasPrecision(5, 2).HasDefaultValue(0m);

            builder.Property(x => x.AchievedValue).HasPrecision(18, 2).HasDefaultValue(0m);
            builder.Property(x => x.RemainingValue).HasPrecision(18, 2).HasDefaultValue(0m);
            builder.Property(x => x.OverAchievementValue).HasPrecision(18, 2).HasDefaultValue(0m);
            builder.Property(x => x.AchievementPercentage).HasPrecision(9, 2).HasDefaultValue(0m);
            builder.Property(x => x.LockedExchangeRate).HasPrecision(18, 6).HasDefaultValue(1.000000m);
            builder.Property(x => x.OriginalTargetValue).HasPrecision(18, 2);
            builder.Property(x => x.ProrationFactor).HasPrecision(5, 4).HasDefaultValue(1.0000m);
            builder.Property(x => x.CalculatedCommissionAmount).HasPrecision(18, 2).HasDefaultValue(0m);

            builder.Property(x => x.Currency).HasMaxLength(8).HasDefaultValue("INR").IsRequired();
            builder.Property(x => x.Status).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ApprovalToken).HasMaxLength(256);
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.CreatedBy).HasMaxLength(64);
            builder.Property(x => x.UpdatedBy).HasMaxLength(64);
            builder.Property(x => x.IsLocked).HasDefaultValue(false);
            builder.Property(x => x.RevisionNumber).HasDefaultValue(1);
            builder.Property(x => x.IsAutoAggregated).HasDefaultValue(false);

            // Optimistic Concurrency Token via PostgreSQL system column 'xmin'
            builder.Property(x => x.ConcurrencyVersion).IsRowVersion();

            // Filtered & High-Performance Composite Indexes
            builder.HasIndex(x => new { x.TenantId, x.Status, x.SalesPersonUserId, x.StartDate, x.EndDate })
                .HasDatabaseName("ix_sales_targets_period_rep")
                .HasFilter("\"IsDeleted\" = false");

            builder.HasIndex(x => new { x.TenantId, x.ParentTargetId, x.Status })
                .HasDatabaseName("ix_sales_targets_parent_hierarchy")
                .HasFilter("\"IsDeleted\" = false");

            builder.HasIndex(x => new { x.TenantId, x.Branch, x.FiscalYear, x.Status })
                .HasDatabaseName("ix_sales_targets_branch_fy_perf");

            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.TargetCategory);
            builder.HasIndex(x => x.SalesPersonUserId);
            builder.HasIndex(x => x.StartDate);
            builder.HasIndex(x => x.EndDate);
            builder.HasIndex(x => x.IsDeleted);

            // Navigation Configurations
            builder.HasOne(x => x.ParentTarget)
                .WithMany(x => x.ChildTargets)
                .HasForeignKey(x => x.ParentTargetId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Items)
                .WithOne(x => x.SalesTarget)
                .HasForeignKey(x => x.SalesTargetId)
                .OnDelete(DeleteBehavior.Cascade);

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

            builder.HasMany(x => x.Realizations)
                .WithOne(x => x.Target)
                .HasForeignKey(x => x.SalesTargetId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.ApprovalHistories)
                .WithOne(x => x.SalesTarget)
                .HasForeignKey(x => x.SalesTargetId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class SalesTargetRealizationConfiguration : IEntityTypeConfiguration<SalesTargetRealization>
    {
        public void Configure(EntityTypeBuilder<SalesTargetRealization> builder)
        {
            builder.ToTable("sales_target_realizations");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TenantId).HasMaxLength(64).HasDefaultValue("default").IsRequired();
            builder.Property(x => x.RealizationType).HasMaxLength(64).IsRequired();
            builder.Property(x => x.RealizedAmount).HasPrecision(18, 2);
            builder.Property(x => x.RealizedQuantity).HasPrecision(18, 4);
            builder.Property(x => x.MarginContributionAmount).HasPrecision(18, 2);
            builder.Property(x => x.SourceCurrency).HasMaxLength(8).HasDefaultValue("INR");
            builder.Property(x => x.SourceAmount).HasPrecision(18, 2);
            builder.Property(x => x.LockedFXRate).HasPrecision(18, 6).HasDefaultValue(1.000000m);
            builder.Property(x => x.ReversalReason).HasMaxLength(500);
            builder.Property(x => x.AppliedBy).HasMaxLength(100).IsRequired();

            builder.HasIndex(x => new { x.TenantId, x.SalesTargetId, x.RealizationType, x.TransactionDate })
                .HasDatabaseName("ix_target_realizations_target_type_date");

            builder.HasIndex(x => new { x.TenantId, x.SalesOrderId, x.IsReversal })
                .HasDatabaseName("ix_target_realizations_order_lookup");

            builder.HasIndex(x => new { x.TenantId, x.AdvancePaymentId, x.IsReversal })
                .HasDatabaseName("ix_target_realizations_advance_lookup");
        }
    }

    public class SalesTargetItemConfiguration : IEntityTypeConfiguration<SalesTargetItem>
    {
        public void Configure(EntityTypeBuilder<SalesTargetItem> builder)
        {
            builder.ToTable("sales_target_items");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TenantId).HasMaxLength(64).HasDefaultValue("default").IsRequired();
            builder.Property(x => x.CategoryName).HasMaxLength(128);
            builder.Property(x => x.ItemCode).HasMaxLength(64);
            builder.Property(x => x.ItemDescription).HasMaxLength(256);
            builder.Property(x => x.TargetQuantity).HasPrecision(18, 4);
            builder.Property(x => x.TargetRate).HasPrecision(18, 2);
            builder.Property(x => x.TargetValue).HasPrecision(18, 2);
            builder.Property(x => x.RealizedQuantity).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.RealizedValue).HasPrecision(18, 2).HasDefaultValue(0m);
            builder.Property(x => x.MinimumMarginPercentage).HasPrecision(5, 2);

            builder.HasIndex(x => new { x.TenantId, x.SalesTargetId, x.ProductCategoryId })
                .HasDatabaseName("ix_sales_target_items_cat_lookup");
        }
    }

    public class SalesTargetSplitRuleConfiguration : IEntityTypeConfiguration<SalesTargetSplitRule>
    {
        public void Configure(EntityTypeBuilder<SalesTargetSplitRule> builder)
        {
            builder.ToTable("sales_target_split_rules");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TenantId).HasMaxLength(64).HasDefaultValue("default").IsRequired();
            builder.Property(x => x.RuleName).HasMaxLength(128).IsRequired();
            builder.Property(x => x.LeadGeneratorPercentage).HasPrecision(5, 2);
            builder.Property(x => x.ClosingRepPercentage).HasPrecision(5, 2);
            builder.Property(x => x.TechnicalConsultantPercentage).HasPrecision(5, 2);

            builder.HasIndex(x => new { x.TenantId, x.RuleName }).IsUnique();
        }
    }

    public class SalesTargetApprovalHistoryConfiguration : IEntityTypeConfiguration<SalesTargetApprovalHistory>
    {
        public void Configure(EntityTypeBuilder<SalesTargetApprovalHistory> builder)
        {
            builder.ToTable("sales_target_approval_histories");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TenantId).HasMaxLength(64).HasDefaultValue("default").IsRequired();
            builder.Property(x => x.Action).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ApproverUserId).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ApproverName).HasMaxLength(128).IsRequired();
            builder.Property(x => x.ApproverRole).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Comments).HasMaxLength(1000);
            builder.Property(x => x.HmacSignatureToken).HasMaxLength(256);

            builder.HasIndex(x => new { x.TenantId, x.SalesTargetId, x.Timestamp })
                .HasDatabaseName("ix_target_approval_hist_lookup");
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
