using System;

namespace ERP.Domain.Sales
{
    public class QuotationItem
    {
        public int Id { get; set; }

        public string TenantId { get; set; } = "default";

        public int QuotationId { get; set; }

        public Quotation? Quotation { get; set; }

        // Physical Inventory Interlinkage
        public PricingItemType ItemType { get; set; } = PricingItemType.FinishedGood;

        public int? ItemId { get; set; }

        public int? ProductId { get; set; }

        public int? WarehouseId { get; set; }

        public string BaseStockUom { get; set; } = "Nos";

        public int LineNumber { get; set; } = 1;

        public string ItemCode { get; set; } = string.Empty;

        public string ItemName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Quantity { get; set; } = 1m;

        public string Unit { get; set; } = "NOS";

        public decimal UnitPrice { get; set; }

        public decimal DiscountPercent { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal TaxPercent { get; set; } = 18m;

        public decimal TaxAmount { get; set; }

        public decimal LineTotal { get; set; }

        public int SortOrder { get; set; } = 1;

        // Frozen Snapshot Fields from Price List Resolution Engine
        public int? ResolvedPriceListId { get; set; }

        public string PriceListNumberSnapshot { get; set; } = string.Empty;

        public string ResolutionStageSnapshot { get; set; } = string.Empty;

        public decimal OriginalSellingRate { get; set; }

        public decimal FloorPriceSnapshot { get; set; }

        public decimal MaxDiscountPctSnapshot { get; set; }

        public decimal TaxSlabPercentSnapshot { get; set; } = 18m;

        public DateTimeOffset SnapshotTimestampUtc { get; set; } = DateTimeOffset.UtcNow;

        public bool IsCommercialDeviation { get; set; }
    }
}
