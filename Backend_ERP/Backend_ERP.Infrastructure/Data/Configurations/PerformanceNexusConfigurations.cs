using ERP.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class SalespersonPerformanceConfiguration : IEntityTypeConfiguration<SalespersonPerformance>
    {
        public void Configure(EntityTypeBuilder<SalespersonPerformance> builder)
        {
            builder.ToTable("salesperson_performances");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired().HasDefaultValue("default");
            builder.Property(x => x.SalesPersonName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.SalesTeam).HasMaxLength(128);
            builder.Property(x => x.Branch).HasMaxLength(128);
            builder.Property(x => x.RegionalManager).HasMaxLength(128);
            builder.Property(x => x.PeriodType).HasMaxLength(32).HasDefaultValue("FiscalYear");
            builder.Property(x => x.TierBadge).HasMaxLength(32).HasDefaultValue("Silver");
            builder.Property(x => x.Status).HasMaxLength(32).HasDefaultValue("Active");

            builder.Property(x => x.TotalTargetValue).HasPrecision(18, 2);
            builder.Property(x => x.TotalBookedRevenue).HasPrecision(18, 2);
            builder.Property(x => x.TotalCashCollected).HasPrecision(18, 2);
            builder.Property(x => x.TotalDispatchedRevenue).HasPrecision(18, 2);
            builder.Property(x => x.TotalGrossMarginAmount).HasPrecision(18, 2);
            builder.Property(x => x.GrossMarginPercentage).HasPrecision(7, 2);
            builder.Property(x => x.AttainmentPercentage).HasPrecision(7, 2);
            builder.Property(x => x.PacingIndex).HasPrecision(7, 2);
            builder.Property(x => x.ConversionRate).HasPrecision(7, 2);
            builder.Property(x => x.AvgDealVelocityDays).HasPrecision(7, 2);
            builder.Property(x => x.DiscountDisciplineScore).HasPrecision(7, 2);
            builder.Property(x => x.WeightedScore).HasPrecision(7, 2);
            builder.Property(x => x.TotalAccruedCommission).HasPrecision(18, 2);
            builder.Property(x => x.TotalClawbackCommission).HasPrecision(18, 2);
            builder.Property(x => x.TotalPaidCommission).HasPrecision(18, 2);

            builder.Property(x => x.Version).IsRowVersion();

            builder.HasIndex(x => new { x.TenantId, x.FinancialYear, x.SalesPersonUserId })
                   .IsUnique()
                   .HasDatabaseName("ix_perf_profiles_tenant_fy_user");

            builder.HasIndex(x => new { x.TenantId, x.FinancialYear, x.WeightedScore })
                   .HasDatabaseName("ix_perf_profiles_leaderboard_rank");

            builder.HasIndex(x => new { x.TenantId, x.Branch, x.FinancialYear })
                   .HasDatabaseName("ix_perf_profiles_branch_fy");

            builder.HasIndex(x => new { x.TenantId, x.SalesTeam, x.FinancialYear })
                   .HasDatabaseName("ix_perf_profiles_team_fy");
        }
    }

    public class SalesIncentiveSchemeConfiguration : IEntityTypeConfiguration<SalesIncentiveScheme>
    {
        public void Configure(EntityTypeBuilder<SalesIncentiveScheme> builder)
        {
            builder.ToTable("sales_incentive_schemes");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired().HasDefaultValue("default");
            builder.Property(x => x.SchemeCode).HasMaxLength(64).IsRequired();
            builder.Property(x => x.SchemeName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.PeriodType).HasMaxLength(32).HasDefaultValue("Monthly");
            builder.Property(x => x.Status).HasMaxLength(32).HasDefaultValue("Active");

            builder.Property(x => x.BaseCommissionRate).HasPrecision(5, 2);
            builder.Property(x => x.MarginHurdlePercentage).HasPrecision(5, 2);
            builder.Property(x => x.MinimumAttainmentThreshold).HasPrecision(5, 2);
            builder.Property(x => x.MinCashClearancePercentage).HasPrecision(5, 2);
            builder.Property(x => x.AcceleratorTiersJson).HasColumnType("jsonb").IsRequired();

            builder.Property(x => x.Version).IsRowVersion();

            builder.HasIndex(x => new { x.TenantId, x.SchemeCode, x.FiscalYear })
                   .IsUnique()
                   .HasDatabaseName("ix_incentive_schemes_tenant_code_fy");
        }
    }

    public class SalesCommissionLedgerConfiguration : IEntityTypeConfiguration<SalesCommissionLedger>
    {
        public void Configure(EntityTypeBuilder<SalesCommissionLedger> builder)
        {
            builder.ToTable("sales_commission_ledgers");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired().HasDefaultValue("default");
            builder.Property(x => x.TransactionType).HasMaxLength(64).IsRequired();
            builder.Property(x => x.PayoutStatus).HasMaxLength(32).HasDefaultValue("Accrued");
            builder.Property(x => x.ClawbackReason).HasMaxLength(512);
            builder.Property(x => x.ApprovalToken).HasMaxLength(128);
            builder.Property(x => x.ApprovedByUserId).HasMaxLength(64);
            builder.Property(x => x.PaymentVoucherReference).HasMaxLength(128);

            builder.Property(x => x.TransactionAmount).HasPrecision(18, 2);
            builder.Property(x => x.CommissionRate).HasPrecision(5, 2);
            builder.Property(x => x.AcceleratorMultiplier).HasPrecision(4, 2);
            builder.Property(x => x.AccruedCommission).HasPrecision(18, 2);

            builder.HasOne(x => x.Profile)
                   .WithMany(p => p.CommissionEntries)
                   .HasForeignKey(x => x.ProfileId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.IncentiveScheme)
                   .WithMany()
                   .HasForeignKey(x => x.IncentiveSchemeId)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(x => new { x.TenantId, x.SalesPersonUserId, x.PayoutStatus })
                   .HasDatabaseName("ix_commission_ledger_rep_status");

            builder.HasIndex(x => new { x.TenantId, x.SalesOrderId })
                   .HasDatabaseName("ix_commission_ledger_order");
        }
    }

    public class SalesRepBadgeConfiguration : IEntityTypeConfiguration<SalesRepBadge>
    {
        public void Configure(EntityTypeBuilder<SalesRepBadge> builder)
        {
            builder.ToTable("sales_rep_badges");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired().HasDefaultValue("default");
            builder.Property(x => x.BadgeCode).HasMaxLength(64).IsRequired();
            builder.Property(x => x.BadgeTitle).HasMaxLength(128).IsRequired();
            builder.Property(x => x.BadgeCategory).HasMaxLength(64).HasDefaultValue("Quota");
            builder.Property(x => x.Icon).HasMaxLength(64).HasDefaultValue("trophy");
            builder.Property(x => x.Color).HasMaxLength(64).HasDefaultValue("emerald");
            builder.Property(x => x.EvaluationCriteriaSnapshot).HasColumnType("jsonb").IsRequired();

            builder.HasOne(x => x.Profile)
                   .WithMany(p => p.Badges)
                   .HasForeignKey(x => x.ProfileId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.TenantId, x.SalesPersonUserId, x.BadgeCode })
                   .HasDatabaseName("ix_rep_badges_user_code");
        }
    }

    public class SalesLeaderboardSnapshotConfiguration : IEntityTypeConfiguration<SalesLeaderboardSnapshot>
    {
        public void Configure(EntityTypeBuilder<SalesLeaderboardSnapshot> builder)
        {
            builder.ToTable("sales_leaderboard_snapshots");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired().HasDefaultValue("default");
            builder.Property(x => x.PeriodKey).HasMaxLength(32).IsRequired();
            builder.Property(x => x.PartitionType).HasMaxLength(32).HasDefaultValue("Global");
            builder.Property(x => x.PartitionValue).HasMaxLength(128).HasDefaultValue("ALL");
            builder.Property(x => x.SalesPersonName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.TierBadge).HasMaxLength(32).HasDefaultValue("Silver");

            builder.Property(x => x.WeightedScore).HasPrecision(7, 2);
            builder.Property(x => x.AttainmentPercentage).HasPrecision(7, 2);
            builder.Property(x => x.TotalBookedRevenue).HasPrecision(18, 2);
            builder.Property(x => x.TotalCashCollected).HasPrecision(18, 2);
            builder.Property(x => x.GrossMarginPercentage).HasPrecision(7, 2);

            builder.HasIndex(x => new { x.TenantId, x.FinancialYear, x.PeriodKey, x.PartitionType, x.PartitionValue, x.Rank })
                   .HasDatabaseName("ix_leaderboard_snapshots_period_rank");
        }
    }
}

