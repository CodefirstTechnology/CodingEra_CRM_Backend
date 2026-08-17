using System;
using System.Collections.Generic;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;
using Xunit;

namespace Backend_ERP.Tests
{
    public class PurchaseOrderContractTests
    {
        [Fact]
        public void PurchaseOrderRules_calculates_line_amounts_and_totals_correctly()
        {
            var lines = new List<PurchaseOrderLine>
            {
                new PurchaseOrderLine
                {
                    ItemName = "High Grade Steel Sheet",
                    Quantity = 10m,
                    Rate = 1000m,
                    Discount = 10m, // 10000 - 1000 = 9000
                    Tax = 18m // 9000 + 1620 = 10620
                },
                new PurchaseOrderLine
                {
                    ItemName = "Aluminum Alloy Ingot",
                    Quantity = 5m,
                    Rate = 500m,
                    Discount = 0m, // 2500
                    Tax = 18m // 2500 + 450 = 2950
                }
            };

            PurchaseOrderRules.CalculateTotals(lines, out var subtotal, out var discountTotal, out var taxTotal, out var grandTotal);

            Assert.Equal(12500m, subtotal);
            Assert.Equal(1000m, discountTotal);
            Assert.Equal(2070m, taxTotal);
            Assert.Equal(13570m, grandTotal);

            Assert.Equal(10620m, lines[0].Amount);
            Assert.Equal(2950m, lines[1].Amount);
        }

        [Theory]
        [InlineData(PurchaseOrderStatus.Draft, PurchaseOrderStatus.Submitted, true)]
        [InlineData(PurchaseOrderStatus.Submitted, PurchaseOrderStatus.Approved, true)]
        [InlineData(PurchaseOrderStatus.Approved, PurchaseOrderStatus.Ordered, true)]
        [InlineData(PurchaseOrderStatus.Ordered, PurchaseOrderStatus.Completed, true)]
        [InlineData(PurchaseOrderStatus.Completed, PurchaseOrderStatus.Draft, false)]
        public void PurchaseOrderRules_validates_status_transitions(PurchaseOrderStatus current, PurchaseOrderStatus target, bool expected)
        {
            Assert.Equal(expected, PurchaseOrderRules.CanTransition(current, target));
        }

        [Fact]
        public void PurchaseOrderDto_exposes_expected_json_contract_properties()
        {
            var dto = new PurchaseOrderDto
            {
                Id = 1,
                PurchaseOrderNumber = "PO-2026-000001",
                Vendor = new PurchaseOrderVendorDto
                {
                    VendorName = "Global Supplies Ltd",
                    GstNumber = "27AAAAA0000A1Z5"
                },
                TotalAmount = 50000m,
                Status = PurchaseOrderStatus.Approved,
                Priority = PurchaseOrderPriority.High
            };

            Assert.Equal("PO-2026-000001", dto.PurchaseOrderNumber);
            Assert.Equal("Global Supplies Ltd", dto.Vendor.VendorName);
            Assert.Equal(50000m, dto.TotalAmount);
            Assert.Equal(PurchaseOrderStatus.Approved, dto.Status);
            Assert.Equal(PurchaseOrderPriority.High, dto.Priority);
        }
    }
}
