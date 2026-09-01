namespace ERP.Application.Procurement.Dtos
{
    public class ErpItemLookupDto
    {
        public int Id { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal AvailableQuantity { get; set; }
        public string ItemType { get; set; } = "FinishedGood"; // "RawMaterial" or "FinishedGood"
    }
}
