using System;
using System.Collections.Generic;
using ERP.Domain.Procurement;

namespace ERP.Application.Procurement.Dtos
{
    public class GateInwardRequestDto
    {
        public int PurchaseOrderId { get; set; }
        public string? VehicleNumber { get; set; }
        public string? TransporterName { get; set; }
        public string? LRNumber { get; set; }
        public string? EWayBillNumber { get; set; }
        public decimal? WeighbridgeGrossWeight { get; set; }
        public decimal? WeighbridgeTareWeight { get; set; }
        public string? Warehouse { get; set; }
        public string? Remarks { get; set; }
        public List<GateInwardLineItemDto> Items { get; set; } = new();
    }

    public class GateInwardLineItemDto
    {
        public int PurchaseOrderLineId { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public string? BatchNumber { get; set; }
        public string? HeatNumber { get; set; }
        public string? MtcNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? Remarks { get; set; }
    }

    public class QcDispositionRequestDto
    {
        public int GoodsReceiptId { get; set; }
        public string? InspectionLotNumber { get; set; }
        public QualityInspectionStatus OverallStatus { get; set; } = QualityInspectionStatus.Passed;
        public List<QcItemDispositionDto> Items { get; set; } = new();
        public string? Remarks { get; set; }
    }

    public class QcItemDispositionDto
    {
        public int GoodsReceiptItemId { get; set; }
        public decimal QcInspectedQuantity { get; set; }
        public decimal QcAcceptedQuantity { get; set; }
        public decimal QcRejectedQuantity { get; set; }
        public decimal QcReworkQuantity { get; set; }
        public string? RejectionReasonCode { get; set; }
        public string? BatchNumber { get; set; }
        public string? HeatNumber { get; set; }
        public string? MtcNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? QcDispositionNotes { get; set; }
    }

    public class GoodsReceiptPostRequestDto
    {
        public int GoodsReceiptId { get; set; }
        public string? Remarks { get; set; }
    }

    public class InventoryValuationResultDto
    {
        public int GoodsReceiptId { get; set; }
        public int ItemsValuedCount { get; set; }
        public decimal TotalValuationAmount { get; set; }
        public List<StockTransactionResultDto> Transactions { get; set; } = new();
    }

    public class StockTransactionResultDto
    {
        public int StockTransactionId { get; set; }
        public string TransactionNumber { get; set; } = string.Empty;
        public string MaterialCode { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal PreviousWac { get; set; }
        public decimal NewWac { get; set; }
        public decimal NewBalance { get; set; }
    }
}

