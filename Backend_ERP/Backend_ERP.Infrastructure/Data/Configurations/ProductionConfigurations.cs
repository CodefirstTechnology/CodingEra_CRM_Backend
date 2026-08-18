using ERP.Domain.Production;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class BillOfMaterialsConfiguration : IEntityTypeConfiguration<BillOfMaterials>
    {
        public void Configure(EntityTypeBuilder<BillOfMaterials> builder)
        {
            builder.ToTable("production_boms");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.BomNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.BomNumber).IsUnique();

            builder.Property(x => x.Version).HasMaxLength(16).IsRequired();
            builder.Property(x => x.ProductCode).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ProductName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Revision).HasMaxLength(16).IsRequired();
            
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            
            builder.Property(x => x.ProcessNotes).HasMaxLength(2000);
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.AttachmentName).HasMaxLength(512);
            builder.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            builder.Property(x => x.UpdatedBy).HasMaxLength(256).IsRequired();

            builder.HasIndex(x => x.ProductId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.IsDeleted);

            builder.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Materials)
                .WithOne(x => x.Bom)
                .HasForeignKey(x => x.BomId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class BomMaterialLineConfiguration : IEntityTypeConfiguration<BomMaterialLine>
    {
        public void Configure(EntityTypeBuilder<BomMaterialLine> builder)
        {
            builder.ToTable("production_bom_materials");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.MaterialCode).HasMaxLength(64).IsRequired();
            builder.Property(x => x.MaterialName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Quantity).HasPrecision(18, 4).IsRequired();
            builder.Property(x => x.Uom).HasMaxLength(32).IsRequired();
            builder.Property(x => x.WastagePercent).HasPrecision(18, 4).IsRequired();
            builder.Property(x => x.WarehouseName).HasMaxLength(256).IsRequired();

            builder.HasIndex(x => x.BomId);
            builder.HasIndex(x => x.MaterialId);
            builder.HasIndex(x => x.WarehouseId);

            builder.HasOne(x => x.Material)
                .WithMany()
                .HasForeignKey(x => x.MaterialId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Warehouse)
                .WithMany()
                .HasForeignKey(x => x.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class ProductionPlanConfiguration : IEntityTypeConfiguration<ProductionPlan>
    {
        public void Configure(EntityTypeBuilder<ProductionPlan> builder)
        {
            builder.ToTable("production_plans");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.PlanNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.PlanNumber).IsUnique();

            builder.Property(x => x.PlanningPeriod).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ProductCode).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ProductName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.RequiredQuantity).HasPrecision(18, 4).IsRequired();
            builder.Property(x => x.PlannedQuantity).HasPrecision(18, 4).IsRequired();
            builder.Property(x => x.BomNumber).HasMaxLength(64).IsRequired();
            builder.Property(x => x.WarehouseName).HasMaxLength(256).IsRequired();
            
            builder.Property(x => x.Priority).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            
            builder.Property(x => x.Planner).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.AttachmentName).HasMaxLength(512);
            builder.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            builder.Property(x => x.UpdatedBy).HasMaxLength(256).IsRequired();

            builder.HasIndex(x => x.ProductId);
            builder.HasIndex(x => x.BomId);
            builder.HasIndex(x => x.WarehouseId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.IsDeleted);

            builder.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Bom)
                .WithMany()
                .HasForeignKey(x => x.BomId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Warehouse)
                .WithMany()
                .HasForeignKey(x => x.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
    {
        public void Configure(EntityTypeBuilder<WorkOrder> builder)
        {
            builder.ToTable("production_work_orders");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.WorkOrderNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.WorkOrderNumber).IsUnique();

            builder.Property(x => x.PlanNumber).HasMaxLength(64).IsRequired();
            builder.Property(x => x.BomNumber).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ProductCode).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ProductName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.PlannedQuantity).HasPrecision(18, 4).IsRequired();
            builder.Property(x => x.ProducedQuantity).HasPrecision(18, 4).IsRequired();
            builder.Property(x => x.PendingQuantity).HasPrecision(18, 4).IsRequired();
            
            builder.Property(x => x.Priority).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            
            builder.Property(x => x.Supervisor).HasMaxLength(256).IsRequired();
            builder.Property(x => x.MachineCode).HasMaxLength(64);
            builder.Property(x => x.MachineName).HasMaxLength(256);
            builder.Property(x => x.AssignedTeam).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.AttachmentName).HasMaxLength(512);
            builder.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            builder.Property(x => x.UpdatedBy).HasMaxLength(256).IsRequired();

            builder.HasIndex(x => x.PlanId);
            builder.HasIndex(x => x.BomId);
            builder.HasIndex(x => x.ProductId);
            builder.HasIndex(x => x.MachineId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.IsDeleted);

            builder.HasOne(x => x.Plan)
                .WithMany()
                .HasForeignKey(x => x.PlanId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Bom)
                .WithMany()
                .HasForeignKey(x => x.BomId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Machine)
                .WithMany()
                .HasForeignKey(x => x.MachineId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

    public class ProductionScheduleConfiguration : IEntityTypeConfiguration<ProductionSchedule>
    {
        public void Configure(EntityTypeBuilder<ProductionSchedule> builder)
        {
            builder.ToTable("production_schedules");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ScheduleNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.ScheduleNumber).IsUnique();

            builder.Property(x => x.WorkOrderNumber).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ProductName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.MachineCode).HasMaxLength(64).IsRequired();
            builder.Property(x => x.MachineName).HasMaxLength(256).IsRequired();
            
            builder.Property(x => x.Shift).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            
            builder.Property(x => x.Operator).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Capacity).HasPrecision(18, 4).IsRequired();
            builder.Property(x => x.UtilizationPercent).HasPrecision(18, 4).IsRequired();
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            builder.Property(x => x.UpdatedBy).HasMaxLength(256).IsRequired();

            builder.HasIndex(x => x.WorkOrderId);
            builder.HasIndex(x => x.MachineId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.IsDeleted);

            builder.HasOne(x => x.WorkOrder)
                .WithMany()
                .HasForeignKey(x => x.WorkOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Machine)
                .WithMany()
                .HasForeignKey(x => x.MachineId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class MachineConfiguration : IEntityTypeConfiguration<Machine>
    {
        public void Configure(EntityTypeBuilder<Machine> builder)
        {
            builder.ToTable("production_machines");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.MachineCode).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.MachineCode).IsUnique();

            builder.Property(x => x.MachineName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Department).HasMaxLength(128).IsRequired();
            builder.Property(x => x.RunningHours).HasPrecision(18, 4).IsRequired();
            builder.Property(x => x.IdleHours).HasPrecision(18, 4).IsRequired();
            builder.Property(x => x.BreakdownHours).HasPrecision(18, 4).IsRequired();
            builder.Property(x => x.UtilizationPercent).HasPrecision(18, 4).IsRequired();
            
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            builder.Property(x => x.UpdatedBy).HasMaxLength(256).IsRequired();

            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.IsDeleted);
        }
    }

    public class ProductionEntryConfiguration : IEntityTypeConfiguration<ProductionEntry>
    {
        public void Configure(EntityTypeBuilder<ProductionEntry> builder)
        {
            builder.ToTable("production_entries");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.EntryNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.EntryNumber).IsUnique();

            builder.Property(x => x.WorkOrderNumber).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ProductCode).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ProductName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.ProducedQuantity).HasPrecision(18, 4).IsRequired();
            builder.Property(x => x.GoodQuantity).HasPrecision(18, 4).IsRequired();
            builder.Property(x => x.RejectedQuantity).HasPrecision(18, 4).IsRequired();
            
            builder.Property(x => x.Shift).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            
            builder.Property(x => x.Operator).HasMaxLength(256).IsRequired();
            builder.Property(x => x.MachineCode).HasMaxLength(64).IsRequired();
            builder.Property(x => x.MachineName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.AttachmentName).HasMaxLength(512);
            builder.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            builder.Property(x => x.UpdatedBy).HasMaxLength(256).IsRequired();

            builder.HasIndex(x => x.WorkOrderId);
            builder.HasIndex(x => x.ProductId);
            builder.HasIndex(x => x.MachineId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.IsDeleted);

            builder.HasOne(x => x.WorkOrder)
                .WithMany()
                .HasForeignKey(x => x.WorkOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Machine)
                .WithMany()
                .HasForeignKey(x => x.MachineId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class MaterialConsumptionConfiguration : IEntityTypeConfiguration<MaterialConsumption>
    {
        public void Configure(EntityTypeBuilder<MaterialConsumption> builder)
        {
            builder.ToTable("production_consumptions");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ConsumptionNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.ConsumptionNumber).IsUnique();

            builder.Property(x => x.WorkOrderNumber).HasMaxLength(64).IsRequired();
            builder.Property(x => x.BomNumber).HasMaxLength(64).IsRequired();
            builder.Property(x => x.MaterialCode).HasMaxLength(64).IsRequired();
            builder.Property(x => x.MaterialName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.PlannedQuantity).HasPrecision(18, 4).IsRequired();
            builder.Property(x => x.ActualQuantity).HasPrecision(18, 4).IsRequired();
            builder.Property(x => x.Variance).HasPrecision(18, 4).IsRequired();
            builder.Property(x => x.Uom).HasMaxLength(32).IsRequired();
            builder.Property(x => x.WarehouseName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.BatchNumber).HasMaxLength(128).IsRequired();
            builder.Property(x => x.StockOutReference).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            builder.Property(x => x.UpdatedBy).HasMaxLength(256).IsRequired();

            builder.HasIndex(x => x.WorkOrderId);
            builder.HasIndex(x => x.BomId);
            builder.HasIndex(x => x.EntryId);
            builder.HasIndex(x => x.MaterialId);
            builder.HasIndex(x => x.WarehouseId);
            builder.HasIndex(x => x.IsDeleted);

            builder.HasOne(x => x.WorkOrder)
                .WithMany()
                .HasForeignKey(x => x.WorkOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Bom)
                .WithMany()
                .HasForeignKey(x => x.BomId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Entry)
                .WithMany()
                .HasForeignKey(x => x.EntryId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.Material)
                .WithMany()
                .HasForeignKey(x => x.MaterialId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Warehouse)
                .WithMany()
                .HasForeignKey(x => x.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class RejectionRecordConfiguration : IEntityTypeConfiguration<RejectionRecord>
    {
        public void Configure(EntityTypeBuilder<RejectionRecord> builder)
        {
            builder.ToTable("production_rejections");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.RejectionNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.RejectionNumber).IsUnique();

            builder.Property(x => x.WorkOrderNumber).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ProductCode).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ProductName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Quantity).HasPrecision(18, 4).IsRequired();
            builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            builder.Property(x => x.Category).HasMaxLength(128).IsRequired();
            builder.Property(x => x.Operator).HasMaxLength(256).IsRequired();
            builder.Property(x => x.MachineCode).HasMaxLength(64).IsRequired();
            builder.Property(x => x.MachineName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.CorrectiveAction).HasMaxLength(2000);
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            builder.Property(x => x.UpdatedBy).HasMaxLength(256).IsRequired();

            builder.HasIndex(x => x.WorkOrderId);
            builder.HasIndex(x => x.EntryId);
            builder.HasIndex(x => x.ProductId);
            builder.HasIndex(x => x.MachineId);
            builder.HasIndex(x => x.IsDeleted);

            builder.HasOne(x => x.WorkOrder)
                .WithMany()
                .HasForeignKey(x => x.WorkOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Entry)
                .WithMany()
                .HasForeignKey(x => x.EntryId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Machine)
                .WithMany()
                .HasForeignKey(x => x.MachineId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class ProductionDocumentSequenceConfiguration : IEntityTypeConfiguration<ProductionDocumentSequence>
    {
        public void Configure(EntityTypeBuilder<ProductionDocumentSequence> builder)
        {
            builder.ToTable("production_document_sequences");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Prefix).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.Prefix).IsUnique();
        }
    }
}
