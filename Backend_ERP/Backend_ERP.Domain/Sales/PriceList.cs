using System;
using System.Collections.Generic;
using System.Linq;
using NpgsqlTypes;

namespace ERP.Domain.Sales
{
    public class PriceList
    {
        public int Id { get; set; }

        public string TenantId { get; set; } = "default";

        public string PriceListNumber { get; set; } = string.Empty;

        public string PriceListName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public PriceListTierType TierType { get; set; } = PriceListTierType.Retail;

        public SalesChannelType Channel { get; set; } = SalesChannelType.Direct;

        public string CustomerCategory { get; set; } = PriceListCustomerCategories.Standard;

        public string Currency { get; set; } = "INR";

        public NpgsqlRange<DateOnly> ValidityPeriod { get; set; }

        public DateOnly EffectiveFrom { get; set; }

        public DateOnly? EffectiveTo { get; set; }

        public string Status { get; set; } = PriceListStatuses.Draft;

        public int Priority { get; set; } = 0;

        public VolumeSlabResolutionStrategy SlabStrategy { get; set; } = VolumeSlabResolutionStrategy.HighestQualifyingFloor;

        public bool IsAutoRepriceEnabled { get; set; }

        public string Remarks { get; set; } = string.Empty;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTimeOffset CreatedDate { get; set; }

        public string UpdatedBy { get; set; } = string.Empty;

        public DateTimeOffset UpdatedDate { get; set; }

        public bool IsDeleted { get; set; }

        // Optimistic concurrency token mapped to PostgreSQL xmin
        public uint Version { get; set; }

        public ICollection<PriceListItem> Items { get; set; } = new List<PriceListItem>();

        public ICollection<CustomerPriceListMapping> CustomerMappings { get; set; } = new List<CustomerPriceListMapping>();

        public ICollection<PriceListHistory> History { get; set; } = new List<PriceListHistory>();

        public void AddItem(
            PricingItemType itemType,
            int itemId,
            string itemCode,
            string itemName,
            string unit,
            decimal basePrice,
            decimal sellingPrice,
            decimal floorPrice,
            decimal maxDiscount,
            decimal taxPercentage,
            string baseUom = "Nos",
            string pricingUom = "Nos",
            decimal uomConversionFactor = 1.0000m)
        {
            if (string.IsNullOrWhiteSpace(itemCode))
                throw new InvalidOperationException("Item Code is mandatory.");

            if (sellingPrice < 0 || basePrice < 0)
                throw new InvalidOperationException("Rates cannot be negative.");

            var effectiveFloor = floorPrice > 0 ? floorPrice : sellingPrice * 0.90m;
            if (effectiveFloor > sellingPrice)
                throw new InvalidOperationException($"Floor price ({effectiveFloor}) cannot exceed selling rate ({sellingPrice}).");

            if (maxDiscount < 0 || maxDiscount > 100)
                throw new InvalidOperationException("Maximum discount must be between 0% and 100%.");

            if (Items.Any(i => i.ItemCode.Equals(itemCode.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Item '{itemCode}' already exists in this price list.");

            Items.Add(new PriceListItem
            {
                TenantId = TenantId,
                PriceListId = Id,
                ItemType = itemType,
                ItemId = itemId,
                ItemCode = itemCode.Trim().ToUpperInvariant(),
                ItemName = itemName.Trim(),
                Unit = unit.Trim(),
                BaseUom = string.IsNullOrWhiteSpace(baseUom) ? unit.Trim() : baseUom.Trim(),
                PricingUom = string.IsNullOrWhiteSpace(pricingUom) ? unit.Trim() : pricingUom.Trim(),
                UomConversionFactor = uomConversionFactor > 0 ? uomConversionFactor : 1.0000m,
                BasePrice = basePrice,
                SellingPrice = sellingPrice,
                MinimumPrice = effectiveFloor,
                FloorPrice = effectiveFloor,
                MaximumDiscount = maxDiscount,
                TaxPercentage = taxPercentage,
                SortOrder = Items.Count + 1
            });
        }

        public void AddVolumeSlab(string itemCode, decimal minQty, decimal? maxQty, decimal rate, decimal discountPct)
        {
            var item = Items.FirstOrDefault(i => i.ItemCode.Equals(itemCode.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Item '{itemCode}' does not exist in price list.");

            if (minQty < 1.0m)
                throw new InvalidOperationException("Minimum quantity slab must be >= 1.0.");

            if (maxQty.HasValue && maxQty.Value <= minQty)
                throw new InvalidOperationException("Max quantity must be greater than Min quantity.");

            var effectiveFloor = item.FloorPrice > 0 ? item.FloorPrice : item.MinimumPrice;
            if (rate < effectiveFloor)
                throw new InvalidOperationException($"Slab rate ({rate}) cannot be lower than item floor price ({effectiveFloor}).");

            if (item.Slabs.Any(s =>
                (minQty >= s.MinQuantity && (!s.MaxQuantity.HasValue || minQty <= s.MaxQuantity.Value)) ||
                (maxQty.HasValue && maxQty.Value >= s.MinQuantity && (!s.MaxQuantity.HasValue || maxQty.Value <= s.MaxQuantity.Value))))
            {
                throw new InvalidOperationException("Volume slab bounds cannot overlap with existing slabs.");
            }

            item.Slabs.Add(new PriceListItemSlab
            {
                TenantId = TenantId,
                PriceListItemId = item.Id,
                MinQuantity = minQty,
                MaxQuantity = maxQty,
                Rate = rate,
                DiscountPercentage = discountPct
            });
        }

        public void Activate(string actingUser)
        {
            if (Status == PriceListStatuses.Active)
                return;

            if (EffectiveTo.HasValue && EffectiveTo.Value < EffectiveFrom)
                throw new InvalidOperationException("Effective To date cannot precede Effective From date.");

            if (!Items.Any())
                throw new InvalidOperationException("Cannot activate an empty price list without item records.");

            Status = PriceListStatuses.Active;
            UpdatedBy = actingUser;
            UpdatedDate = DateTimeOffset.UtcNow;

            ValidityPeriod = new NpgsqlRange<DateOnly>(
                EffectiveFrom,
                true,
                false,
                EffectiveTo ?? DateOnly.MaxValue,
                EffectiveTo.HasValue,
                !EffectiveTo.HasValue);
        }
    }
}
