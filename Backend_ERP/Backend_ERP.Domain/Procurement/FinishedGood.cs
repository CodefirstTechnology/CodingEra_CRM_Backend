using System;

namespace ERP.Domain.Procurement
{
    public class FinishedGood
    {
        public int Id { get; set; }

        public string ProductCode { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public decimal FinishedQuantity { get; set; }

        public decimal ReservedQuantity { get; set; }

        public decimal AvailableQuantity { get; set; }

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

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }
    }
}
