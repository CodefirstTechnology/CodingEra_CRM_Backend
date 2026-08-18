using ERP.Domain.Procurement;

namespace ERP.Domain.Production
{
    public class BomMaterialLine
    {
        public int Id { get; set; }

        public int BomId { get; set; }

        public BillOfMaterials? Bom { get; set; }

        public int MaterialId { get; set; }

        public RawMaterial? Material { get; set; }

        public string MaterialCode { get; set; } = string.Empty;

        public string MaterialName { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public string Uom { get; set; } = "Nos";

        public decimal WastagePercent { get; set; }

        public int WarehouseId { get; set; }

        public Warehouse? Warehouse { get; set; }

        public string WarehouseName { get; set; } = string.Empty;
    }
}
