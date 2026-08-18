using ERP.Domain.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
    {
        public void Configure(EntityTypeBuilder<Warehouse> builder)
        {
            builder.ToTable("warehouses");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.Code).IsUnique();

            builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Location).HasMaxLength(256);
            builder.Property(x => x.Address).HasMaxLength(500);
            builder.Property(x => x.Manager).HasMaxLength(256);

            builder.Property(x => x.Capacity).HasPrecision(18, 4);
            builder.Property(x => x.UsedCapacity).HasPrecision(18, 4);
            builder.Property(x => x.AvailableCapacity).HasPrecision(18, 4);

            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            builder.HasIndex(x => x.Status);
        }
    }

    public class RawMaterialConfiguration : IEntityTypeConfiguration<RawMaterial>
    {
        public void Configure(EntityTypeBuilder<RawMaterial> builder)
        {
            builder.ToTable("raw_materials");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.MaterialCode).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.MaterialCode).IsUnique();

            builder.Property(x => x.MaterialName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Category).HasMaxLength(128);
            builder.Property(x => x.WarehouseName).HasMaxLength(256);
            builder.Property(x => x.Rack).HasMaxLength(128);
            builder.Property(x => x.Unit).HasMaxLength(32);

            builder.Property(x => x.OpeningStock).HasPrecision(18, 4);
            builder.Property(x => x.AvailableStock).HasPrecision(18, 4);
            builder.Property(x => x.ReservedStock).HasPrecision(18, 4);
            builder.Property(x => x.MinimumStock).HasPrecision(18, 4);
            builder.Property(x => x.MaximumStock).HasPrecision(18, 4);
            builder.Property(x => x.ReorderLevel).HasPrecision(18, 4);
            builder.Property(x => x.UnitCost).HasPrecision(18, 4);
            builder.Property(x => x.CurrentValue).HasPrecision(18, 4);

            builder.Property(x => x.StockAgeBand).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.Supplier).HasMaxLength(256);
            builder.Property(x => x.LinkedGRNNumber).HasMaxLength(64);
            builder.Property(x => x.LinkedPONumber).HasMaxLength(64);
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            builder.HasIndex(x => x.WarehouseId);
            builder.HasIndex(x => x.Category);
        }
    }

    public class FinishedGoodConfiguration : IEntityTypeConfiguration<FinishedGood>
    {
        public void Configure(EntityTypeBuilder<FinishedGood> builder)
        {
            builder.ToTable("finished_goods");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ProductCode).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.ProductCode).IsUnique();

            builder.Property(x => x.ProductName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.WarehouseName).HasMaxLength(256);
            builder.Property(x => x.BatchNumber).HasMaxLength(128);
            builder.Property(x => x.ProductionReference).HasMaxLength(128);
            builder.Property(x => x.Unit).HasMaxLength(32);

            builder.Property(x => x.FinishedQuantity).HasPrecision(18, 4);
            builder.Property(x => x.ReservedQuantity).HasPrecision(18, 4);
            builder.Property(x => x.AvailableQuantity).HasPrecision(18, 4);
            builder.Property(x => x.UnitCost).HasPrecision(18, 4);
            builder.Property(x => x.CurrentValue).HasPrecision(18, 4);

            builder.Property(x => x.DispatchStatus).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            builder.HasIndex(x => x.WarehouseId);
        }
    }

    public class StockTransactionConfiguration : IEntityTypeConfiguration<StockTransaction>
    {
        public void Configure(EntityTypeBuilder<StockTransaction> builder)
        {
            builder.ToTable("stock_transactions");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TransactionNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.TransactionNumber).IsUnique();

            builder.Property(x => x.MaterialCode).HasMaxLength(64);
            builder.Property(x => x.MaterialName).HasMaxLength(256);
            builder.Property(x => x.WarehouseName).HasMaxLength(256);
            builder.Property(x => x.Unit).HasMaxLength(32);
            builder.Property(x => x.Quantity).HasPrecision(18, 4);

            builder.Property(x => x.TransactionType).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.ReferenceType).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.ReferenceNumber).HasMaxLength(64);
            builder.Property(x => x.Reason).HasMaxLength(256);
            builder.Property(x => x.User).HasMaxLength(256);
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.CreatedBy).HasMaxLength(256);

            builder.HasIndex(x => x.TransactionType);
            builder.HasIndex(x => x.WarehouseId);
            builder.HasIndex(x => x.MaterialId);
        }
    }

    public class InventoryBatchConfiguration : IEntityTypeConfiguration<InventoryBatch>
    {
        public void Configure(EntityTypeBuilder<InventoryBatch> builder)
        {
            builder.ToTable("inventory_batches");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.BatchNumber).HasMaxLength(128).IsRequired();
            builder.HasIndex(x => x.BatchNumber);

            builder.Property(x => x.MaterialCode).HasMaxLength(64);
            builder.Property(x => x.MaterialName).HasMaxLength(256);
            builder.Property(x => x.WarehouseName).HasMaxLength(256);
            builder.Property(x => x.Supplier).HasMaxLength(256);
            builder.Property(x => x.GRNNumber).HasMaxLength(64);
            builder.Property(x => x.Unit).HasMaxLength(32);

            builder.Property(x => x.AvailableQuantity).HasPrecision(18, 4);
            builder.Property(x => x.ConsumedQuantity).HasPrecision(18, 4);
            builder.Property(x => x.RemainingQuantity).HasPrecision(18, 4);
            builder.Property(x => x.UnitCost).HasPrecision(18, 4);

            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.MaterialId);
        }
    }

    public class StockTransferConfiguration : IEntityTypeConfiguration<StockTransfer>
    {
        public void Configure(EntityTypeBuilder<StockTransfer> builder)
        {
            builder.ToTable("stock_transfers");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TransferNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.TransferNumber).IsUnique();

            builder.Property(x => x.FromWarehouseName).HasMaxLength(256);
            builder.Property(x => x.ToWarehouseName).HasMaxLength(256);
            builder.Property(x => x.TotalQuantity).HasPrecision(18, 4);

            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.RequestedBy).HasMaxLength(256);
            builder.Property(x => x.ApprovedBy).HasMaxLength(256);
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            builder.HasIndex(x => x.Status);

            builder.HasMany(x => x.Items)
                .WithOne(x => x.StockTransfer)
                .HasForeignKey(x => x.StockTransferId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class StockTransferItemConfiguration : IEntityTypeConfiguration<StockTransferItem>
    {
        public void Configure(EntityTypeBuilder<StockTransferItem> builder)
        {
            builder.ToTable("stock_transfer_items");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.MaterialCode).HasMaxLength(64);
            builder.Property(x => x.MaterialName).HasMaxLength(256);
            builder.Property(x => x.Unit).HasMaxLength(32);
            builder.Property(x => x.Quantity).HasPrecision(18, 4);
            builder.Property(x => x.BatchNumber).HasMaxLength(128);

            builder.HasIndex(x => x.StockTransferId);
        }
    }

    public class StockAlertConfiguration : IEntityTypeConfiguration<StockAlert>
    {
        public void Configure(EntityTypeBuilder<StockAlert> builder)
        {
            builder.ToTable("stock_alerts");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.MaterialCode).HasMaxLength(64);
            builder.Property(x => x.MaterialName).HasMaxLength(256);
            builder.Property(x => x.WarehouseName).HasMaxLength(256);
            builder.Property(x => x.Unit).HasMaxLength(32);

            builder.Property(x => x.CurrentStock).HasPrecision(18, 4);
            builder.Property(x => x.MinimumStock).HasPrecision(18, 4);
            builder.Property(x => x.ReorderQuantity).HasPrecision(18, 4);

            builder.Property(x => x.Priority).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.LinkedRequisitionNumber).HasMaxLength(64);

            builder.HasIndex(x => x.Priority);
            builder.HasIndex(x => x.MaterialId);
        }
    }

    public class PhysicalVerificationConfiguration : IEntityTypeConfiguration<PhysicalVerification>
    {
        public void Configure(EntityTypeBuilder<PhysicalVerification> builder)
        {
            builder.ToTable("physical_verifications");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.VerificationNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.VerificationNumber).IsUnique();

            builder.Property(x => x.WarehouseName).HasMaxLength(256);
            builder.Property(x => x.Verifier).HasMaxLength(256);

            builder.Property(x => x.ExpectedQuantity).HasPrecision(18, 4);
            builder.Property(x => x.ActualQuantity).HasPrecision(18, 4);
            builder.Property(x => x.Variance).HasPrecision(18, 4);
            builder.Property(x => x.VarianceValue).HasPrecision(18, 4);

            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.ApprovedBy).HasMaxLength(256);
            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            builder.HasIndex(x => x.Status);

            builder.HasMany(x => x.Lines)
                .WithOne(x => x.PhysicalVerification)
                .HasForeignKey(x => x.PhysicalVerificationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class PhysicalVerificationLineConfiguration : IEntityTypeConfiguration<PhysicalVerificationLine>
    {
        public void Configure(EntityTypeBuilder<PhysicalVerificationLine> builder)
        {
            builder.ToTable("physical_verification_lines");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.MaterialCode).HasMaxLength(64);
            builder.Property(x => x.MaterialName).HasMaxLength(256);
            builder.Property(x => x.Unit).HasMaxLength(32);

            builder.Property(x => x.ExpectedQuantity).HasPrecision(18, 4);
            builder.Property(x => x.ActualQuantity).HasPrecision(18, 4);
            builder.Property(x => x.Variance).HasPrecision(18, 4);
            builder.Property(x => x.Remarks).HasMaxLength(2000);

            builder.HasIndex(x => x.PhysicalVerificationId);
        }
    }

    public class StoreInventoryDocumentSequenceConfiguration : IEntityTypeConfiguration<StoreInventoryDocumentSequence>
    {
        public void Configure(EntityTypeBuilder<StoreInventoryDocumentSequence> builder)
        {
            builder.ToTable("store_inventory_document_sequences");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.Prefix).IsUnique();
        }
    }
}
