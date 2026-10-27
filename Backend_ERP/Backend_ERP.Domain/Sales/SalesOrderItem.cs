using System.Collections.Generic;

namespace ERP.Domain.Sales
{
    public class SalesOrderItem
    {
        public int Id { get; set; }

        public string TenantId { get; set; } = "default";

        public int SalesOrderId { get; set; }

        public SalesOrder SalesOrder { get; set; } = null!;

        /// <summary>Frontend wire id (GUID string).</summary>
        public string LineKey { get; set; } = string.Empty;

        public int SortIndex { get; set; }

        // Physical item linkages
        public PricingItemType ItemType { get; set; } = PricingItemType.FinishedGood;

        public int? ItemId { get; set; }

        public int? WarehouseId { get; set; }

        public string ItemCode { get; set; } = string.Empty;

        public string ItemName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public decimal AllocatedQuantity { get; set; } = 0m;

        public decimal DispatchedQuantity { get; set; } = 0m;

        public decimal InvoicedQuantity { get; set; } = 0m;

        public decimal ProformaInvoicedQuantity { get; set; } = 0m;

        public string Unit { get; set; } = "Nos";

        public string BaseStockUom { get; set; } = "Nos";

        public decimal Rate { get; set; }

        /// <summary>Discount percent (0–100).</summary>
        public decimal Discount { get; set; }

        /// <summary>GST percent (0–100).</summary>
        public decimal Gst { get; set; }

        /// <summary>Line amount after discount + GST.</summary>
        public decimal Amount { get; set; }

        // Frozen Commercial Snapshots Promoted from Quotation or Resolved from Price List
        public int? ResolvedPriceListId { get; set; }

        public string PriceListNumberSnapshot { get; set; } = string.Empty;

        public decimal OriginalSellingRate { get; set; }

        public decimal FloorPriceSnapshot { get; set; }

        public decimal MaxDiscountPctSnapshot { get; set; }

        public decimal TaxSlabPercentSnapshot { get; set; }

        public string PlaceOfSupplyStateCode { get; set; } = "27";

        public ICollection<SalesOrderAllocation> Allocations { get; set; } = new List<SalesOrderAllocation>();
    }
}
