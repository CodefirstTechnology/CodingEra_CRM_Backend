namespace ERP.Domain.Sales
{
    public class SalesOrderItem
    {
        public int Id { get; set; }

        public int SalesOrderId { get; set; }

        public SalesOrder SalesOrder { get; set; } = null!;

        /// <summary>Frontend wire id (GUID string).</summary>
        public string LineKey { get; set; } = string.Empty;

        public int SortIndex { get; set; }

        public string ItemName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public string Unit { get; set; } = "Nos";

        public decimal Rate { get; set; }

        /// <summary>Discount percent (0–100).</summary>
        public decimal Discount { get; set; }

        /// <summary>GST percent (0–100).</summary>
        public decimal Gst { get; set; }

        /// <summary>Line amount after discount + GST.</summary>
        public decimal Amount { get; set; }
    }
}
