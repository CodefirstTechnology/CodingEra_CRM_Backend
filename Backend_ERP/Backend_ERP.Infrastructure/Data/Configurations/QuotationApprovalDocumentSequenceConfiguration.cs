using ERP.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class QuotationApprovalDocumentSequenceConfiguration : IEntityTypeConfiguration<QuotationApprovalDocumentSequence>
    {
        public void Configure(EntityTypeBuilder<QuotationApprovalDocumentSequence> builder)
        {
            builder.ToTable("QuotationApprovalDocumentSequences");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Prefix).IsRequired().HasMaxLength(10);
            
            builder.HasIndex(x => new { x.FinancialYear, x.Prefix }).IsUnique();
        }
    }
}
