using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Common.Interfaces;
using ERP.Application.Sales.Approvals;
using ERP.Application.Sales.Dtos;
using ERP.Application.Sales.Pricing;
using ERP.Application.Sales.Quotations;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Sales;
using ERP.Infrastructure.Sales.Caching;
using ERP.Infrastructure.Sales.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Backend_ERP.Tests
{
    public class EnterprisePriceListHardenedTests
    {
        private ERPDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ERPDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ERPDbContext(options);
        }

        private class MockTenantSettingsService : ITenantSettingsService
        {
            public Task<string> GetTenantTimeZoneIdAsync(string tenantId, CancellationToken cancellationToken = default)
            {
                return Task.FromResult("India Standard Time");
            }
        }

        [Fact]
        public async Task PriceResolutionEngine_VolumeSlab_Gap_Evaluates_HighestQualifyingFloor()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var cache = new HybridPricingCacheService(memoryCache, NullLogger<HybridPricingCacheService>.Instance);
            var tenantSettings = new MockTenantSettingsService();
            var engine = new PriceResolutionEngine(db, cache, tenantSettings);

            var priceList = new PriceList
            {
                Id = 1,
                TenantId = "TENANT-1",
                PriceListNumber = "PL-VOL-01",
                PriceListName = "Bulk Volume Test List",
                CustomerCategory = "Distributor",
                Currency = "INR",
                EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
                EffectiveTo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
                Status = PriceListStatuses.Active,
                SlabStrategy = VolumeSlabResolutionStrategy.HighestQualifyingFloor
            };

            priceList.AddItem(
                PricingItemType.FinishedGood,
                1,
                "FG-STEEL-01",
                "Steel Sheet 2mm",
                "Nos",
                basePrice: 100m,
                sellingPrice: 100m,
                floorPrice: 70m,
                maxDiscount: 20m,
                taxPercentage: 18m,
                baseUom: "Nos",
                pricingUom: "Nos",
                uomConversionFactor: 1.0m);

            // Slab A: 10 - 50 @ 90
            priceList.AddVolumeSlab("FG-STEEL-01", 10m, 50m, 90m, 10m);
            // Slab B: 100 - 500 @ 80
            priceList.AddVolumeSlab("FG-STEEL-01", 100m, 500m, 80m, 20m);

            db.PriceLists.Add(priceList);
            await db.SaveChangesAsync();

            // Act 1: Order 75 units (falls in the gap between 50 and 100)
            var gapResult = await engine.ResolveRateAsync(new PriceListResolveRequestDto
            {
                TenantId = "TENANT-1",
                CustomerCategory = "Distributor",
                ItemCode = "FG-STEEL-01",
                Quantity = 75m,
                Currency = "INR",
                RequestedUom = "Nos"
            });

            // Assert 1: Under HighestQualifyingFloor, 75 earns Slab A rate (90.00), NOT unearned Slab B (80.00)
            Assert.Equal(90m, gapResult.SellingPrice);
            Assert.Contains("Gap Qualifying Floor", gapResult.AppliedSlabDescription);

            // Act 2: Order 5 units (sub-threshold, lowest slab starts at 10)
            var subThresholdResult = await engine.ResolveRateAsync(new PriceListResolveRequestDto
            {
                TenantId = "TENANT-1",
                CustomerCategory = "Distributor",
                ItemCode = "FG-STEEL-01",
                Quantity = 5m,
                Currency = "INR",
                RequestedUom = "Nos"
            });

            // Assert 2: Sub-threshold orders pay base price (100.00)
            Assert.Equal(100m, subThresholdResult.SellingPrice);
            Assert.Contains("Sub-Threshold Order", subThresholdResult.AppliedSlabDescription);
        }

        [Fact]
        public async Task PriceResolutionEngine_UOM_Conversion_Calculates_Rate_Correctly()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var cache = new HybridPricingCacheService(memoryCache, NullLogger<HybridPricingCacheService>.Instance);
            var tenantSettings = new MockTenantSettingsService();
            var engine = new PriceResolutionEngine(db, cache, tenantSettings);

            var priceList = new PriceList
            {
                Id = 2,
                TenantId = "TENANT-1",
                PriceListNumber = "PL-UOM-01",
                PriceListName = "Packaging Unit Test List",
                CustomerCategory = "Standard",
                Currency = "INR",
                EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
                EffectiveTo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
                Status = PriceListStatuses.Active
            };

            // 1 BOX = 24 Nos, Base Rate = 240.00 per BOX -> 10.00 per Nos
            priceList.AddItem(
                PricingItemType.FinishedGood,
                10,
                "FG-FASTENER-BOX",
                "Fastener Bolt M8",
                "BOX",
                basePrice: 240m,
                sellingPrice: 240m,
                floorPrice: 192m,
                maxDiscount: 15m,
                taxPercentage: 18m,
                baseUom: "Nos",
                pricingUom: "BOX",
                uomConversionFactor: 24.0m);

            db.PriceLists.Add(priceList);
            await db.SaveChangesAsync();

            // Act: Request 12 individual pieces (in "Nos")
            var resolveResult = await engine.ResolveRateAsync(new PriceListResolveRequestDto
            {
                TenantId = "TENANT-1",
                CustomerCategory = "Standard",
                ItemCode = "FG-FASTENER-BOX",
                Quantity = 12m,
                Currency = "INR",
                RequestedUom = "Nos"
            });

            // Assert: Rate per Nos is 10.00 (240 / 24)
            Assert.Equal(10.00m, resolveResult.EffectiveRatePerQuotedUom);
            Assert.Equal(10.00m, resolveResult.SellingPrice);
            Assert.Equal(8.00m, resolveResult.FloorPrice); // 192 / 24 = 8.00 floor per piece
        }

        [Fact]
        public void CommercialComplianceService_Detects_FloorPrice_And_MaxDiscount_Violations()
        {
            // Arrange
            var complianceService = new CommercialComplianceService();

            var quotation = new Quotation
            {
                Id = 1,
                QuotationNumber = "QT-TEST-001",
                CustomerName = "Acme Corp",
                Currency = "INR",
                Items = new List<QuotationItem>
                {
                    new QuotationItem
                    {
                        Id = 1,
                        ItemCode = "FG-001",
                        Quantity = 10m,
                        UnitPrice = 85m,                // Quoted 85
                        FloorPriceSnapshot = 90m,       // Floor is 90 -> BREACH!
                        DiscountPercent = 5m,
                        MaxDiscountPctSnapshot = 15m
                    }
                }
            };

            // Act
            var result = complianceService.EvaluateQuotation(quotation);

            // Assert
            Assert.True(result.IsBlocked);
            Assert.Equal(CommercialDeviationSeverity.ExecutiveApproval, result.Severity);
            Assert.Contains("breaches absolute floor price", result.DeviationReason);
            Assert.Equal(50m, result.ViolationAmount); // (90 - 85) * 10 = 50 delta
        }

        [Fact]
        public void PriceList_Domain_Invariants_Enforce_Floor_Price_Less_Than_Selling_Rate()
        {
            // Arrange
            var priceList = new PriceList
            {
                Id = 3,
                PriceListName = "Invalid List"
            };

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                priceList.AddItem(
                    PricingItemType.FinishedGood,
                    1,
                    "FG-BAD",
                    "Bad Item",
                    "Nos",
                    basePrice: 100m,
                    sellingPrice: 100m,
                    floorPrice: 120m, // Invalid: floor > selling
                    maxDiscount: 10m,
                    taxPercentage: 18m);
            });

            Assert.Contains("cannot exceed selling rate", ex.Message);
        }
    }
}
