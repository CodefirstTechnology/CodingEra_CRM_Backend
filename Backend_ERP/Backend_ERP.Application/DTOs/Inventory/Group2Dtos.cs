using System;
using System.Collections.Generic;

namespace Backend_ERP.Application.DTOs.Inventory
{
    public class BatchAllocationRequestDto
    {
        public Guid ItemId { get; set; }
        public Guid WarehouseId { get; set; }
        public decimal RequestedQty { get; set; }
        public int MinUsableShelfLifeDays { get; set; } = 30;
    }

    public class FEFOAllocationResultDto
    {
        public Guid BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public Guid BinId { get; set; }
        public decimal AllocatedQty { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int RemainingDays { get; set; }

        public FEFOAllocationResultDto() { }

        public FEFOAllocationResultDto(Guid batchId, string batchNumber, Guid binId, decimal allocatedQty, DateTime expiryDate, int remainingDays)
        {
            BatchId = batchId;
            BatchNumber = batchNumber;
            BinId = binId;
            AllocatedQty = allocatedQty;
            ExpiryDate = expiryDate;
            RemainingDays = remainingDays;
        }
    }

    public class BatchQuarantineRequestDto
    {
        public string QuarantineReason { get; set; } = string.Empty;
        public Guid UserId { get; set; }
    }

    public class TransferOrderCreateDto
    {
        public Guid OriginWarehouseId { get; set; }
        public Guid DestinationWarehouseId { get; set; }
        public string? CarrierName { get; set; }
        public string? VehicleNumber { get; set; }
        public string? EwayBillNumber { get; set; }
        public Guid UserId { get; set; }
        public List<TransferLineCreateDto> Lines { get; set; } = new();
    }

    public class TransferLineCreateDto
    {
        public int ItemId { get; set; }
        public Guid? BatchId { get; set; }
        public Guid? OriginBinId { get; set; }
        public Guid? DestinationBinId { get; set; }
        public decimal RequestedQty { get; set; }
    }

    public class DispatchTransferRequestDto
    {
        public string? CarrierName { get; set; }
        public string? VehicleNumber { get; set; }
        public string? EwayBillNumber { get; set; }
    }

    public class ReceiveTransferRequestDto
    {
        public List<ReceiveTransferLineDto> Lines { get; set; } = new();
    }

    public class ReceiveTransferLineDto
    {
        public Guid LineId { get; set; }
        public Guid? DestinationBinId { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal DamagedQty { get; set; }
        public string? VarianceReason { get; set; }
    }
}
