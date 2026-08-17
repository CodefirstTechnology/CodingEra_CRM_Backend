using System;
using System.Collections.Generic;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;
using Xunit;

namespace Backend_ERP.Tests
{
    public class VendorComparisonContractTests
    {
        [Fact]
        public void VendorComparisonRules_computes_lowest_price_and_best_delivery_metrics()
        {
            var entries = new List<VendorComparisonEntry>
            {
                new VendorComparisonEntry
                {
                    VendorId = 1,
                    VendorName = "Vendor Alpha",
                    TotalCost = 50000m,
                    DeliveryTimeDays = 14
                },
                new VendorComparisonEntry
                {
                    VendorId = 2,
                    VendorName = "Vendor Beta",
                    TotalCost = 45000m,
                    DeliveryTimeDays = 21
                },
                new VendorComparisonEntry
                {
                    VendorId = 3,
                    VendorName = "Vendor Gamma",
                    TotalCost = 48000m,
                    DeliveryTimeDays = 7
                }
            };

            VendorComparisonRules.EvaluateMetrics(entries);

            var beta = entries.Find(e => e.VendorId == 2);
            var gamma = entries.Find(e => e.VendorId == 3);
            var alpha = entries.Find(e => e.VendorId == 1);

            Assert.NotNull(beta);
            Assert.True(beta!.IsLowestPrice);
            Assert.False(beta.IsBestDelivery);

            Assert.NotNull(gamma);
            Assert.False(gamma!.IsLowestPrice);
            Assert.True(gamma.IsBestDelivery);

            Assert.Equal(1, beta.Ranking);
        }

        [Theory]
        [InlineData(VendorComparisonStatus.Pending, VendorComparisonStatus.Compared, true)]
        [InlineData(VendorComparisonStatus.Compared, VendorComparisonStatus.Approved, true)]
        [InlineData(VendorComparisonStatus.Approved, VendorComparisonStatus.Awarded, true)]
        [InlineData(VendorComparisonStatus.Awarded, VendorComparisonStatus.Pending, false)]
        public void VendorComparisonRules_validates_status_transitions(VendorComparisonStatus current, VendorComparisonStatus target, bool expected)
        {
            Assert.Equal(expected, VendorComparisonRules.CanTransition(current, target));
        }

        [Fact]
        public void VendorComparisonCreateRequestDto_exposes_expected_contract_properties()
        {
            var req = new VendorComparisonCreateRequestDto
            {
                RFQId = 10,
                Title = "Steel Rod Procurement Comparison",
                RecommendedVendorId = 2,
                Vendors = new List<VendorQuotationEntryDto>
                {
                    new VendorQuotationEntryDto
                    {
                        VendorId = 2,
                        VendorName = "Tata Steel Distributors",
                        QuotationRef = "VQ-2026-000005",
                        UnitPrice = 450m,
                        TotalCost = 45000m,
                        DeliveryTimeDays = 5
                    }
                }
            };

            Assert.Equal(10, req.RFQId);
            Assert.Equal("Steel Rod Procurement Comparison", req.Title);
            Assert.Equal(2, req.RecommendedVendorId);
            Assert.Single(req.Vendors);
            Assert.Equal("Tata Steel Distributors", req.Vendors[0].VendorName);
            Assert.Equal(45000m, req.Vendors[0].TotalCost);
        }
    }
}
