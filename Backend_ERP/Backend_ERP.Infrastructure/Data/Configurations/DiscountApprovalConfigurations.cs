using ERP.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class DiscountApprovalConfiguration : IEntityTypeConfiguration<DiscountApproval>
    {
        public void Configure(EntityTypeBuilder<DiscountApproval> builder)
        {
            builder.ToTable("discount_approvals");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ApprovalNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.ApprovalNumber).IsUnique();

            builder.Property(x => x.SourceType).HasMaxLength(64).IsRequired();
            builder.Property(x => x.QuotationNumber).HasMaxLength(64);
            builder.Property(x => x.SalesOrderNumber).HasMaxLength(64);
            builder.Property(x => x.CustomerName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.CustomerCategory).HasMaxLength(64).IsRequired();
            builder.Property(x => x.RequestedDiscountPercentage).HasPrecision(9, 4);
            builder.Property(x => x.ApprovedDiscountPercentage).HasPrecision(9, 4);
            builder.Property(x => x.RequestedAmount).HasPrecision(18, 2);
            builder.Property(x => x.ApprovedAmount).HasPrecision(18, 2);
            builder.Property(x => x.ApprovalLevel).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Priority).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Status).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Reason).HasMaxLength(2000);
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.CreatedBy).HasMaxLength(64);
            builder.Property(x => x.UpdatedBy).HasMaxLength(64);

            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.Priority);
            builder.HasIndex(x => x.ApprovalLevel);
            builder.HasIndex(x => x.SourceType);
            builder.HasIndex(x => x.CustomerCategory);
            builder.HasIndex(x => x.SalesPersonUserId);
            builder.HasIndex(x => x.RequestDate);
            builder.HasIndex(x => x.IsDeleted);
            builder.HasIndex(x => x.SalesOrderId);
            builder.HasIndex(x => x.PriceListId);
            builder.HasIndex(x => x.QuotationId);

            builder.HasMany(x => x.History)
                .WithOne(x => x.DiscountApproval)
                .HasForeignKey(x => x.DiscountApprovalId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Comments)
                .WithOne(x => x.DiscountApproval)
                .HasForeignKey(x => x.DiscountApprovalId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class DiscountApprovalHistoryConfiguration : IEntityTypeConfiguration<DiscountApprovalHistory>
    {
        public void Configure(EntityTypeBuilder<DiscountApprovalHistory> builder)
        {
            builder.ToTable("discount_approval_history");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Action).HasMaxLength(64).IsRequired();
            builder.Property(x => x.OldStatus).HasMaxLength(64);
            builder.Property(x => x.NewStatus).HasMaxLength(64);
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.PerformedBy).HasMaxLength(64);

            builder.HasIndex(x => new { x.DiscountApprovalId, x.PerformedOn });
        }
    }

    public class DiscountApprovalCommentConfiguration : IEntityTypeConfiguration<DiscountApprovalComment>
    {
        public void Configure(EntityTypeBuilder<DiscountApprovalComment> builder)
        {
            builder.ToTable("discount_approval_comments");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Comment).HasMaxLength(2000).IsRequired();
            builder.Property(x => x.CommentedBy).HasMaxLength(64);

            builder.HasIndex(x => new { x.DiscountApprovalId, x.CommentedOn });
        }
    }

    public class DiscountApprovalDocumentSequenceConfiguration
        : IEntityTypeConfiguration<DiscountApprovalDocumentSequence>
    {
        public void Configure(EntityTypeBuilder<DiscountApprovalDocumentSequence> builder)
        {
            builder.ToTable("discount_approval_document_sequences");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Prefix).HasMaxLength(16).IsRequired();
            builder.HasIndex(x => new { x.FinancialYear, x.Prefix }).IsUnique();
        }
    }
}
