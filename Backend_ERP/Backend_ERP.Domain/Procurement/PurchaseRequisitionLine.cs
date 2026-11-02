namespace ERP.Domain.Procurement
{
    public class PurchaseRequisitionLine
    {
        public int Id { get; set; }

        public int TenantId { get; set; } = 1;

        public int PurchaseRequisitionId { get; set; }

        public PurchaseRequisition? PurchaseRequisition { get; set; }

        public string ItemType { get; set; } = "Inventory";

        public int? ItemId { get; set; }

        public string ItemCode { get; set; } = string.Empty;

        public int? UomId { get; set; }

        public string ItemName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public decimal SourcedQuantity { get; set; } = 0m;

        public decimal OrderedQuantity { get; set; } = 0m;

        public string Uom { get; set; } = "PCS";

        public decimal EstimatedPrice { get; set; }

        public decimal TotalAmount { get; set; }

        public int? TargetWarehouseId { get; set; }

        public int? WorkOrderId { get; set; }

        public bool IsAvlRestricted { get; set; } = false;

        public PurchaseRequisitionLineStatus LineStatus { get; set; } = PurchaseRequisitionLineStatus.PendingSourcing;
    }
}
