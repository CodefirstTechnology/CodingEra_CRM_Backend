using Backend_ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend_ERP.Infrastructure.Data.Configurations
{
    public class StockAlertThresholdConfiguration : IEntityTypeConfiguration<StockAlertThresholdEntity>
    {
        public void Configure(EntityTypeBuilder<StockAlertThresholdEntity> builder)
        {
            builder.ToTable("stock_alert_thresholds");
            builder.HasKey(x => x.ThresholdId);

            builder.Property(x => x.ThresholdId).HasColumnName("threshold_id");
            builder.Property(x => x.ItemId).HasColumnName("item_id").IsRequired();
            builder.Property(x => x.WarehouseId).HasColumnName("warehouse_id").IsRequired();
            builder.Property(x => x.MinStockLevel).HasColumnName("min_stock_level").HasColumnType("numeric(18,4)");
            builder.Property(x => x.ReorderPoint).HasColumnName("reorder_point").HasColumnType("numeric(18,4)");
            builder.Property(x => x.MaxStockLevel).HasColumnName("max_stock_level").HasColumnType("numeric(18,4)");
            builder.Property(x => x.SafetyStock).HasColumnName("safety_stock").HasColumnType("numeric(18,4)");
            builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");

            builder.Property(x => x.RowVersion)
                .HasColumnName("xmin")
                .HasColumnType("uint")
                .ValueGeneratedOnAddOrUpdate()
                .IsRowVersion();
        }
    }

    public class StockAlertLogConfiguration : IEntityTypeConfiguration<StockAlertLogEntity>
    {
        public void Configure(EntityTypeBuilder<StockAlertLogEntity> builder)
        {
            builder.ToTable("stock_alert_logs");
            builder.HasKey(x => x.AlertId);

            builder.Property(x => x.AlertId).HasColumnName("alert_id");
            builder.Property(x => x.ThresholdId).HasColumnName("threshold_id").IsRequired();
            builder.Property(x => x.ItemId).HasColumnName("item_id").IsRequired();
            builder.Property(x => x.WarehouseId).HasColumnName("warehouse_id").IsRequired();
            builder.Property(x => x.AlertType).HasColumnName("alert_type").HasMaxLength(50).IsRequired();
            builder.Property(x => x.AlertPriority).HasColumnName("alert_priority").HasMaxLength(30).HasDefaultValue("Warning");
            builder.Property(x => x.CurrentQty).HasColumnName("current_qty").HasColumnType("numeric(18,4)");
            builder.Property(x => x.AvailableQty).HasColumnName("available_qty").HasColumnType("numeric(18,4)");
            builder.Property(x => x.OnOrderQty).HasColumnName("on_order_qty").HasColumnType("numeric(18,4)");
            builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("Active");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");

            builder.HasOne(x => x.Threshold)
                .WithMany()
                .HasForeignKey(x => x.ThresholdId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class ItemValuationLayerConfiguration : IEntityTypeConfiguration<ItemValuationLayerEntity>
    {
        public void Configure(EntityTypeBuilder<ItemValuationLayerEntity> builder)
        {
            builder.ToTable("item_valuation_layers");
            builder.HasKey(x => x.LayerId);

            builder.Property(x => x.LayerId).HasColumnName("layer_id");
            builder.Property(x => x.ItemId).HasColumnName("item_id").IsRequired();
            builder.Property(x => x.WarehouseId).HasColumnName("warehouse_id").IsRequired();
            builder.Property(x => x.ReceiptDate).HasColumnName("receipt_date").IsRequired();
            builder.Property(x => x.UnitCost).HasColumnName("unit_cost").HasColumnType("numeric(18,6)");
            builder.Property(x => x.InitialQty).HasColumnName("initial_qty").HasColumnType("numeric(18,4)");
            builder.Property(x => x.RemainingQty).HasColumnName("remaining_qty").HasColumnType("numeric(18,4)");
            builder.Property(x => x.IsDepleted).HasColumnName("is_depleted").HasDefaultValue(false);
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        }
    }

    public class ItemWacBalanceConfiguration : IEntityTypeConfiguration<ItemWacBalanceEntity>
    {
        public void Configure(EntityTypeBuilder<ItemWacBalanceEntity> builder)
        {
            builder.ToTable("item_wac_balances");
            builder.HasKey(x => x.BalanceId);

            builder.Property(x => x.BalanceId).HasColumnName("balance_id");
            builder.Property(x => x.ItemId).HasColumnName("item_id").IsRequired();
            builder.Property(x => x.WarehouseId).HasColumnName("warehouse_id").IsRequired();
            builder.Property(x => x.TotalQty).HasColumnName("total_qty").HasColumnType("numeric(18,4)");
            builder.Property(x => x.TotalValue).HasColumnName("total_value").HasColumnType("numeric(18,4)");
            builder.Property(x => x.WeightedAvgCost).HasColumnName("weighted_avg_cost").HasColumnType("numeric(18,6)");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.Property(x => x.RowVersion)
                .HasColumnName("xmin")
                .HasColumnType("uint")
                .ValueGeneratedOnAddOrUpdate()
                .IsRowVersion();
        }
    }

    public class PhysicalCountSheetConfiguration : IEntityTypeConfiguration<PhysicalCountSheetEntity>
    {
        public void Configure(EntityTypeBuilder<PhysicalCountSheetEntity> builder)
        {
            builder.ToTable("physical_count_sheets");
            builder.HasKey(x => x.SheetId);

            builder.Property(x => x.SheetId).HasColumnName("sheet_id");
            builder.Property(x => x.SheetNumber).HasColumnName("sheet_number").HasMaxLength(100).IsRequired();
            builder.Property(x => x.WarehouseId).HasColumnName("warehouse_id").IsRequired();
            builder.Property(x => x.CountDate).HasColumnName("count_date");
            builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("Draft");
            builder.Property(x => x.ConductedByUserId).HasColumnName("conducted_by_user_id").IsRequired();
            builder.Property(x => x.ReconciledByUserId).HasColumnName("reconciled_by_user_id");
            builder.Property(x => x.ReconciledAt).HasColumnName("reconciled_at");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");

            builder.Property(x => x.RowVersion)
                .HasColumnName("xmin")
                .HasColumnType("uint")
                .ValueGeneratedOnAddOrUpdate()
                .IsRowVersion();

            builder.HasMany(x => x.Lines)
                .WithOne(x => x.Sheet)
                .HasForeignKey(x => x.SheetId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class PhysicalCountLineConfiguration : IEntityTypeConfiguration<PhysicalCountLineEntity>
    {
        public void Configure(EntityTypeBuilder<PhysicalCountLineEntity> builder)
        {
            builder.ToTable("physical_count_lines");
            builder.HasKey(x => x.LineId);

            builder.Property(x => x.LineId).HasColumnName("line_id");
            builder.Property(x => x.SheetId).HasColumnName("sheet_id").IsRequired();
            builder.Property(x => x.ItemId).HasColumnName("item_id").IsRequired();
            builder.Property(x => x.BinId).HasColumnName("bin_id");
            builder.Property(x => x.BatchId).HasColumnName("batch_id");
            builder.Property(x => x.SystemQty).HasColumnName("system_qty").HasColumnType("numeric(18,4)");
            builder.Property(x => x.CountedQty).HasColumnName("counted_qty").HasColumnType("numeric(18,4)");
            builder.Property(x => x.VarianceQty).HasColumnName("variance_qty").HasColumnType("numeric(18,4)");
            builder.Property(x => x.VarianceReason).HasColumnName("variance_reason").HasMaxLength(250);
        }
    }
}
