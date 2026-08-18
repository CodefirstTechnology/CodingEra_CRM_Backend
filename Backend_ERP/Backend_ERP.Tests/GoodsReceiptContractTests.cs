using System;
using System.Collections.Generic;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;
using Xunit;

namespace Backend_ERP.Tests
{
    public class GoodsReceiptContractTests
    {
        [Fact]
        public void GoodsReceiptRules_validates_over_receiving_and_summary_calculations()
        {
            var items = new List<GoodsReceiptItem>
            {
                new GoodsReceiptItem
                {
                    ItemName = "Heavy Duty Steel Pipe",
                    OrderedQuantity = 100m,
                    PreviouslyReceivedQuantity = 30m,
                    RemainingQuantity = 70m,
                    ReceivedQuantity = 50m,
                    RejectedQuantity = 5m
                },
                new GoodsReceiptItem
                {
                    ItemName = "High Grade Fasteners",
                    OrderedQuantity = 500m,
                    PreviouslyReceivedQuantity = 100m,
                    RemainingQuantity = 400m,
                    ReceivedQuantity = 400m,
                    RejectedQuantity = 0m
                }
            };

            var err = GoodsReceiptRules.ValidateItems(items);
            Assert.Null(err);

            var (ordered, received, remaining, rejected, pct) = GoodsReceiptRules.CalculateSummary(items);

            Assert.Equal(600m, ordered);
            Assert.Equal(450m, received);
            Assert.Equal(150m, remaining);
            Assert.Equal(5m, rejected);
            Assert.Equal(75m, pct);
        }

        [Fact]
        public void GoodsReceiptRules_detects_over_receiving_errors()
        {
            var invalidItems = new List<GoodsReceiptItem>
            {
                new GoodsReceiptItem
                {
                    ItemName = "Precision Gear Assembly",
                    OrderedQuantity = 10m,
                    PreviouslyReceivedQuantity = 5m,
                    RemainingQuantity = 5m,
                    ReceivedQuantity = 6m, // Exceeds remaining (5)
                    RejectedQuantity = 0m
                }
            };

            var err = GoodsReceiptRules.ValidateItems(invalidItems);
            Assert.NotNull(err);
            Assert.Contains("exceeds remaining", err);
        }

        [Theory]
        [InlineData(GoodsReceiptStatus.Draft, GoodsReceiptStatus.Submitted, true)]
        [InlineData(GoodsReceiptStatus.Submitted, GoodsReceiptStatus.Completed, true)]
        [InlineData(GoodsReceiptStatus.Completed, GoodsReceiptStatus.Draft, false)]
        [InlineData(GoodsReceiptStatus.Cancelled, GoodsReceiptStatus.Completed, false)]
        public void GoodsReceiptRules_validates_status_transitions(GoodsReceiptStatus current, GoodsReceiptStatus target, bool expected)
        {
            Assert.Equal(expected, GoodsReceiptRules.CanTransition(current, target));
        }

        [Fact]
        public void GoodsReceiptDto_exposes_expected_json_contract_properties()
        {
            var dto = new GoodsReceiptDto
            {
                Id = 1,
                GRNNumber = "GRN-2026-000001",
                PurchaseOrderId = 10,
                PurchaseOrderNumber = "PO-2026-000005",
                VendorName = "Quality Metals Inc",
                Warehouse = "Main Store — Sanand",
                Status = GoodsReceiptStatus.Completed
            };

            Assert.Equal("GRN-2026-000001", dto.GRNNumber);
            Assert.Equal(10, dto.PurchaseOrderId);
            Assert.Equal("PO-2026-000005", dto.PurchaseOrderNumber);
            Assert.Equal("Quality Metals Inc", dto.VendorName);
            Assert.Equal("Main Store — Sanand", dto.Warehouse);
            Assert.Equal(GoodsReceiptStatus.Completed, dto.Status);
        }
    }
}
