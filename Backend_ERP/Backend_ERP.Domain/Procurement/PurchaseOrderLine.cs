namespace ERP.Domain.Procurement
{
    public class PurchaseOrderLine
    {
        public int Id { get; set; }

        public int PurchaseOrderId { get; set; }

        public PurchaseOrder? PurchaseOrder { get; set; }

        public string ItemName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public string Unit { get; set; } = "Nos";

        public decimal Rate { get; set; }

        public decimal Discount { get; set; }

        public decimal Tax { get; set; }

        public decimal Amount { get; set; }
    }
}
