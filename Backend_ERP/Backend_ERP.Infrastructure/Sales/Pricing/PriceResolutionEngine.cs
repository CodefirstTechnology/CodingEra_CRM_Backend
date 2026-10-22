using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Common.Interfaces;
using ERP.Application.Sales.Dtos;
using ERP.Application.Sales.Pricing;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales.Pricing
{
    public class PriceResolutionEngine : IPriceResolutionEngine
    {
        private readonly ERPDbContext _db;
        private readonly IHybridPricingCache _cache;
        private readonly ITenantSettingsService _tenantSettings;

        public PriceResolutionEngine(
            ERPDbContext db,
            IHybridPricingCache cache,
            ITenantSettingsService tenantSettings)
        {
            _db = db;
            _cache = cache;
            _tenantSettings = tenantSettings;
        }

        public async Task<PriceListResolveDto> ResolveRateAsync(
            PriceListResolveRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.ItemCode))
            {
                throw new InvalidOperationException("Item code is required for price resolution.");
            }

            var tenantId = string.IsNullOrWhiteSpace(request.TenantId) ? "default" : request.TenantId.Trim();
            var itemCode = request.ItemCode.Trim();

            // 1. Timezone Normalization: Convert instant to Tenant Local Calendar Date
            var requestInstantUtc = request.EffectiveDateUtc ?? DateTimeOffset.UtcNow;
            var tenantTzId = await _tenantSettings.GetTenantTimeZoneIdAsync(tenantId, cancellationToken);
            var tenantTz = TimeZoneInfo.FindSystemTimeZoneById(tenantTzId);
            var tenantLocalTime = TimeZoneInfo.ConvertTime(requestInstantUtc, tenantTz);
            var effectiveDateOnly = DateOnly.FromDateTime(tenantLocalTime.Date);

            var requestedQty = request.Quantity <= 0 ? 1.0m : request.Quantity;
            var currency = string.IsNullOrWhiteSpace(request.Currency) ? "INR" : request.Currency.Trim().ToUpperInvariant();
            var requestedUom = string.IsNullOrWhiteSpace(request.RequestedUom) ? "Nos" : request.RequestedUom.Trim();
            var customerId = request.CustomerId?.Trim() ?? string.Empty;
            var customerCategory = request.CustomerCategory?.Trim() ?? "Standard";

            // 2. Tenant-Scoped Cache Key
            var cacheKey = $"erp:pricing:v1:{tenantId}:{customerCategory}:{customerId}:{itemCode}:{requestedUom}:{currency}:{effectiveDateOnly:yyyyMMdd}:{requestedQty}";

            return await _cache.GetOrCreateAsync(cacheKey, tenantId, async () =>
            {
                // =========================================================================
                // WATERFALL STAGE 1: Customer-Specific Contract Override
                // =========================================================================
                if (!string.IsNullOrWhiteSpace(customerId))
                {
                    var customerContract = await (
                        from cplm in _db.CustomerPriceListMappings.AsNoTracking()
                        join pl in _db.PriceLists.AsNoTracking() on cplm.PriceListId equals pl.Id
                        join item in _db.PriceListItems.AsNoTracking().Include(i => i.Slabs) on pl.Id equals item.PriceListId
                        where cplm.TenantId == tenantId
                          && cplm.CustomerId == customerId
                          && cplm.IsActive
                          && pl.Status == PriceListStatuses.Active
                          && !pl.IsDeleted
                          && pl.Currency == currency
                          && pl.EffectiveFrom <= effectiveDateOnly && (pl.EffectiveTo == null || pl.EffectiveTo >= effectiveDateOnly)
                          && item.ItemCode == itemCode
                        orderby cplm.PriorityWeight descending, pl.Priority descending
                        select new { List = pl, Item = item }
                    ).FirstOrDefaultAsync(cancellationToken);

                    if (customerContract != null)
                    {
                        return CalculateCommercialRate(customerContract.List, customerContract.Item, requestedQty, requestedUom, "CustomerContractMapping", effectiveDateOnly);
                    }
                }

                // =========================================================================
                // WATERFALL STAGE 2: Customer Tier / Channel Group
                // =========================================================================
                var tierMatch = await (
                    from pl in _db.PriceLists.AsNoTracking()
                    join item in _db.PriceListItems.AsNoTracking().Include(i => i.Slabs) on pl.Id equals item.PriceListId
                    where pl.TenantId == tenantId
                      && pl.CustomerCategory == customerCategory
                      && pl.Status == PriceListStatuses.Active
                      && !pl.IsDeleted
                      && pl.Currency == currency
                      && pl.EffectiveFrom <= effectiveDateOnly && (pl.EffectiveTo == null || pl.EffectiveTo >= effectiveDateOnly)
                      && item.ItemCode == itemCode
                    orderby pl.Priority descending, pl.EffectiveFrom descending
                    select new { List = pl, Item = item }
                ).FirstOrDefaultAsync(cancellationToken);

                if (tierMatch != null)
                {
                    return CalculateCommercialRate(tierMatch.List, tierMatch.Item, requestedQty, requestedUom, "CustomerTierPriceList", effectiveDateOnly);
                }

                // =========================================================================
                // WATERFALL STAGE 3: Default Global Sales Price List
                // =========================================================================
                var globalMatch = await (
                    from pl in _db.PriceLists.AsNoTracking()
                    join item in _db.PriceListItems.AsNoTracking().Include(i => i.Slabs) on pl.Id equals item.PriceListId
                    where pl.TenantId == tenantId
                      && pl.CustomerCategory == PriceListCustomerCategories.Standard
                      && pl.Status == PriceListStatuses.Active
                      && !pl.IsDeleted
                      && pl.Currency == currency
                      && pl.EffectiveFrom <= effectiveDateOnly && (pl.EffectiveTo == null || pl.EffectiveTo >= effectiveDateOnly)
                      && item.ItemCode == itemCode
                    orderby pl.Priority descending, pl.EffectiveFrom descending
                    select new { List = pl, Item = item }
                ).FirstOrDefaultAsync(cancellationToken);

                if (globalMatch != null)
                {
                    return CalculateCommercialRate(globalMatch.List, globalMatch.Item, requestedQty, requestedUom, "GlobalStandardPriceList", effectiveDateOnly);
                }

                // =========================================================================
                // WATERFALL STAGE 4: Item Master Base Fallback
                // =========================================================================
                var fallbackFg = await _db.FinishedGoods.AsNoTracking()
                    .Where(fg => fg.ProductCode == itemCode && !fg.IsDeleted)
                    .Select(fg => new
                    {
                        fg.ProductCode,
                        fg.ProductName,
                        fg.Unit,
                        StandardSellingRate = fg.UnitCost * 1.25m,
                        FloorPrice = fg.UnitCost * 1.05m
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (fallbackFg != null)
                {
                    return new PriceListResolveDto
                    {
                        PriceListId = 0,
                        PriceListNumber = "ITEM-MASTER-FALLBACK",
                        PriceListName = "Item Master Standard 25% Markup Fallback",
                        ItemCode = fallbackFg.ProductCode,
                        ItemName = fallbackFg.ProductName,
                        CustomerCategory = customerCategory,
                        Currency = currency,
                        QuotedUom = requestedUom,
                        BaseStockUom = fallbackFg.Unit,
                        PricingUom = fallbackFg.Unit,
                        UomConversionMultiplier = 1.0m,
                        EffectiveRatePerQuotedUom = Math.Round(fallbackFg.StandardSellingRate, 4),
                        EffectiveRatePerBaseUom = Math.Round(fallbackFg.StandardSellingRate, 4),
                        BasePrice = Math.Round(fallbackFg.StandardSellingRate, 2),
                        SellingPrice = Math.Round(fallbackFg.StandardSellingRate, 2),
                        MinimumPrice = Math.Round(fallbackFg.FloorPrice, 2),
                        FloorPrice = Math.Round(fallbackFg.FloorPrice, 2),
                        MaximumDiscount = 5.0m,
                        TaxPercentage = 18.0m,
                        ResolutionStage = "ItemMasterFallback",
                        AppliedSlabDescription = "Item Master Base Markup Fallback (Qty 1+)",
                        EffectiveFrom = effectiveDateOnly.ToString("yyyy-MM-dd"),
                        ResolvedViaPriority = false
                    };
                }

                throw new InvalidOperationException(
                    $"Commercial Failure: Zero pricing resolution paths satisfied for item '{itemCode}' in tenant '{tenantId}'.");
            }, TimeSpan.FromMinutes(10));
        }

        private static PriceListResolveDto CalculateCommercialRate(
            PriceList pl,
            PriceListItem item,
            decimal requestedQty,
            string requestedUom,
            string stage,
            DateOnly effectiveDate)
        {
            // 1. UOM Multiplier Normalization
            decimal uomConversion = 1.0m;
            var pricingUom = string.IsNullOrWhiteSpace(item.PricingUom) ? item.Unit : item.PricingUom;
            var baseUom = string.IsNullOrWhiteSpace(item.BaseUom) ? item.Unit : item.BaseUom;
            var factor = item.UomConversionFactor > 0 ? item.UomConversionFactor : 1.0000m;

            if (requestedUom.Equals(pricingUom, StringComparison.OrdinalIgnoreCase))
            {
                uomConversion = 1.0m;
            }
            else if (requestedUom.Equals(baseUom, StringComparison.OrdinalIgnoreCase))
            {
                uomConversion = 1.0m / factor;
            }
            else
            {
                uomConversion = 1.0m;
            }

            // Normalize quantity into Base Inventory Units for volume slab evaluation
            decimal baseQtyForSlab = requestedQty * (requestedUom.Equals(pricingUom, StringComparison.OrdinalIgnoreCase)
                ? factor
                : 1.0m);

            decimal resolvedPricingRate = item.SellingPrice;
            decimal resolvedDiscount = item.DiscountPercentage;
            string slabDescription = "Standard Base Rate";

            // 2. Volume Slab Gap Handling
            if (item.Slabs != null && item.Slabs.Count > 0)
            {
                var sortedSlabs = item.Slabs.OrderBy(s => s.MinQuantity).ToList();
                var lowestMin = sortedSlabs.First().MinQuantity;

                if (baseQtyForSlab < lowestMin)
                {
                    resolvedPricingRate = item.SellingPrice;
                    resolvedDiscount = item.DiscountPercentage;
                    slabDescription = $"Sub-Threshold Order ({baseQtyForSlab} < {lowestMin} Base Units): Base Rate Applied";
                }
                else
                {
                    var exactIntervalMatch = sortedSlabs
                        .Where(s => baseQtyForSlab >= s.MinQuantity && (!s.MaxQuantity.HasValue || baseQtyForSlab <= s.MaxQuantity.Value))
                        .FirstOrDefault();

                    if (exactIntervalMatch != null)
                    {
                        resolvedPricingRate = exactIntervalMatch.Rate;
                        resolvedDiscount = exactIntervalMatch.DiscountPercentage;
                        slabDescription = $"Exact Slab Match: {exactIntervalMatch.MinQuantity} - {exactIntervalMatch.MaxQuantity?.ToString() ?? "Infinity"}";
                    }
                    else
                    {
                        // FALLS INTO AN EXPLICIT GAP BETWEEN SLABS
                        if (pl.SlabStrategy == VolumeSlabResolutionStrategy.HighestQualifyingFloor)
                        {
                            var qualifiedSlab = sortedSlabs
                                .Where(s => s.MinQuantity <= baseQtyForSlab)
                                .OrderByDescending(s => s.MinQuantity)
                                .First();

                            resolvedPricingRate = qualifiedSlab.Rate;
                            resolvedDiscount = qualifiedSlab.DiscountPercentage;
                            slabDescription = $"Gap Qualifying Floor: Earned Slab [{qualifiedSlab.MinQuantity} - {qualifiedSlab.MaxQuantity}] for {baseQtyForSlab} units";
                        }
                        else
                        {
                            resolvedPricingRate = item.SellingPrice;
                            resolvedDiscount = item.DiscountPercentage;
                            slabDescription = $"Gap Fallback: Uncontracted volume gap ({baseQtyForSlab} units) defaulted to Base Rate";
                        }
                    }
                }
            }

            // 3. Compute Effective Rates across Quoted UOM and Base UOM
            decimal effectiveRatePerQuoted = resolvedPricingRate * uomConversion;
            decimal effectiveRatePerBase = resolvedPricingRate / factor;
            decimal effectiveFloor = (item.FloorPrice > 0 ? item.FloorPrice : item.MinimumPrice) * uomConversion;

            return new PriceListResolveDto
            {
                PriceListId = pl.Id,
                PriceListNumber = pl.PriceListNumber,
                PriceListName = pl.PriceListName,
                Priority = pl.Priority,
                CustomerCategory = pl.CustomerCategory,
                Currency = pl.Currency,
                ItemCode = item.ItemCode,
                ItemName = item.ItemName,
                QuotedUom = requestedUom,
                BaseStockUom = baseUom,
                PricingUom = pricingUom,
                UomConversionMultiplier = uomConversion,
                EffectiveRatePerQuotedUom = Math.Round(effectiveRatePerQuoted, 4),
                EffectiveRatePerBaseUom = Math.Round(effectiveRatePerBase, 4),
                BasePrice = item.BasePrice,
                SellingPrice = Math.Round(effectiveRatePerQuoted, 2),
                DiscountPercentage = resolvedDiscount,
                MinimumPrice = Math.Round(effectiveFloor, 2),
                FloorPrice = Math.Round(effectiveFloor, 2),
                MaximumDiscount = item.MaximumDiscount,
                TaxPercentage = item.TaxPercentage,
                ResolutionStage = stage,
                AppliedSlabDescription = slabDescription,
                EffectiveFrom = pl.EffectiveFrom.ToString("yyyy-MM-dd"),
                EffectiveTo = pl.EffectiveTo?.ToString("yyyy-MM-dd"),
                AlternativeListsAvailable = 0,
                ResolvedViaPriority = pl.Priority > 0
            };
        }
    }
}
