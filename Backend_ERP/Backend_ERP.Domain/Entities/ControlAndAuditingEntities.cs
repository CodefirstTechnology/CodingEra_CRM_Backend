using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Backend_ERP.Domain.Entities
{
    public class StockAlertThresholdEntity
    {
        [Key]
        public Guid ThresholdId { get; set; } = Guid.NewGuid();
        public int ItemId { get; set; }
        public Guid WarehouseId { get; set; }
        public decimal MinStockLevel { get; set; }
        public decimal ReorderPoint { get; set; }
        public decimal MaxStockLevel { get; set; }
        public decimal SafetyStock { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // PostgreSQL xmin concurrency token
        public uint RowVersion { get; set; }
    }

    public class StockAlertLogEntity
    {
        [Key]
        public Guid AlertId { get; set; } = Guid.NewGuid();
        public Guid ThresholdId { get; set; }
        public int ItemId { get; set; }
        public Guid WarehouseId { get; set; }
        public string AlertType { get; set; } = null!; // MinStock, ReorderPoint, MaxStockOverflow
        public string AlertPriority { get; set; } = "Warning"; // Critical, Warning, Normal
        public decimal CurrentQty { get; set; }
        public decimal AvailableQty { get; set; }
        public decimal OnOrderQty { get; set; }
        public string Status { get; set; } = "Active"; // Active, Acknowledged, Resolved
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public StockAlertThresholdEntity Threshold { get; set; } = null!;
    }

    public class ItemValuationLayerEntity
    {
        [Key]
        public Guid LayerId { get; set; } = Guid.NewGuid();
        public int ItemId { get; set; }
        public Guid WarehouseId { get; set; }
        public DateTime ReceiptDate { get; set; } = DateTime.UtcNow;
        public decimal UnitCost { get; set; }
        public decimal InitialQty { get; set; }
        public decimal RemainingQty { get; set; }
        public bool IsDepleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ItemWacBalanceEntity
    {
        [Key]
        public Guid BalanceId { get; set; } = Guid.NewGuid();
        public int ItemId { get; set; }
        public Guid WarehouseId { get; set; }
        public decimal TotalQty { get; set; }
        public decimal TotalValue { get; set; }
        public decimal WeightedAvgCost { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // PostgreSQL xmin concurrency token
        public uint RowVersion { get; set; }
    }

    public class PhysicalCountSheetEntity
    {
        [Key]
        public Guid SheetId { get; set; } = Guid.NewGuid();
        public string SheetNumber { get; set; } = null!;
        public Guid WarehouseId { get; set; }
        public DateTime CountDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Draft"; // Draft, Submitted, Reconciled, Cancelled
        public int ConductedByUserId { get; set; }
        public int? ReconciledByUserId { get; set; }
        public DateTime? ReconciledAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // PostgreSQL xmin concurrency token
        public uint RowVersion { get; set; }

        public ICollection<PhysicalCountLineEntity> Lines { get; set; } = new List<PhysicalCountLineEntity>();
    }

    public class PhysicalCountLineEntity
    {
        [Key]
        public Guid LineId { get; set; } = Guid.NewGuid();
        public Guid SheetId { get; set; }
        public int ItemId { get; set; }
        public Guid? BinId { get; set; }
        public Guid? BatchId { get; set; }
        public decimal SystemQty { get; set; }
        public decimal CountedQty { get; set; }
        public decimal VarianceQty { get; set; }
        public string? VarianceReason { get; set; }

        public PhysicalCountSheetEntity Sheet { get; set; } = null!;
    }
}
