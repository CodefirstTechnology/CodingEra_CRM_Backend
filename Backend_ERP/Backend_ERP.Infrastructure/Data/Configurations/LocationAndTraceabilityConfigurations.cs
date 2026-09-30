using Backend_ERP.Domain.Entities;
using ERP.Domain.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend_ERP.Infrastructure.Data.Configurations
{
    public class WarehouseEntityConfiguration : IEntityTypeConfiguration<WarehouseEntity>
    {
        public void Configure(EntityTypeBuilder<WarehouseEntity> builder)
        {
            builder.ToTable("warehouse_entities");
            builder.HasKey(x => x.WarehouseId);

            builder.Property(x => x.WarehouseId).HasColumnName("warehouse_id");
            builder.Property(x => x.WarehouseCode).HasColumnName("warehouse_code").HasMaxLength(50).IsRequired();
            builder.Property(x => x.WarehouseName).HasColumnName("warehouse_name").HasMaxLength(200).IsRequired();
            builder.Property(x => x.Address).HasColumnName("address").HasMaxLength(500);
            builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");

            builder.Property(x => x.RowVersion)
                .HasColumnName("xmin")
                .HasColumnType("uint")
                .ValueGeneratedOnAddOrUpdate()
                .IsRowVersion();

            builder.HasMany(x => x.Zones)
                .WithOne(x => x.Warehouse)
                .HasForeignKey(x => x.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class WarehouseZoneEntityConfiguration : IEntityTypeConfiguration<WarehouseZoneEntity>
    {
        public void Configure(EntityTypeBuilder<WarehouseZoneEntity> builder)
        {
            builder.ToTable("warehouse_zones");
            builder.HasKey(x => x.ZoneId);

            builder.Property(x => x.ZoneId).HasColumnName("zone_id");
            builder.Property(x => x.WarehouseId).HasColumnName("warehouse_id").IsRequired();
            builder.Property(x => x.ZoneCode).HasColumnName("zone_code").HasMaxLength(50).IsRequired();
            builder.Property(x => x.ZoneName).HasColumnName("zone_name").HasMaxLength(200).IsRequired();
            builder.Property(x => x.ZoneType).HasColumnName("zone_type").HasConversion<string>();
            builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");

            builder.HasMany(x => x.Bins)
                .WithOne(x => x.Zone)
                .HasForeignKey(x => x.ZoneId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class WarehouseBinEntityConfiguration : IEntityTypeConfiguration<WarehouseBinEntity>
    {
        public void Configure(EntityTypeBuilder<WarehouseBinEntity> builder)
        {
            builder.ToTable("warehouse_bins");
            builder.HasKey(x => x.BinId);

            builder.Property(x => x.BinId).HasColumnName("bin_id");
            builder.Property(x => x.ZoneId).HasColumnName("zone_id").IsRequired();
            builder.Property(x => x.WarehouseId).HasColumnName("warehouse_id").IsRequired();
            builder.Property(x => x.BinCode).HasColumnName("bin_code").HasMaxLength(50).IsRequired();
            builder.Property(x => x.MaxWeightKg).HasColumnName("max_weight_kg").HasColumnType("numeric(18,4)");
            builder.Property(x => x.MaxVolumeM3).HasColumnName("max_volume_m3").HasColumnType("numeric(18,4)");
            builder.Property(x => x.CurrentWeightKg).HasColumnName("current_weight_kg").HasColumnType("numeric(18,4)");
            builder.Property(x => x.CurrentVolumeM3).HasColumnName("current_volume_m3").HasColumnType("numeric(18,4)");
            builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>();
            builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        }
    }

    public class ItemBatchEntityConfiguration : IEntityTypeConfiguration<ItemBatchEntity>
    {
        public void Configure(EntityTypeBuilder<ItemBatchEntity> builder)
        {
            builder.ToTable("item_batches");
            builder.HasKey(x => x.BatchId);

            builder.Property(x => x.BatchId).HasColumnName("batch_id");
            builder.Property(x => x.BatchNumber).HasColumnName("batch_number").HasMaxLength(100).IsRequired();
            builder.Property(x => x.ItemId).HasColumnName("item_id").IsRequired();
            builder.Property(x => x.ManufactureDate).HasColumnName("manufacture_date");
            builder.Property(x => x.ExpiryDate).HasColumnName("expiry_date");
            builder.Property(x => x.RetestDate).HasColumnName("retest_date");
            builder.Property(x => x.BestBeforeDate).HasColumnName("best_before_date");
            builder.Property(x => x.QcStatus).HasColumnName("qc_status").HasMaxLength(30).HasDefaultValue("Released");
            builder.Property(x => x.IsLocked).HasColumnName("is_locked").HasDefaultValue(false);
            builder.Property(x => x.SupplierLotNo).HasColumnName("supplier_lot_no").HasMaxLength(100);
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");

            builder.Property(x => x.RowVersion)
                .HasColumnName("xmin")
                .HasColumnType("uint")
                .ValueGeneratedOnAddOrUpdate()
                .IsRowVersion();
        }
    }

    public class StockTransferHardeningConfiguration : IEntityTypeConfiguration<StockTransfer>
    {
        public void Configure(EntityTypeBuilder<StockTransfer> builder)
        {
            builder.Property(x => x.DispatchDate).HasColumnName("DispatchDate");
            builder.Property(x => x.ReceiptDate).HasColumnName("ReceiptDate");
            builder.Property(x => x.EwayBillNumber).HasColumnName("EwayBillNumber").HasMaxLength(100);
            builder.Property(x => x.CarrierName).HasColumnName("CarrierName").HasMaxLength(100);
            builder.Property(x => x.VehicleNumber).HasColumnName("VehicleNumber").HasMaxLength(50);
            builder.Property(x => x.CreatedByUserId).HasColumnName("CreatedByUserId");

            builder.Property(x => x.RowVersion)
                .HasColumnName("xmin")
                .HasColumnType("uint")
                .ValueGeneratedOnAddOrUpdate()
                .IsRowVersion();
        }
    }

    public class StockTransferItemHardeningConfiguration : IEntityTypeConfiguration<StockTransferItem>
    {
        public void Configure(EntityTypeBuilder<StockTransferItem> builder)
        {
            builder.Property(x => x.BatchId).HasColumnName("BatchId");
            builder.Property(x => x.OriginBinId).HasColumnName("OriginBinId");
            builder.Property(x => x.DestinationBinId).HasColumnName("DestinationBinId");
            builder.Property(x => x.RequestedQty).HasColumnName("RequestedQty").HasColumnType("numeric(18,4)");
            builder.Property(x => x.DispatchedQty).HasColumnName("DispatchedQty").HasColumnType("numeric(18,4)");
            builder.Property(x => x.ReceivedQty).HasColumnName("ReceivedQty").HasColumnType("numeric(18,4)");
            builder.Property(x => x.DamagedQty).HasColumnName("DamagedQty").HasColumnType("numeric(18,4)");
            builder.Property(x => x.VarianceReason).HasColumnName("VarianceReason").HasMaxLength(250);
        }
    }
}
