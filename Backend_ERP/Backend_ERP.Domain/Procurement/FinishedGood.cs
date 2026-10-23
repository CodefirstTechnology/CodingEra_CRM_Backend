using System;

namespace ERP.Domain.Procurement
{
    public class FinishedGood
    {
        public int Id { get; set; }

        public string TenantId { get; set; } = "default";

        public string ProductCode { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// Total physical stock on hand in the warehouse.
        /// </summary>
        public decimal FinishedQuantity { get; set; }

        /// <summary>
        /// Soft-reserved stock committed to active Sales Orders awaiting dispatch.
        /// </summary>
        public decimal ReservedQuantity { get; set; }

        /// <summary>
        /// Available-to-Promise (ATP) stock: FinishedQuantity - ReservedQuantity.
        /// </summary>
        public decimal AvailableQuantity { get; set; }

        /// <summary>
        /// Minimum threshold before reorder triggers.
        /// </summary>
        public decimal ReorderLevel { get; set; }

        public int WarehouseId { get; set; }

        public Warehouse? Warehouse { get; set; }

        public string WarehouseName { get; set; } = string.Empty;

        public string BatchNumber { get; set; } = string.Empty;

        public string ProductionReference { get; set; } = string.Empty;

        public DateTime ManufacturingDate { get; set; } = DateTime.UtcNow;

        public DateTime? ExpiryDate { get; set; }

        public decimal UnitCost { get; set; }

        public decimal CurrentValue { get; set; }

        public FgDispatchStatus DispatchStatus { get; set; } = FgDispatchStatus.NotReady;

        public string Unit { get; set; } = "Nos";

        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// Optimistic concurrency token mapped to PostgreSQL xmin.
        /// </summary>
        public uint Version { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }

        public void RecalculateAvailable()
        {
            AvailableQuantity = Math.Max(0m, FinishedQuantity - ReservedQuantity);
        }
    }
}
