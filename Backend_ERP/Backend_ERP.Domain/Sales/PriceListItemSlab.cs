using System;

namespace ERP.Domain.Sales
{
    public class PriceListItemSlab
    {
        public int Id { get; set; }

        public string TenantId { get; set; } = "default";

        public int PriceListItemId { get; set; }

        public PriceListItem PriceListItem { get; set; } = null!;

        public decimal MinQuantity { get; set; }

        public decimal? MaxQuantity { get; set; }

        public decimal Rate { get; set; }

        public decimal DiscountPercentage { get; set; }
    }
}
