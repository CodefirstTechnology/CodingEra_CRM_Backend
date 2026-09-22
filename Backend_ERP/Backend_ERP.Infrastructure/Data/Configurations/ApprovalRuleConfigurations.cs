using ERP.Domain.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class ApprovalRuleConfiguration : IEntityTypeConfiguration<ApprovalRule>
    {
        public void Configure(EntityTypeBuilder<ApprovalRule> builder)
        {
            builder.ToTable("approval_rules");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
            builder.Property(x => x.DocumentType).HasMaxLength(64).IsRequired();
            builder.Property(x => x.IsActive).HasDefaultValue(true);

            builder.HasMany(x => x.Tiers)
                .WithOne(x => x.ApprovalRule)
                .HasForeignKey(x => x.ApprovalRuleId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class ApprovalRuleTierConfiguration : IEntityTypeConfiguration<ApprovalRuleTier>
    {
        public void Configure(EntityTypeBuilder<ApprovalRuleTier> builder)
        {
            builder.ToTable("approval_rule_tiers");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.RequiredRole).HasMaxLength(128).IsRequired();
            builder.Property(x => x.MinAmount).HasPrecision(18, 2).HasDefaultValue(0m);
            builder.Property(x => x.MaxAmount).HasPrecision(18, 2).HasDefaultValue(0m);

            builder.HasIndex(x => new { x.ApprovalRuleId, x.SequenceNumber }).IsUnique();
        }
    }
}
