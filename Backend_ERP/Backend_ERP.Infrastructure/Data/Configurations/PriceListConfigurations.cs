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

            builder.Property(x => x.Version).IsRowVersion();

            builder.Property(x => x.TenantId).HasMaxLength(64).HasDefaultValue("default").IsRequired();
            builder.Property(x => x.PriceListNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.PriceListNumber).IsUnique();

            builder.Property(x => x.PriceListName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(2000);
            builder.Property(x => x.TierType).HasConversion<string>().HasMaxLength(32).HasDefaultValue(PriceListTierType.Retail);
            builder.Property(x => x.Channel).HasConversion<string>().HasMaxLength(32).HasDefaultValue(SalesChannelType.Direct);
            builder.Property(x => x.CustomerCategory).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Currency).HasMaxLength(8).IsRequired();
            builder.Property(x => x.Status).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Priority).HasDefaultValue(0).IsRequired();
            builder.Property(x => x.SlabStrategy).HasConversion<string>().HasMaxLength(32).HasDefaultValue(VolumeSlabResolutionStrategy.HighestQualifyingFloor);
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.CreatedBy).HasMaxLength(64);
            builder.Property(x => x.UpdatedBy).HasMaxLength(64);

            builder.Property(x => x.ValidityPeriod)
                .HasColumnType("daterange")
                .IsRequired();

            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.CustomerCategory);
            builder.HasIndex(x => x.Currency);
            builder.HasIndex(x => x.EffectiveFrom);
            builder.HasIndex(x => x.Priority);
            builder.HasIndex(x => x.IsDeleted);
            builder.HasIndex(x => new { x.TenantId, x.Status, x.TierType, x.Currency, x.Priority });
            builder.HasIndex(x => new { x.PriceListName, x.CustomerCategory, x.Currency, x.Status });
            builder.HasIndex(x => new { x.Status, x.CustomerCategory, x.Currency, x.Priority, x.EffectiveFrom });

            builder.HasMany(x => x.Items)
                .WithOne(x => x.PriceList)
                .HasForeignKey(x => x.PriceListId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.CustomerMappings)
                .WithOne(x => x.PriceList)
                .HasForeignKey(x => x.PriceListId)
                .OnDelete(DeleteBehavior.Restrict);

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

            builder.Property(x => x.Version).IsRowVersion();

            builder.Property(x => x.TenantId).HasMaxLength(64).HasDefaultValue("default").IsRequired();
            builder.Property(x => x.ItemType).HasConversion<string>().HasMaxLength(32).HasDefaultValue(PricingItemType.FinishedGood);
            builder.Property(x => x.ItemCode).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ItemName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.ItemCategory).HasMaxLength(128);
            builder.Property(x => x.Unit).HasMaxLength(32);

            builder.Property(x => x.BaseUom).HasMaxLength(16).HasDefaultValue("Nos").IsRequired();
            builder.Property(x => x.PricingUom).HasMaxLength(16).HasDefaultValue("Nos").IsRequired();
            builder.Property(x => x.UomConversionFactor).HasPrecision(12, 4).HasDefaultValue(1.0000m);

            builder.Property(x => x.BasePrice).HasPrecision(18, 2);
            builder.Property(x => x.SellingPrice).HasPrecision(18, 2);
            builder.Property(x => x.DiscountPercentage).HasPrecision(9, 4);
            builder.Property(x => x.MinimumPrice).HasPrecision(18, 2);
            builder.Property(x => x.FloorPrice).HasPrecision(18, 2);
            builder.Property(x => x.MaximumDiscount).HasPrecision(9, 4);
            builder.Property(x => x.TaxPercentage).HasPrecision(9, 4);
            builder.Property(x => x.HsnSacCode).HasMaxLength(16);
            builder.Property(x => x.Remarks).HasMaxLength(2000);

            builder.HasIndex(x => new { x.PriceListId, x.SortOrder });
            builder.HasIndex(x => new { x.PriceListId, x.ItemCode });
            builder.HasIndex(x => x.ItemCode);
            builder.HasIndex(x => new { x.TenantId, x.ItemCode });

            builder.HasMany(x => x.Slabs)
                .WithOne(x => x.PriceListItem)
                .HasForeignKey(x => x.PriceListItemId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class PriceListItemSlabConfiguration : IEntityTypeConfiguration<PriceListItemSlab>
    {
        public void Configure(EntityTypeBuilder<PriceListItemSlab> builder)
        {
            builder.ToTable("price_list_item_slabs");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TenantId).HasMaxLength(64).HasDefaultValue("default").IsRequired();
            builder.Property(x => x.MinQuantity).HasPrecision(12, 3);
            builder.Property(x => x.MaxQuantity).HasPrecision(12, 3);
            builder.Property(x => x.Rate).HasPrecision(18, 2);
            builder.Property(x => x.DiscountPercentage).HasPrecision(9, 4);

            builder.HasIndex(x => new { x.PriceListItemId, x.MinQuantity });
        }
    }

    public class CustomerPriceListMappingConfiguration : IEntityTypeConfiguration<CustomerPriceListMapping>
    {
        public void Configure(EntityTypeBuilder<CustomerPriceListMapping> builder)
        {
            builder.ToTable("customer_price_list_mappings");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TenantId).HasMaxLength(64).HasDefaultValue("default").IsRequired();
            builder.Property(x => x.CustomerId).HasMaxLength(64).IsRequired();
            builder.Property(x => x.CustomerName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.EffectiveRange).HasColumnType("daterange").IsRequired();

            builder.HasIndex(x => new { x.TenantId, x.CustomerId, x.IsActive, x.PriorityWeight });
        }
    }

    public class PriceListHistoryConfiguration : IEntityTypeConfiguration<PriceListHistory>
    {
        public void Configure(EntityTypeBuilder<PriceListHistory> builder)
        {
            builder.ToTable("price_list_history");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Action).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Priority);
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
