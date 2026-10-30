using System;

namespace ERP.Domain.Sales
{
    public class SalesTargetItem
    {
        public int Id { get; set; }

        public string TenantId { get; set; } = "default";

        public int SalesTargetId { get; set; }

        public SalesTarget SalesTarget { get; set; } = null!;

        public int? ProductCategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public int? ItemMasterId { get; set; }

        public string ItemCode { get; set; } = string.Empty;

        public string ItemDescription { get; set; } = string.Empty;

        public decimal TargetQuantity { get; set; } = 0m;

        public decimal TargetRate { get; set; } = 0m;

        public decimal TargetValue { get; set; } = 0m;

        public decimal RealizedQuantity { get; set; } = 0m;

        public decimal RealizedValue { get; set; } = 0m;

        public decimal MinimumMarginPercentage { get; set; } = 0m;

        public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;
    }
}

