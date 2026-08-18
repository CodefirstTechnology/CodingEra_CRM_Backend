using System;

namespace ERP.Domain.Procurement
{
    public class InventoryBatch
    {
        public int Id { get; set; }

        public string BatchNumber { get; set; } = string.Empty;

        public int MaterialId { get; set; }

        public string MaterialCode { get; set; } = string.Empty;

        public string MaterialName { get; set; } = string.Empty;

        public int WarehouseId { get; set; }

        public string WarehouseName { get; set; } = string.Empty;

        public string Supplier { get; set; } = string.Empty;

        public int? GRNId { get; set; }

        public string? GRNNumber { get; set; }

        public DateTime ManufacturingDate { get; set; } = DateTime.UtcNow;

        public DateTime? ExpiryDate { get; set; }

        public decimal AvailableQuantity { get; set; }

        public decimal ConsumedQuantity { get; set; }

        public decimal RemainingQuantity { get; set; }

        public string Unit { get; set; } = "Nos";

        public decimal UnitCost { get; set; }

        public BatchStatus Status { get; set; } = BatchStatus.Active;

        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }
    }
}
