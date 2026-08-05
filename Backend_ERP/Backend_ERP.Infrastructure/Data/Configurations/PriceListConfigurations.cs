using ERP.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class PriceListConfiguration : IEntityTypeConfiguration<PriceList>
    {
        public void Configure(EntityTypeBuilder<PriceList> builder)
        {
            builder.ToTable("price_lists");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.PriceListNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.PriceListNumber).IsUnique();

            builder.Property(x => x.PriceListName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(2000);
            builder.Property(x => x.CustomerCategory).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Currency).HasMaxLength(8).IsRequired();
            builder.Property(x => x.Status).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.CreatedBy).HasMaxLength(64);
            builder.Property(x => x.UpdatedBy).HasMaxLength(64);

            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.CustomerCategory);
            builder.HasIndex(x => x.Currency);
            builder.HasIndex(x => x.EffectiveFrom);
            builder.HasIndex(x => x.IsDeleted);
            builder.HasIndex(x => new { x.PriceListName, x.CustomerCategory, x.Currency, x.Status });

            builder.HasMany(x => x.Items)
                .WithOne(x => x.PriceList)
                .HasForeignKey(x => x.PriceListId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.History)
                .WithOne(x => x.PriceList)
                .HasForeignKey(x => x.PriceListId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class PriceListItemConfiguration : IEntityTypeConfiguration<PriceListItem>
    {
        public void Configure(EntityTypeBuilder<PriceListItem> builder)
        {
            builder.ToTable("price_list_items");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ItemCode).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ItemName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.ItemCategory).HasMaxLength(128);
            builder.Property(x => x.Unit).HasMaxLength(32);
            builder.Property(x => x.BasePrice).HasPrecision(18, 2);
            builder.Property(x => x.SellingPrice).HasPrecision(18, 2);
            builder.Property(x => x.DiscountPercentage).HasPrecision(9, 4);
            builder.Property(x => x.MinimumPrice).HasPrecision(18, 2);
            builder.Property(x => x.MaximumDiscount).HasPrecision(9, 4);
            builder.Property(x => x.TaxPercentage).HasPrecision(9, 4);
            builder.Property(x => x.Remarks).HasMaxLength(2000);

            builder.HasIndex(x => new { x.PriceListId, x.SortOrder });
            builder.HasIndex(x => new { x.PriceListId, x.ItemCode });
            builder.HasIndex(x => x.ItemCode);
        }
    }

    public class PriceListHistoryConfiguration : IEntityTypeConfiguration<PriceListHistory>
    {
        public void Configure(EntityTypeBuilder<PriceListHistory> builder)
        {
            builder.ToTable("price_list_history");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Action).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.ChangedBy).HasMaxLength(64);

            builder.HasIndex(x => new { x.PriceListId, x.ChangedOn });
        }
    }

    public class PriceListDocumentSequenceConfiguration : IEntityTypeConfiguration<PriceListDocumentSequence>
    {
        public void Configure(EntityTypeBuilder<PriceListDocumentSequence> builder)
        {
            builder.ToTable("price_list_document_sequences");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Prefix).HasMaxLength(16).IsRequired();
            builder.HasIndex(x => new { x.FinancialYear, x.Prefix }).IsUnique();
        }
    }
}
