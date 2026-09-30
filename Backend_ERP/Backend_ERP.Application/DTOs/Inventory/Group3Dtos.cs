using System;
using System.Collections.Generic;

namespace Backend_ERP.Application.DTOs.Inventory
{
    public class SetThresholdDto
    {
        public int ItemId { get; set; }
        public Guid WarehouseId { get; set; }
        public decimal MinStockLevel { get; set; }
        public decimal ReorderPoint { get; set; }
        public decimal MaxStockLevel { get; set; }
        public decimal SafetyStock { get; set; }
    }

    public class StockAlertDto
    {
        public Guid AlertId { get; set; }
        public Guid ThresholdId { get; set; }
        public int ItemId { get; set; }
        public Guid WarehouseId { get; set; }
        public string AlertType { get; set; } = string.Empty;
        public string AlertPriority { get; set; } = string.Empty;
        public decimal CurrentQty { get; set; }
        public decimal AvailableQty { get; set; }
        public decimal OnOrderQty { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class StockValuationSummaryDto
    {
        public int ItemId { get; set; }
        public Guid WarehouseId { get; set; }
        public decimal TotalQty { get; set; }
        public decimal TotalWacValue { get; set; }
        public decimal WeightedAvgCost { get; set; }
        public decimal TotalFifoValue { get; set; }
        public int ActiveFifoLayers { get; set; }
    }

    public class ValuationInwardReceiptDto
    {
        public int ItemId { get; set; }
        public Guid WarehouseId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public DateTime? ReceiptDate { get; set; }
    }

    public class ValuationOutwardDepletionDto
    {
        public int ItemId { get; set; }
        public Guid WarehouseId { get; set; }
        public decimal Quantity { get; set; }
    }

    public class ValuationDepletionResultDto
    {
        public decimal TotalCostDepleted { get; set; }
        public decimal AverageCostPerUnit { get; set; }
        public int LayersDepleted { get; set; }
    }

    public class CreateCountSheetDto
    {
        public Guid WarehouseId { get; set; }
        public int ConductedByUserId { get; set; }
        public List<CountLineCreateDto> Lines { get; set; } = new();
    }

    public class CountLineCreateDto
    {
        public int ItemId { get; set; }
        public Guid? BinId { get; set; }
        public Guid? BatchId { get; set; }
        public decimal SystemQty { get; set; }
        public decimal CountedQty { get; set; }
        public string? VarianceReason { get; set; }
    }

    public class ReconcileCountSheetDto
    {
        public int ReconciledByUserId { get; set; }
    }

    public class CountSheetDto
    {
        public Guid SheetId { get; set; }
        public string SheetNumber { get; set; } = string.Empty;
        public Guid WarehouseId { get; set; }
        public DateTime CountDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ConductedByUserId { get; set; }
        public int? ReconciledByUserId { get; set; }
        public DateTime? ReconciledAt { get; set; }
        public List<CountLineDto> Lines { get; set; } = new();
    }

    public class CountLineDto
    {
        public Guid LineId { get; set; }
        public int ItemId { get; set; }
        public Guid? BinId { get; set; }
        public Guid? BatchId { get; set; }
        public decimal SystemQty { get; set; }
        public decimal CountedQty { get; set; }
        public decimal VarianceQty { get; set; }
        public string? VarianceReason { get; set; }
    }
}
