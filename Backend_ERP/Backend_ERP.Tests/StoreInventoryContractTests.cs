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
    }
}
