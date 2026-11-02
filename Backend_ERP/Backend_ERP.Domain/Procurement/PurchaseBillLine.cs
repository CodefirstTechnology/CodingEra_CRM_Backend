namespace ERP.Domain.Procurement
{
    public class PurchaseBillLine
    {
        public int Id { get; set; }

        public int PurchaseBillId { get; set; }

        public PurchaseBill? PurchaseBill { get; set; }

        public int? PurchaseOrderLineId { get; set; }

        public PurchaseOrderLine? PurchaseOrderLine { get; set; }

        public int? GoodsReceiptId { get; set; }

        public GoodsReceipt? GoodsReceipt { get; set; }

        public int? GoodsReceiptItemId { get; set; }

        public GoodsReceiptItem? GoodsReceiptItem { get; set; }

        public int? ItemId { get; set; }

        public string ItemName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal TaxPercent { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal TotalAmount { get; set; }

        // 3-Way Match Line Discrepancies
        public decimal PoRate { get; set; }

        public decimal GrnAcceptedQuantity { get; set; }

        public decimal RateVariance { get; set; }

        public decimal QuantityVariance { get; set; }

        public decimal TaxVariance { get; set; }

        public ThreeWayMatchStatus LineMatchStatus { get; set; } = ThreeWayMatchStatus.Pending;

        public string? MatchRemarks { get; set; }
    }
}
