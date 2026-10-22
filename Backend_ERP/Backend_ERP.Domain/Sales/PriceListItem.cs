using System.Collections.Generic;

namespace ERP.Domain.Sales
{
    public class PriceListItem
    {
        public int Id { get; set; }

        public string TenantId { get; set; } = "default";

        public int PriceListId { get; set; }

        public PriceList PriceList { get; set; } = null!;

        public PricingItemType ItemType { get; set; } = PricingItemType.FinishedGood;

        public int ItemId { get; set; }

        public string ItemCode { get; set; } = string.Empty;

        public string ItemName { get; set; } = string.Empty;

        public string ItemCategory { get; set; } = string.Empty;

        public string Unit { get; set; } = "Nos";

        // UOM Conversion
        public string BaseUom { get; set; } = "Nos";

        public string PricingUom { get; set; } = "Nos";

        public decimal UomConversionFactor { get; set; } = 1.0000m;

        public decimal BasePrice { get; set; }

        public decimal SellingPrice { get; set; }

        public decimal DiscountPercentage { get; set; }

        public decimal MinimumPrice { get; set; }

        public decimal FloorPrice { get; set; }

        public decimal MaximumDiscount { get; set; }

        public decimal TaxPercentage { get; set; }

        public string? HsnSacCode { get; set; }

        public string Remarks { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        // Optimistic concurrency token mapped to PostgreSQL xmin
        public uint Version { get; set; }

        public ICollection<PriceListItemSlab> Slabs { get; set; } = new List<PriceListItemSlab>();
    }
}
