namespace ERP.Domain.Sales
{
    public class PriceListItem
    {
        public int Id { get; set; }

        public int PriceListId { get; set; }

        public PriceList PriceList { get; set; } = null!;

        public string ItemCode { get; set; } = string.Empty;

        public string ItemName { get; set; } = string.Empty;

        public string ItemCategory { get; set; } = string.Empty;

        public string Unit { get; set; } = "Nos";

        public decimal BasePrice { get; set; }

        public decimal SellingPrice { get; set; }

        public decimal DiscountPercentage { get; set; }

        public decimal MinimumPrice { get; set; }

        public decimal MaximumDiscount { get; set; }

        public decimal TaxPercentage { get; set; }

        public string Remarks { get; set; } = string.Empty;

        public int SortOrder { get; set; }
    }
}
