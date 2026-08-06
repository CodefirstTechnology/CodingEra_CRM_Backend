using ERP.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class QuotationApprovalCommentConfiguration : IEntityTypeConfiguration<QuotationApprovalComment>
    {
        public void Configure(EntityTypeBuilder<QuotationApprovalComment> builder)
        {
            builder.ToTable("QuotationApprovalComments");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Comment).IsRequired().HasMaxLength(4000);
            builder.Property(x => x.CommentedBy).IsRequired().HasMaxLength(100);

            builder.HasOne(x => x.QuotationApproval)
                .WithMany(x => x.Comments)
                .HasForeignKey(x => x.QuotationApprovalId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
