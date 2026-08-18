namespace ERP.Domain.Procurement
{
    public class GoodsReceiptItem
    {
        public int Id { get; set; }

        public int GoodsReceiptId { get; set; }

        public GoodsReceipt? GoodsReceipt { get; set; }

        public int PurchaseOrderLineId { get; set; }

        public PurchaseOrderLine? PurchaseOrderLine { get; set; }

        public string ItemName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Unit { get; set; } = "Nos";

        public decimal OrderedQuantity { get; set; }

        public decimal PreviouslyReceivedQuantity { get; set; }

        public decimal RemainingQuantity { get; set; }

        public decimal ReceivedQuantity { get; set; }

        public decimal RejectedQuantity { get; set; }

        public string Remarks { get; set; } = string.Empty;
    }
}
