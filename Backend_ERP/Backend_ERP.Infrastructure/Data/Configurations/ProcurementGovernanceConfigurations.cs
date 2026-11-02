using ERP.Domain.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class ProcurementWorkflowStageLogConfiguration : IEntityTypeConfiguration<ProcurementWorkflowStageLog>
    {
        public void Configure(EntityTypeBuilder<ProcurementWorkflowStageLog> builder)
        {
            builder.ToTable("procurement_workflow_stage_logs");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired().HasDefaultValue("TENANT-DEFAULT");
            builder.Property(x => x.EntityType).HasMaxLength(32).IsRequired();
            builder.Property(x => x.DocumentNumber).HasMaxLength(64).IsRequired();
            builder.Property(x => x.StageName).HasMaxLength(128).IsRequired();
            builder.Property(x => x.ElapsedBusinessHours).HasPrecision(8, 2);
            builder.Property(x => x.TargetSlaHours).HasPrecision(8, 2);
            builder.Property(x => x.DepartmentName).HasMaxLength(64);
            builder.Property(x => x.ActionedByUserName).HasMaxLength(128);

            builder.HasIndex(x => new { x.TenantId, x.EntityType, x.IsSlaBreached, x.StartedAt })
                .HasDatabaseName("idx_procurement_stage_perf");
        }
    }

    public class ProcurementAuditLogConfiguration : IEntityTypeConfiguration<ProcurementAuditLog>
    {
        public void Configure(EntityTypeBuilder<ProcurementAuditLog> builder)
        {
            builder.ToTable("procurement_audit_logs");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired().HasDefaultValue("TENANT-DEFAULT");
            builder.Property(x => x.EntityName).HasMaxLength(64).IsRequired();
            builder.Property(x => x.EntityId).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ActionType).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ChangedByUser).HasMaxLength(128).IsRequired();
            builder.Property(x => x.UserIpAddress).HasMaxLength(45);

            builder.Property(x => x.OldValuesJson).HasColumnType("jsonb");
            builder.Property(x => x.NewValuesJson).HasColumnType("jsonb");

            builder.Property(x => x.PreviousLogHash).HasMaxLength(64).IsRequired();
            builder.Property(x => x.CurrentLogHash).HasMaxLength(64).IsRequired();

            builder.HasIndex(x => new { x.TenantId, x.BlockIndex })
                .IsUnique()
                .HasDatabaseName("idx_procurement_audit_chain");

            builder.HasIndex(x => new { x.TenantId, x.EntityName, x.EntityId, x.Timestamp })
                .HasDatabaseName("idx_procurement_audit_entity");
        }
    }
}

