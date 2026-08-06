using ERP.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class QuotationApprovalHistoryConfiguration : IEntityTypeConfiguration<QuotationApprovalHistory>
    {
        public void Configure(EntityTypeBuilder<QuotationApprovalHistory> builder)
        {
            builder.ToTable("QuotationApprovalHistories");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Action).IsRequired().HasMaxLength(50);
            builder.Property(x => x.OldStatus).HasMaxLength(50);
            builder.Property(x => x.NewStatus).IsRequired().HasMaxLength(50);
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.PerformedBy).IsRequired().HasMaxLength(100);

            builder.HasOne(x => x.QuotationApproval)
                .WithMany(x => x.History)
                .HasForeignKey(x => x.QuotationApprovalId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
