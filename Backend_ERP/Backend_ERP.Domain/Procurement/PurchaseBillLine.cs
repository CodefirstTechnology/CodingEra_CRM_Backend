namespace ERP.Domain.Procurement
{
    public class PurchaseBillLine
    {
        public int Id { get; set; }

        public int PurchaseBillId { get; set; }

        public PurchaseBill? PurchaseBill { get; set; }

        public string ItemName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal TaxPercent { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal TotalAmount { get; set; }
    }
}
