using System;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;
using Xunit;

namespace Backend_ERP.Tests
{
    public class StoreInventoryContractTests
    {
        [Fact]
        public void StoreInventoryRules_validates_stock_out_quantities()
        {
            var err = StoreInventoryRules.ValidateStockOut(100m, 50m);
            Assert.Null(err);

            var invalidErr = StoreInventoryRules.ValidateStockOut(100m, 150m);
            Assert.NotNull(invalidErr);
            Assert.Contains("exceeds available stock", invalidErr);
        }

        [Theory]
        [InlineData(TransferStatus.Draft, TransferStatus.Approved, true)]
        [InlineData(TransferStatus.Approved, TransferStatus.Completed, true)]
        [InlineData(TransferStatus.Completed, TransferStatus.Draft, false)]
        public void StoreInventoryRules_validates_transfer_status_transitions(TransferStatus current, TransferStatus target, bool expected)
        {
            Assert.Equal(expected, StoreInventoryRules.CanTransitionTransfer(current, target));
        }

        [Fact]
        public void WarehouseDto_exposes_expected_json_contract_properties()
        {
            var dto = new WarehouseDto
            {
                Id = 1,
                Code = "WH-2026-000001",
                Name = "Central Raw Material Warehouse",
                Location = "Zone A, Bay 12",
                Capacity = 10000m,
                UsedCapacity = 2500m,
                AvailableCapacity = 7500m,
                Status = WarehouseStatus.Active
            };

            Assert.Equal("WH-2026-000001", dto.Code);
            Assert.Equal("Central Raw Material Warehouse", dto.Name);
            Assert.Equal(10000m, dto.Capacity);
            Assert.Equal(7500m, dto.AvailableCapacity);
            Assert.Equal(WarehouseStatus.Active, dto.Status);
        }

        [Fact]
        public void RawMaterialDto_exposes_expected_json_contract_properties()
        {
            var dto = new RawMaterialDto
            {
                Id = 1,
                MaterialCode = "MAT-2026-000001",
                MaterialName = "Steel Rod 20mm",
                Category = "Metals",
                AvailableStock = 500m,
                ReservedStock = 50m,
                UnitCost = 15.5m,
                CurrentValue = 7750m,
                StockAgeBand = StockAgeBand.Band0To30
            };

            Assert.Equal("MAT-2026-000001", dto.MaterialCode);
            Assert.Equal("Steel Rod 20mm", dto.MaterialName);
            Assert.Equal(500m, dto.AvailableStock);
            Assert.Equal(7750m, dto.CurrentValue);
        }

        [Fact]
        public void StockTransactionDto_exposes_expected_json_contract_properties()
        {
            var dto = new StockTransactionDto
            {
                Id = 1,
                TransactionNumber = "TXN-2026-000001",
                TransactionType = StockTxnType.StockIn,
                MaterialCode = "MAT-2026-000001",
                WarehouseName = "Main Store",
                Quantity = 100m,
                ReferenceType = StockReferenceType.GRN,
                ReferenceNumber = "GRN-2026-000001"
            };

            Assert.Equal("TXN-2026-000001", dto.TransactionNumber);
            Assert.Equal(StockTxnType.StockIn, dto.TransactionType);
            Assert.Equal(100m, dto.Quantity);
            Assert.Equal(StockReferenceType.GRN, dto.ReferenceType);
        }

        [Fact]
        public void StoreInventoryRules_validates_finished_goods_stock_bounds()
        {
            var err = StoreInventoryRules.ValidateStockOut(200m, 100m);
            Assert.Null(err);

            var invalidErr = StoreInventoryRules.ValidateStockOut(200m, 200.0002m);
            Assert.NotNull(invalidErr);
            Assert.Contains("exceeds available stock", invalidErr);
        }

        [Theory]
        [InlineData(TransferStatus.Draft, TransferStatus.Cancelled, true)]
        [InlineData(TransferStatus.Approved, TransferStatus.Cancelled, true)]
        [InlineData(TransferStatus.Transferred, TransferStatus.Cancelled, true)]
        [InlineData(TransferStatus.Completed, TransferStatus.Cancelled, false)]
        [InlineData(TransferStatus.Cancelled, TransferStatus.Draft, false)]
        public void StoreInventoryRules_validates_cancellation_status_transitions(TransferStatus current, TransferStatus target, bool expected)
        {
            Assert.Equal(expected, StoreInventoryRules.CanTransitionTransfer(current, target));
        }

        [Fact]
        public void StockTransferDto_exposes_expected_json_properties()
        {
            var dto = new StockTransferDto
            {
                Id = 1,
                TransferNumber = "TRF-2026-000001",
                FromWarehouseId = 1,
                FromWarehouseName = "Source WH",
                ToWarehouseId = 2,
                ToWarehouseName = "Dest WH",
                TotalQuantity = 150m,
                Status = TransferStatus.Draft,
                Remarks = "Urgent transfer"
            };

            Assert.Equal("TRF-2026-000001", dto.TransferNumber);
            Assert.Equal(1, dto.FromWarehouseId);
            Assert.Equal(2, dto.ToWarehouseId);
            Assert.Equal(150m, dto.TotalQuantity);
            Assert.Equal(TransferStatus.Draft, dto.Status);
            Assert.Equal("Urgent transfer", dto.Remarks);
        }

        [Fact]
        public void StockAlertDto_exposes_expected_json_properties()
        {
            var dto = new StockAlertDto
            {
                Id = 1,
                MaterialId = 10,
                MaterialCode = "MAT-2026-000010",
                MaterialName = "Copper Wire 1mm",
                WarehouseId = 1,
                WarehouseName = "Main Store",
                CurrentStock = 5m,
                MinimumStock = 10m,
                ReorderQuantity = 15m,
                Priority = AlertPriority.Warning,
                SuggestedPurchase = true
            };

            Assert.Equal("MAT-2026-000010", dto.MaterialCode);
            Assert.Equal("Copper Wire 1mm", dto.MaterialName);
            Assert.Equal(5m, dto.CurrentStock);
            Assert.Equal(AlertPriority.Warning, dto.Priority);
            Assert.True(dto.SuggestedPurchase);
        }

        [Theory]
        [InlineData(VerificationStatus.Scheduled, VerificationStatus.InProgress, true)]
        [InlineData(VerificationStatus.InProgress, VerificationStatus.Completed, true)]
        [InlineData(VerificationStatus.Completed, VerificationStatus.Adjusted, true)]
        [InlineData(VerificationStatus.Adjusted, VerificationStatus.Scheduled, false)]
        public void StoreInventoryRules_validates_verification_transitions(VerificationStatus current, VerificationStatus target, bool expected)
        {
            Assert.Equal(expected, StoreInventoryRules.CanTransitionVerification(current, target));
        }

        [Fact]
        public void PhysicalVerificationDto_exposes_expected_json_properties()
        {
            var dto = new PhysicalVerificationDto
            {
                Id = 1,
                VerificationNumber = "PV-2026-0001",
                WarehouseId = 1,
                WarehouseName = "Main WH",
                Verifier = "Verifier Name",
                ExpectedQuantity = 100m,
                ActualQuantity = 95m,
                Variance = -5m,
                VarianceValue = -250m,
                Status = VerificationStatus.Scheduled,
                AdjustmentPosted = false
            };

            Assert.Equal("PV-2026-0001", dto.VerificationNumber);
            Assert.Equal("Main WH", dto.WarehouseName);
            Assert.Equal("Verifier Name", dto.Verifier);
            Assert.Equal(100m, dto.ExpectedQuantity);
            Assert.Equal(95m, dto.ActualQuantity);
            Assert.Equal(-5m, dto.Variance);
            Assert.Equal(-250m, dto.VarianceValue);
            Assert.Equal(VerificationStatus.Scheduled, dto.Status);
            Assert.False(dto.AdjustmentPosted);
        }

        [Fact]
        public void StockValuationSummaryDto_exposes_expected_json_properties()
        {
            var dto = new StockValuationSummaryDto
            {
                InventoryValue = 1000m,
                OpeningValue = 800m,
                ClosingValue = 1000m,
                AverageCost = 50m,
                FifoPlaceholder = "FIFO valuation will be calculated by the backend inventory engine.",
                WeightedAveragePlaceholder = "Weighted average cost will be calculated by the backend inventory engine.",
                WarehouseValues = new List<WarehouseValueDto>
                {
                    new() { WarehouseId = 1, WarehouseName = "Main WH", Value = 1000m, Quantity = 20m }
                },
                CategoryValues = new List<CategoryValueDto>
                {
                    new() { Category = "Raw Materials", Value = 1000m, Quantity = 20m }
                }
            };

            Assert.Equal(1000m, dto.InventoryValue);
            Assert.Equal(800m, dto.OpeningValue);
            Assert.Equal("FIFO valuation will be calculated by the backend inventory engine.", dto.FifoPlaceholder);
            Assert.Single(dto.WarehouseValues);
            Assert.Single(dto.CategoryValues);
        }

        [Fact]
        public void VerificationDashboardDto_exposes_expected_json_properties()
        {
            var dto = new VerificationDashboardDto
            {
                PendingVerifications = 2,
                Completed = 5,
                VarianceAmount = 150m,
                Cards = new List<DashCardDto>
                {
                    new() { Label = "Pending Verifications", Value = "2" }
                }
            };

            Assert.Equal(2, dto.PendingVerifications);
            Assert.Equal(5, dto.Completed);
            Assert.Equal(150m, dto.VarianceAmount);
            Assert.Single(dto.Cards);
        }
    }
}
