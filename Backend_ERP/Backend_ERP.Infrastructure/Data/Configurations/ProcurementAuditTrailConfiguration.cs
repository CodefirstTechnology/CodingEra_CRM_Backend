using ERP.Domain.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class ProcurementAuditTrailConfiguration : IEntityTypeConfiguration<ProcurementAuditTrailEntry>
    {
        public void Configure(EntityTypeBuilder<ProcurementAuditTrailEntry> builder)
        {
            builder.ToTable("procurement_audit_trail_entries");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Module).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Action).HasMaxLength(128).IsRequired();
            builder.Property(x => x.EntityNumber).HasMaxLength(64);
            builder.Property(x => x.User).HasMaxLength(256);
            builder.Property(x => x.OldValue).HasMaxLength(1000);
            builder.Property(x => x.NewValue).HasMaxLength(1000);
            builder.Property(x => x.Remarks).HasMaxLength(2000);

            builder.HasIndex(x => x.Module);
            builder.HasIndex(x => x.Action);
            builder.HasIndex(x => x.EntityId);
            builder.HasIndex(x => x.EntityNumber);
        }
    }
}
