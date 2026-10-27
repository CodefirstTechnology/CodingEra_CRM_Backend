using System;

namespace ERP.Domain.Sales
{
    public class ProformaInvoiceItem
    {
        public int Id { get; set; }

        public int ProformaInvoiceId { get; set; }

        public ProformaInvoice ProformaInvoice { get; set; } = null!;

        public int? SalesOrderItemId { get; set; }

        public int? QuotationItemId { get; set; }

        public string LineKey { get; set; } = string.Empty;

        public string ItemCode { get; set; } = string.Empty;

        public string ItemName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string HsnCode { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public string Unit { get; set; } = "Nos";

        public decimal Rate { get; set; }

        public decimal Discount { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal Gst { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal Amount { get; set; }

        // Zero-Recalculation Frozen Commercial Snapshots
        public decimal OriginalSellingRate { get; set; }

        public decimal FloorPriceSnapshot { get; set; }

        public decimal MaxDiscountPctSnapshot { get; set; }

        public decimal TaxSlabPercentSnapshot { get; set; }

        public string PlaceOfSupplyStateCode { get; set; } = "27";

        /// <summary>How much of this line item has already been converted to a Sales Invoice.</summary>
        public decimal ConvertedQuantity { get; set; } = 0m;

        public decimal RemainingQuantity => Math.Max(0m, Quantity - ConvertedQuantity);

        public int SortOrder { get; set; }
    }
}
