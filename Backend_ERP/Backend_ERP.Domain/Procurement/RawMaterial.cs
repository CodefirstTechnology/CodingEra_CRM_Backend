using System;

namespace ERP.Domain.Procurement
{
    public class RawMaterial
    {
        public int Id { get; set; }

        public string MaterialCode { get; set; } = string.Empty;

        public string MaterialName { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public int WarehouseId { get; set; }

        public Warehouse? Warehouse { get; set; }

        public string WarehouseName { get; set; } = string.Empty;

        public string Rack { get; set; } = string.Empty;

        public string Unit { get; set; } = "Nos";

        public decimal OpeningStock { get; set; }

        public decimal AvailableStock { get; set; }

        public decimal ReservedStock { get; set; }

        public decimal MinimumStock { get; set; }

        public decimal MaximumStock { get; set; }

        public decimal ReorderLevel { get; set; }

        public decimal UnitCost { get; set; }

        public decimal CurrentValue { get; set; }

        public int BatchCount { get; set; }

        public int StockAgeDays { get; set; }

        public StockAgeBand StockAgeBand { get; set; } = StockAgeBand.Band0To30;

        public string Supplier { get; set; } = string.Empty;

        public DateTime? LastReceiptDate { get; set; }

        public int? LinkedGRNId { get; set; }

        public string? LinkedGRNNumber { get; set; }

        public int? LinkedPOId { get; set; }

        public string? LinkedPONumber { get; set; }

        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }
    }
}
