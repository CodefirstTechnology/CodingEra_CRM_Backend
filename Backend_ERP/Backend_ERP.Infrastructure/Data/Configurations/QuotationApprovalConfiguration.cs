using ERP.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class QuotationApprovalConfiguration : IEntityTypeConfiguration<QuotationApproval>
    {
        public void Configure(EntityTypeBuilder<QuotationApproval> builder)
        {
            builder.ToTable("QuotationApprovals");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ApprovalNumber).IsRequired().HasMaxLength(50);
            builder.HasIndex(x => x.ApprovalNumber).IsUnique();

            builder.Property(x => x.QuotationNumber).IsRequired().HasMaxLength(50);
            builder.HasIndex(x => x.QuotationNumber);

            builder.Property(x => x.SalesOrderNumber).HasMaxLength(50);
            builder.Property(x => x.CustomerName).IsRequired().HasMaxLength(255);
            builder.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.ApprovalLevel).IsRequired().HasMaxLength(50);
            builder.Property(x => x.Priority).IsRequired().HasMaxLength(50);
            builder.Property(x => x.Status).IsRequired().HasMaxLength(50);
            builder.Property(x => x.Reason).HasMaxLength(2000);
            builder.Property(x => x.Remarks).HasMaxLength(2000);

            builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(100);
            builder.Property(x => x.UpdatedBy).HasMaxLength(100);

            builder.HasIndex(x => x.RequestDate);
            builder.HasIndex(x => x.SalesPersonUserId);
            builder.HasIndex(x => x.Status);
        }
    }
}
