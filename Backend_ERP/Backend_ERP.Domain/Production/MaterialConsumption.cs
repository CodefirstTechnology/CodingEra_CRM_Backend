using System;
using ERP.Domain.Procurement;

namespace ERP.Domain.Production
{
    public class MaterialConsumption
    {
        public int Id { get; set; }

        public string ConsumptionNumber { get; set; } = string.Empty;

        public int WorkOrderId { get; set; }

        public WorkOrder? WorkOrder { get; set; }

        public string WorkOrderNumber { get; set; } = string.Empty;

        public int BomId { get; set; }

        public BillOfMaterials? Bom { get; set; }

        public string BomNumber { get; set; } = string.Empty;

        public int? EntryId { get; set; }

        public ProductionEntry? Entry { get; set; }

        public int MaterialId { get; set; }

        public RawMaterial? Material { get; set; }

        public string MaterialCode { get; set; } = string.Empty;

        public string MaterialName { get; set; } = string.Empty;

        public decimal PlannedQuantity { get; set; }

        public decimal ActualQuantity { get; set; }

        public decimal Variance { get; set; }

        public string Uom { get; set; } = "Nos";

        public int WarehouseId { get; set; }

        public Warehouse? Warehouse { get; set; }

        public string WarehouseName { get; set; } = string.Empty;

        public string BatchNumber { get; set; } = string.Empty;

        public string StockOutReference { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }
    }
}
