using System;
using System.Collections.Generic;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;
using Xunit;

namespace Backend_ERP.Tests
{
    public class RFQContractTests
    {
        [Fact]
        public void RFQRules_validates_dates_and_lines()
        {
            var now = DateTime.UtcNow;
            Assert.Throws<InvalidOperationException>(() =>
                RFQRules.ValidateCreate(now, now.AddDays(-2), new List<(string, decimal)> { ("Item 1", 5m) }));

            Assert.Throws<InvalidOperationException>(() =>
                RFQRules.ValidateCreate(now, now.AddDays(5), new List<(string, decimal)>()));
        }

        [Theory]
        [InlineData(RFQStatus.Draft, RFQStatus.Sent, true)]
        [InlineData(RFQStatus.Sent, RFQStatus.VendorResponsesReceived, true)]
        [InlineData(RFQStatus.Sent, RFQStatus.ComparisonReady, true)]
        [InlineData(RFQStatus.ComparisonReady, RFQStatus.Awarded, true)]
        [InlineData(RFQStatus.Closed, RFQStatus.Draft, false)]
        public void RFQRules_validates_status_transitions(RFQStatus current, RFQStatus target, bool expected)
        {
            Assert.Equal(expected, RFQRules.CanTransition(current, target));
        }

        [Fact]
        public void RFQCreateRequestDto_exposes_expected_contract_properties()
        {
            var req = new RFQCreateRequestDto
            {
                RFQDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(7),
                DeliveryTerms = "FOB Destination",
                PaymentTerms = "Net 30",
                VendorIds = new List<int> { 1, 2 },
                Lines = new List<RFQLineDto>
                {
                    new RFQLineDto
                    {
                        ItemName = "Stainless Steel Bolts",
                        Quantity = 100,
                        Uom = "BOX",
                        TargetUnitPrice = 250m
                    }
                }
            };

            Assert.Equal("FOB Destination", req.DeliveryTerms);
            Assert.Equal(2, req.VendorIds.Count);
            Assert.Single(req.Lines);
            Assert.Equal("Stainless Steel Bolts", req.Lines[0].ItemName);
            Assert.Equal(250m, req.Lines[0].TargetUnitPrice);
        }
    }
}
