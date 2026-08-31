using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ERP.Domain.Procurement;

namespace ERP.Application.Procurement.Dtos
{
    public class InventoryTimelineEventDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("user")]
        public string User { get; set; } = string.Empty;

        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        [JsonPropertyName("fromStatus")]
        public string? FromStatus { get; set; }

        [JsonPropertyName("toStatus")]
        public string? ToStatus { get; set; }

        [JsonPropertyName("remarks")]
        public string? Remarks { get; set; }
    }

    public class InventoryAttachmentDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("sizeKb")]
        public double SizeKb { get; set; }

        [JsonPropertyName("uploadedBy")]
        public string UploadedBy { get; set; } = string.Empty;

        [JsonPropertyName("uploadedAt")]
        public string UploadedAt { get; set; } = string.Empty;
    }

    public class StoreListQueryDto
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public int? WarehouseId { get; set; }
        public string? Category { get; set; }
        public string? TransactionType { get; set; }
        public string? Priority { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    // ── Warehouses ──

    public class WarehouseListItemDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("location")]
        public string Location { get; set; } = string.Empty;

        [JsonPropertyName("manager")]
        public string Manager { get; set; } = string.Empty;

        [JsonPropertyName("capacity")]
        public decimal Capacity { get; set; }

        [JsonPropertyName("usedCapacity")]
        public decimal UsedCapacity { get; set; }

        [JsonPropertyName("availableCapacity")]
        public decimal AvailableCapacity { get; set; }

        [JsonPropertyName("status")]
        public WarehouseStatus Status { get; set; }

        [JsonPropertyName("assignedInventoryCount")]
        public int AssignedInventoryCount { get; set; }
    }

    public class WarehouseDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("location")]
        public string Location { get; set; } = string.Empty;

        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;

        [JsonPropertyName("manager")]
        public string Manager { get; set; } = string.Empty;

        [JsonPropertyName("capacity")]
        public decimal Capacity { get; set; }

        [JsonPropertyName("usedCapacity")]
        public decimal UsedCapacity { get; set; }

        [JsonPropertyName("availableCapacity")]
        public decimal AvailableCapacity { get; set; }

        [JsonPropertyName("status")]
        public WarehouseStatus Status { get; set; }

        [JsonPropertyName("assignedInventoryCount")]
        public int AssignedInventoryCount { get; set; }

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedBy")]
        public string UpdatedBy { get; set; } = string.Empty;

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("timeline")]
        public List<InventoryTimelineEventDto> Timeline { get; set; } = new();

        [JsonPropertyName("attachments")]
        public List<InventoryAttachmentDto> Attachments { get; set; } = new();
    }

    public class WarehouseCreateRequestDto
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("location")]
        public string Location { get; set; } = string.Empty;

        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;

        [JsonPropertyName("manager")]
        public string Manager { get; set; } = string.Empty;

        [JsonPropertyName("capacity")]
        public decimal Capacity { get; set; }

        [JsonPropertyName("status")]
        public WarehouseStatus Status { get; set; } = WarehouseStatus.Active;

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }

    public class WarehouseDashboardDto
    {
        [JsonPropertyName("cards")]
        public List<DashCardDto> Cards { get; set; } = new();

        [JsonPropertyName("totalWarehouses")]
        public int TotalWarehouses { get; set; }

        [JsonPropertyName("capacityUsagePercent")]
        public decimal CapacityUsagePercent { get; set; }

        [JsonPropertyName("inventoryDistribution")]
        public List<WarehouseDistDto> InventoryDistribution { get; set; } = new();
    }

    public class DashCardDto
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("hint")]
        public string? Hint { get; set; }

        [JsonPropertyName("tone")]
        public string? Tone { get; set; }
    }

    public class WarehouseDistDto
    {
        [JsonPropertyName("warehouseName")]
        public string WarehouseName { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public decimal Value { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    // ── Raw Materials ──

    public class RawMaterialListItemDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("materialCode")]
        public string MaterialCode { get; set; } = string.Empty;

        [JsonPropertyName("materialName")]
        public string MaterialName { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("warehouseName")]
        public string WarehouseName { get; set; } = string.Empty;

        [JsonPropertyName("unit")]
        public string Unit { get; set; } = "Nos";

        [JsonPropertyName("availableStock")]
        public decimal AvailableStock { get; set; }

        [JsonPropertyName("reservedStock")]
        public decimal ReservedStock { get; set; }

        [JsonPropertyName("minimumStock")]
        public decimal MinimumStock { get; set; }

        [JsonPropertyName("reorderLevel")]
        public decimal ReorderLevel { get; set; }

        [JsonPropertyName("currentValue")]
        public decimal CurrentValue { get; set; }

        [JsonPropertyName("stockAgeBand")]
        public StockAgeBand StockAgeBand { get; set; }

        [JsonPropertyName("supplier")]
        public string Supplier { get; set; } = string.Empty;

        [JsonPropertyName("lastReceiptDate")]
        public DateTime? LastReceiptDate { get; set; }

        [JsonPropertyName("linkedGrnNumber")]
        public string? LinkedGRNNumber { get; set; }

        [JsonPropertyName("isLowStock")]
        public bool IsLowStock { get; set; }

        [JsonPropertyName("isOutOfStock")]
        public bool IsOutOfStock { get; set; }
    }

    public class RawMaterialDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("materialCode")]
        public string MaterialCode { get; set; } = string.Empty;

        [JsonPropertyName("materialName")]
        public string MaterialName { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("warehouseId")]
        public int WarehouseId { get; set; }

        [JsonPropertyName("warehouseName")]
        public string WarehouseName { get; set; } = string.Empty;

        [JsonPropertyName("rack")]
        public string Rack { get; set; } = string.Empty;

        [JsonPropertyName("unit")]
        public string Unit { get; set; } = "Nos";

        [JsonPropertyName("openingStock")]
        public decimal OpeningStock { get; set; }

        [JsonPropertyName("availableStock")]
        public decimal AvailableStock { get; set; }

        [JsonPropertyName("reservedStock")]
        public decimal ReservedStock { get; set; }

        [JsonPropertyName("minimumStock")]
        public decimal MinimumStock { get; set; }

        [JsonPropertyName("maximumStock")]
        public decimal MaximumStock { get; set; }

        [JsonPropertyName("reorderLevel")]
        public decimal ReorderLevel { get; set; }

        [JsonPropertyName("unitCost")]
        public decimal UnitCost { get; set; }

        [JsonPropertyName("currentValue")]
        public decimal CurrentValue { get; set; }

        [JsonPropertyName("batchCount")]
        public int BatchCount { get; set; }

        [JsonPropertyName("stockAgeDays")]
        public int StockAgeDays { get; set; }

        [JsonPropertyName("stockAgeBand")]
        public StockAgeBand StockAgeBand { get; set; }

        [JsonPropertyName("supplier")]
        public string Supplier { get; set; } = string.Empty;

        [JsonPropertyName("lastReceiptDate")]
        public DateTime? LastReceiptDate { get; set; }

        [JsonPropertyName("linkedGrnId")]
        public int? LinkedGRNId { get; set; }

        [JsonPropertyName("linkedGrnNumber")]
        public string? LinkedGRNNumber { get; set; }

        [JsonPropertyName("linkedPoId")]
        public int? LinkedPOId { get; set; }

        [JsonPropertyName("linkedPoNumber")]
        public string? LinkedPONumber { get; set; }

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedBy")]
        public string UpdatedBy { get; set; } = string.Empty;

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("timeline")]
        public List<InventoryTimelineEventDto> Timeline { get; set; } = new();

        [JsonPropertyName("attachments")]
        public List<InventoryAttachmentDto> Attachments { get; set; } = new();
    }

    public class StockAdjustRequestDto
    {
        [JsonPropertyName("materialId")]
        public int MaterialId { get; set; }

        [JsonPropertyName("warehouseId")]
        public int WarehouseId { get; set; }

        [JsonPropertyName("quantityDelta")]
        public decimal QuantityDelta { get; set; }

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;

        [JsonPropertyName("remarks")]
        public string? Remarks { get; set; }
    }

    public class RawMaterialDashboardDto
    {
        [JsonPropertyName("cards")]
        public List<DashCardDto> Cards { get; set; } = new();

        [JsonPropertyName("totalMaterials")]
        public int TotalMaterials { get; set; }

        [JsonPropertyName("lowStock")]
        public int LowStock { get; set; }

        [JsonPropertyName("outOfStock")]
        public int OutOfStock { get; set; }

        [JsonPropertyName("recentReceipts")]
        public int RecentReceipts { get; set; }

        [JsonPropertyName("inventoryValue")]
        public decimal InventoryValue { get; set; }
    }

    // ── Finished Goods ──

    public class FinishedGoodListItemDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("productCode")]
        public string ProductCode { get; set; } = string.Empty;

        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("finishedQuantity")]
        public decimal FinishedQuantity { get; set; }

        [JsonPropertyName("reservedQuantity")]
        public decimal ReservedQuantity { get; set; }

        [JsonPropertyName("availableQuantity")]
        public decimal AvailableQuantity { get; set; }

        [JsonPropertyName("warehouseName")]
        public string WarehouseName { get; set; } = string.Empty;

        [JsonPropertyName("batchNumber")]
        public string BatchNumber { get; set; } = string.Empty;

        [JsonPropertyName("productionReference")]
        public string ProductionReference { get; set; } = string.Empty;

        [JsonPropertyName("manufacturingDate")]
        public DateTime ManufacturingDate { get; set; }

        [JsonPropertyName("expiryDate")]
        public DateTime? ExpiryDate { get; set; }

        [JsonPropertyName("currentValue")]
        public decimal CurrentValue { get; set; }

        [JsonPropertyName("dispatchStatus")]
        public FgDispatchStatus DispatchStatus { get; set; }
    }

    public class FinishedGoodDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("productCode")]
        public string ProductCode { get; set; } = string.Empty;

        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("finishedQuantity")]
        public decimal FinishedQuantity { get; set; }

        [JsonPropertyName("reservedQuantity")]
        public decimal ReservedQuantity { get; set; }

        [JsonPropertyName("availableQuantity")]
        public decimal AvailableQuantity { get; set; }

        [JsonPropertyName("warehouseId")]
        public int WarehouseId { get; set; }

        [JsonPropertyName("warehouseName")]
        public string WarehouseName { get; set; } = string.Empty;

        [JsonPropertyName("batchNumber")]
        public string BatchNumber { get; set; } = string.Empty;

        [JsonPropertyName("productionReference")]
        public string ProductionReference { get; set; } = string.Empty;

        [JsonPropertyName("manufacturingDate")]
        public DateTime ManufacturingDate { get; set; }

        [JsonPropertyName("expiryDate")]
        public DateTime? ExpiryDate { get; set; }

        [JsonPropertyName("unitCost")]
        public decimal UnitCost { get; set; }

        [JsonPropertyName("currentValue")]
        public decimal CurrentValue { get; set; }

        [JsonPropertyName("dispatchStatus")]
        public FgDispatchStatus DispatchStatus { get; set; }

        [JsonPropertyName("unit")]
        public string Unit { get; set; } = "Nos";

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedBy")]
        public string UpdatedBy { get; set; } = string.Empty;

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("timeline")]
        public List<InventoryTimelineEventDto> Timeline { get; set; } = new();

        [JsonPropertyName("attachments")]
        public List<InventoryAttachmentDto> Attachments { get; set; } = new();
    }

    public class FinishedGoodAdjustRequestDto
    {
        [JsonPropertyName("productId")]
        public int ProductId { get; set; }

        [JsonPropertyName("quantityDelta")]
        public decimal QuantityDelta { get; set; }

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;

        [JsonPropertyName("remarks")]
        public string? Remarks { get; set; }
    }

    public class FinishedGoodDashboardDto
    {
        [JsonPropertyName("cards")]
        public List<DashCardDto> Cards { get; set; } = new();

        [JsonPropertyName("availableProducts")]
        public int AvailableProducts { get; set; }

        [JsonPropertyName("reserved")]
        public int Reserved { get; set; }

        [JsonPropertyName("readyToDispatch")]
        public int ReadyToDispatch { get; set; }

        [JsonPropertyName("inventoryValue")]
        public decimal InventoryValue { get; set; }
    }

    // ── Stock Transactions ──

    public class StockTransactionListItemDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("transactionNumber")]
        public string TransactionNumber { get; set; } = string.Empty;

        [JsonPropertyName("transactionType")]
        public StockTxnType TransactionType { get; set; }

        [JsonPropertyName("materialCode")]
        public string MaterialCode { get; set; } = string.Empty;

        [JsonPropertyName("materialName")]
        public string MaterialName { get; set; } = string.Empty;

        [JsonPropertyName("warehouseName")]
        public string WarehouseName { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("unit")]
        public string Unit { get; set; } = "Nos";

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;

        [JsonPropertyName("referenceType")]
        public StockReferenceType ReferenceType { get; set; }

        [JsonPropertyName("referenceNumber")]
        public string ReferenceNumber { get; set; } = string.Empty;

        [JsonPropertyName("user")]
        public string User { get; set; } = string.Empty;

        [JsonPropertyName("transactionDate")]
        public DateTime TransactionDate { get; set; }
    }

    public class StockTransactionDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("transactionNumber")]
        public string TransactionNumber { get; set; } = string.Empty;

        [JsonPropertyName("transactionType")]
        public StockTxnType TransactionType { get; set; }

        [JsonPropertyName("materialId")]
        public int? MaterialId { get; set; }

        [JsonPropertyName("materialCode")]
        public string MaterialCode { get; set; } = string.Empty;

        [JsonPropertyName("materialName")]
        public string MaterialName { get; set; } = string.Empty;

        [JsonPropertyName("warehouseId")]
        public int WarehouseId { get; set; }

        [JsonPropertyName("warehouseName")]
        public string WarehouseName { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("unit")]
        public string Unit { get; set; } = "Nos";

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;

        [JsonPropertyName("referenceType")]
        public StockReferenceType ReferenceType { get; set; }

        [JsonPropertyName("referenceNumber")]
        public string ReferenceNumber { get; set; } = string.Empty;

        [JsonPropertyName("referenceId")]
        public int? ReferenceId { get; set; }

        [JsonPropertyName("user")]
        public string User { get; set; } = string.Empty;

        [JsonPropertyName("transactionDate")]
        public DateTime TransactionDate { get; set; }

        [JsonPropertyName("remarks")]
        public string Remarks { get; set; } = string.Empty;

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("timeline")]
        public List<InventoryTimelineEventDto> Timeline { get; set; } = new();

        [JsonPropertyName("attachments")]
        public List<InventoryAttachmentDto> Attachments { get; set; } = new();
    }

    public class StockInRequestDto
    {
        [JsonPropertyName("materialId")]
        public int MaterialId { get; set; }

        [JsonPropertyName("warehouseId")]
        public int WarehouseId { get; set; }

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;

        [JsonPropertyName("referenceType")]
        public string? ReferenceType { get; set; }

        [JsonPropertyName("referenceNumber")]
        public string? ReferenceNumber { get; set; }

        [JsonPropertyName("remarks")]
        public string? Remarks { get; set; }

        [JsonPropertyName("transactionDate")]
        public DateTime? TransactionDate { get; set; }
    }

    public class StockOutRequestDto : StockInRequestDto {}

    public class StockTxnDashboardDto
    {
        [JsonPropertyName("cards")]
        public List<DashCardDto> Cards { get; set; } = new();

        [JsonPropertyName("todayTransactions")]
        public int TodayTransactions { get; set; }

        [JsonPropertyName("stockIn")]
        public int StockIn { get; set; }

        [JsonPropertyName("stockOut")]
        public int StockOut { get; set; }

        [JsonPropertyName("adjustments")]
        public int Adjustments { get; set; }
    }

    // ── Batches ──

    public class InventoryBatchListItemDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("batchNumber")]
        public string BatchNumber { get; set; } = string.Empty;

        [JsonPropertyName("materialCode")]
        public string MaterialCode { get; set; } = string.Empty;

        [JsonPropertyName("materialName")]
        public string MaterialName { get; set; } = string.Empty;

        [JsonPropertyName("warehouseName")]
        public string WarehouseName { get; set; } = string.Empty;

        [JsonPropertyName("supplier")]
        public string Supplier { get; set; } = string.Empty;

        [JsonPropertyName("grnNumber")]
        public string? GRNNumber { get; set; }

        [JsonPropertyName("manufacturingDate")]
        public DateTime ManufacturingDate { get; set; }

        [JsonPropertyName("expiryDate")]
        public DateTime? ExpiryDate { get; set; }

        [JsonPropertyName("availableQuantity")]
        public decimal AvailableQuantity { get; set; }

        [JsonPropertyName("consumedQuantity")]
        public decimal ConsumedQuantity { get; set; }

        [JsonPropertyName("remainingQuantity")]
        public decimal RemainingQuantity { get; set; }

        [JsonPropertyName("unit")]
        public string Unit { get; set; } = "Nos";

        [JsonPropertyName("status")]
        public BatchStatus Status { get; set; }
    }

    public class InventoryBatchDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("batchNumber")]
        public string BatchNumber { get; set; } = string.Empty;

        [JsonPropertyName("materialId")]
        public int MaterialId { get; set; }

        [JsonPropertyName("materialCode")]
        public string MaterialCode { get; set; } = string.Empty;

        [JsonPropertyName("materialName")]
        public string MaterialName { get; set; } = string.Empty;

        [JsonPropertyName("warehouseId")]
        public int WarehouseId { get; set; }

        [JsonPropertyName("warehouseName")]
        public string WarehouseName { get; set; } = string.Empty;

        [JsonPropertyName("supplier")]
        public string Supplier { get; set; } = string.Empty;

        [JsonPropertyName("grnId")]
        public int? GRNId { get; set; }

        [JsonPropertyName("grnNumber")]
        public string? GRNNumber { get; set; }

        [JsonPropertyName("manufacturingDate")]
        public DateTime ManufacturingDate { get; set; }

        [JsonPropertyName("expiryDate")]
        public DateTime? ExpiryDate { get; set; }

        [JsonPropertyName("availableQuantity")]
        public decimal AvailableQuantity { get; set; }

        [JsonPropertyName("consumedQuantity")]
        public decimal ConsumedQuantity { get; set; }

        [JsonPropertyName("remainingQuantity")]
        public decimal RemainingQuantity { get; set; }

        [JsonPropertyName("unit")]
        public string Unit { get; set; } = "Nos";

        [JsonPropertyName("unitCost")]
        public decimal UnitCost { get; set; }

        [JsonPropertyName("status")]
        public BatchStatus Status { get; set; }

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedBy")]
        public string UpdatedBy { get; set; } = string.Empty;

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("timeline")]
        public List<InventoryTimelineEventDto> Timeline { get; set; } = new();
    }

    public class BatchDashboardDto
    {
        [JsonPropertyName("cards")]
        public List<DashCardDto> Cards { get; set; } = new();

        [JsonPropertyName("activeBatches")]
        public int ActiveBatches { get; set; }

        [JsonPropertyName("expiringSoon")]
        public int ExpiringSoon { get; set; }

        [JsonPropertyName("expired")]
        public int Expired { get; set; }

        [JsonPropertyName("consumed")]
        public int Consumed { get; set; }
    }

    // ── Stock Transfers ──

    public class StockTransferItemDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("materialId")]
        public int MaterialId { get; set; }

        [JsonPropertyName("materialCode")]
        public string MaterialCode { get; set; } = string.Empty;

        [JsonPropertyName("materialName")]
        public string MaterialName { get; set; } = string.Empty;

        [JsonPropertyName("unit")]
        public string Unit { get; set; } = "Nos";

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("batchNumber")]
        public string? BatchNumber { get; set; }
    }

    public class StockTransferListItemDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("transferNumber")]
        public string TransferNumber { get; set; } = string.Empty;

        [JsonPropertyName("fromWarehouseName")]
        public string FromWarehouseName { get; set; } = string.Empty;

        [JsonPropertyName("toWarehouseName")]
        public string ToWarehouseName { get; set; } = string.Empty;

        [JsonPropertyName("itemCount")]
        public int ItemCount { get; set; }

        [JsonPropertyName("totalQuantity")]
        public decimal TotalQuantity { get; set; }

        [JsonPropertyName("status")]
        public TransferStatus Status { get; set; }

        [JsonPropertyName("transferDate")]
        public DateTime TransferDate { get; set; }

        [JsonPropertyName("requestedBy")]
        public string RequestedBy { get; set; } = string.Empty;

        [JsonPropertyName("approvedBy")]
        public string? ApprovedBy { get; set; }
    }

    public class StockTransferDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("transferNumber")]
        public string TransferNumber { get; set; } = string.Empty;

        [JsonPropertyName("fromWarehouseId")]
        public int FromWarehouseId { get; set; }

        [JsonPropertyName("fromWarehouseName")]
        public string FromWarehouseName { get; set; } = string.Empty;

        [JsonPropertyName("toWarehouseId")]
        public int ToWarehouseId { get; set; }

        [JsonPropertyName("toWarehouseName")]
        public string ToWarehouseName { get; set; } = string.Empty;

        [JsonPropertyName("items")]
        public List<StockTransferItemDto> Items { get; set; } = new();

        [JsonPropertyName("totalQuantity")]
        public decimal TotalQuantity { get; set; }

        [JsonPropertyName("status")]
        public TransferStatus Status { get; set; }

        [JsonPropertyName("transferDate")]
        public DateTime TransferDate { get; set; }

        [JsonPropertyName("requestedBy")]
        public string RequestedBy { get; set; } = string.Empty;

        [JsonPropertyName("approvedBy")]
        public string? ApprovedBy { get; set; }

        [JsonPropertyName("remarks")]
        public string Remarks { get; set; } = string.Empty;

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedBy")]
        public string UpdatedBy { get; set; } = string.Empty;

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("timeline")]
        public List<InventoryTimelineEventDto> Timeline { get; set; } = new();

        [JsonPropertyName("attachments")]
        public List<InventoryAttachmentDto> Attachments { get; set; } = new();
    }

    public class StockTransferCreateRequestDto
    {
        [JsonPropertyName("fromWarehouseId")]
        public int FromWarehouseId { get; set; }

        [JsonPropertyName("toWarehouseId")]
        public int ToWarehouseId { get; set; }

        [JsonPropertyName("transferDate")]
        public DateTime TransferDate { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("remarks")]
        public string? Remarks { get; set; }

        [JsonPropertyName("items")]
        public List<StockTransferItemReqDto> Items { get; set; } = new();
    }

    public class StockTransferItemReqDto
    {
        [JsonPropertyName("materialId")]
        public int MaterialId { get; set; }

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("batchNumber")]
        public string? BatchNumber { get; set; }
    }

    public class StockTransferStatusUpdateDto
    {
        [JsonPropertyName("status")]
        public TransferStatus Status { get; set; }

        [JsonPropertyName("remarks")]
        public string? Remarks { get; set; }
    }

    public class TransferDashboardDto
    {
        [JsonPropertyName("cards")]
        public List<DashCardDto> Cards { get; set; } = new();

        [JsonPropertyName("pendingTransfers")]
        public int PendingTransfers { get; set; }

        [JsonPropertyName("completed")]
        public int Completed { get; set; }

        [JsonPropertyName("inTransit")]
        public int InTransit { get; set; }
    }

    // ── Stock Alerts ──

    public class StockAlertDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("materialId")]
        public int MaterialId { get; set; }

        [JsonPropertyName("materialCode")]
        public string MaterialCode { get; set; } = string.Empty;

        [JsonPropertyName("materialName")]
        public string MaterialName { get; set; } = string.Empty;

        [JsonPropertyName("warehouseId")]
        public int WarehouseId { get; set; }

        [JsonPropertyName("warehouseName")]
        public string WarehouseName { get; set; } = string.Empty;

        [JsonPropertyName("currentStock")]
        public decimal CurrentStock { get; set; }

        [JsonPropertyName("minimumStock")]
        public decimal MinimumStock { get; set; }

        [JsonPropertyName("reorderQuantity")]
        public decimal ReorderQuantity { get; set; }

        [JsonPropertyName("unit")]
        public string Unit { get; set; } = "Nos";

        [JsonPropertyName("priority")]
        public AlertPriority Priority { get; set; }

        [JsonPropertyName("suggestedPurchase")]
        public bool SuggestedPurchase { get; set; }

        [JsonPropertyName("linkedRequisitionId")]
        public int? LinkedRequisitionId { get; set; }

        [JsonPropertyName("linkedRequisitionNumber")]
        public string? LinkedRequisitionNumber { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
    }

    public class AlertDashboardDto
    {
        [JsonPropertyName("cards")]
        public List<DashCardDto> Cards { get; set; } = new();

        [JsonPropertyName("criticalAlerts")]
        public int CriticalAlerts { get; set; }

        [JsonPropertyName("warningAlerts")]
        public int WarningAlerts { get; set; }

        [JsonPropertyName("normal")]
        public int Normal { get; set; }
    }

    // ── Stock Valuation ──

    public class StockValuationRowDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("materialId")]
        public int MaterialId { get; set; }

        [JsonPropertyName("materialCode")]
        public string MaterialCode { get; set; } = string.Empty;

        [JsonPropertyName("materialName")]
        public string MaterialName { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("warehouseId")]
        public int WarehouseId { get; set; }

        [JsonPropertyName("warehouseName")]
        public string WarehouseName { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("unit")]
        public string Unit { get; set; } = "Nos";

        [JsonPropertyName("averageCost")]
        public decimal AverageCost { get; set; }

        [JsonPropertyName("openingValue")]
        public decimal OpeningValue { get; set; }

        [JsonPropertyName("closingValue")]
        public decimal ClosingValue { get; set; }

        [JsonPropertyName("currentValue")]
        public decimal CurrentValue { get; set; }
    }

    public class StockValuationSummaryDto
    {
        [JsonPropertyName("inventoryValue")]
        public decimal InventoryValue { get; set; }

        [JsonPropertyName("openingValue")]
        public decimal OpeningValue { get; set; }

        [JsonPropertyName("closingValue")]
        public decimal ClosingValue { get; set; }

        [JsonPropertyName("averageCost")]
        public decimal AverageCost { get; set; }

        [JsonPropertyName("warehouseValues")]
        public List<WarehouseValueDto> WarehouseValues { get; set; } = new();

        [JsonPropertyName("categoryValues")]
        public List<CategoryValueDto> CategoryValues { get; set; } = new();

        [JsonPropertyName("fifoPlaceholder")]
        public string FifoPlaceholder { get; set; } = "FIFO Valuation Supported";

        [JsonPropertyName("weightedAveragePlaceholder")]
        public string WeightedAveragePlaceholder { get; set; } = "Weighted Average Supported";

        [JsonPropertyName("asOfDate")]
        public DateTime AsOfDate { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("rows")]
        public List<StockValuationRowDto> Rows { get; set; } = new();
    }

    public class WarehouseValueDto
    {
        [JsonPropertyName("warehouseId")]
        public int WarehouseId { get; set; }

        [JsonPropertyName("warehouseName")]
        public string WarehouseName { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public decimal Value { get; set; }

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }
    }

    public class CategoryValueDto
    {
        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public decimal Value { get; set; }

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }
    }

    // ── Physical Verification ──

    public class PhysicalVerificationLineDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("materialId")]
        public int MaterialId { get; set; }

        [JsonPropertyName("materialCode")]
        public string MaterialCode { get; set; } = string.Empty;

        [JsonPropertyName("materialName")]
        public string MaterialName { get; set; } = string.Empty;

        [JsonPropertyName("unit")]
        public string Unit { get; set; } = "Nos";

        [JsonPropertyName("expectedQuantity")]
        public decimal ExpectedQuantity { get; set; }

        [JsonPropertyName("actualQuantity")]
        public decimal ActualQuantity { get; set; }

        [JsonPropertyName("variance")]
        public decimal Variance { get; set; }

        [JsonPropertyName("remarks")]
        public string Remarks { get; set; } = string.Empty;
    }

    public class PhysicalVerificationListItemDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("verificationNumber")]
        public string VerificationNumber { get; set; } = string.Empty;

        [JsonPropertyName("warehouseName")]
        public string WarehouseName { get; set; } = string.Empty;

        [JsonPropertyName("verifier")]
        public string Verifier { get; set; } = string.Empty;

        [JsonPropertyName("verificationDate")]
        public DateTime VerificationDate { get; set; }

        [JsonPropertyName("expectedQuantity")]
        public decimal ExpectedQuantity { get; set; }

        [JsonPropertyName("actualQuantity")]
        public decimal ActualQuantity { get; set; }

        [JsonPropertyName("variance")]
        public decimal Variance { get; set; }

        [JsonPropertyName("varianceValue")]
        public decimal VarianceValue { get; set; }

        [JsonPropertyName("status")]
        public VerificationStatus Status { get; set; }

        [JsonPropertyName("adjustmentPosted")]
        public bool AdjustmentPosted { get; set; }
    }

    public class PhysicalVerificationDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("verificationNumber")]
        public string VerificationNumber { get; set; } = string.Empty;

        [JsonPropertyName("warehouseId")]
        public int WarehouseId { get; set; }

        [JsonPropertyName("warehouseName")]
        public string WarehouseName { get; set; } = string.Empty;

        [JsonPropertyName("verifier")]
        public string Verifier { get; set; } = string.Empty;

        [JsonPropertyName("verificationDate")]
        public DateTime VerificationDate { get; set; }

        [JsonPropertyName("expectedQuantity")]
        public decimal ExpectedQuantity { get; set; }

        [JsonPropertyName("actualQuantity")]
        public decimal ActualQuantity { get; set; }

        [JsonPropertyName("variance")]
        public decimal Variance { get; set; }

        [JsonPropertyName("varianceValue")]
        public decimal VarianceValue { get; set; }

        [JsonPropertyName("status")]
        public VerificationStatus Status { get; set; }

        [JsonPropertyName("adjustmentPosted")]
        public bool AdjustmentPosted { get; set; }

        [JsonPropertyName("remarks")]
        public string Remarks { get; set; } = string.Empty;

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonPropertyName("approvedBy")]
        public string? ApprovedBy { get; set; }

        [JsonPropertyName("lines")]
        public List<PhysicalVerificationLineDto> Lines { get; set; } = new();

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedBy")]
        public string UpdatedBy { get; set; } = string.Empty;

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("timeline")]
        public List<InventoryTimelineEventDto> Timeline { get; set; } = new();

        [JsonPropertyName("attachments")]
        public List<InventoryAttachmentDto> Attachments { get; set; } = new();
    }

    public class PhysicalVerificationCreateRequestDto
    {
        [JsonPropertyName("warehouseId")]
        public int WarehouseId { get; set; }

        [JsonPropertyName("verifier")]
        public string Verifier { get; set; } = string.Empty;

        [JsonPropertyName("verificationDate")]
        public DateTime VerificationDate { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("remarks")]
        public string? Remarks { get; set; }

        [JsonPropertyName("lines")]
        public List<PhysicalVerificationLineReqDto> Lines { get; set; } = new();
    }

    public class PhysicalVerificationLineReqDto
    {
        [JsonPropertyName("materialId")]
        public int MaterialId { get; set; }

        [JsonPropertyName("expectedQuantity")]
        public decimal ExpectedQuantity { get; set; }

        [JsonPropertyName("actualQuantity")]
        public decimal ActualQuantity { get; set; }

        [JsonPropertyName("remarks")]
        public string? Remarks { get; set; }
    }

    public class PhysicalVerificationStatusUpdateDto
    {
        [JsonPropertyName("status")]
        public VerificationStatus Status { get; set; }

        [JsonPropertyName("remarks")]
        public string? Remarks { get; set; }

        [JsonPropertyName("approvedBy")]
        public string? ApprovedBy { get; set; }
    }

    public class VerificationDashboardDto
    {
        [JsonPropertyName("cards")]
        public List<DashCardDto> Cards { get; set; } = new();

        [JsonPropertyName("pendingVerifications")]
        public int PendingVerifications { get; set; }

        [JsonPropertyName("completed")]
        public int Completed { get; set; }

        [JsonPropertyName("varianceAmount")]
        public decimal VarianceAmount { get; set; }
    }

    public class RawMaterialCreateRequestDto
    {
        [JsonPropertyName("materialCode")]
        public string? MaterialCode { get; set; }

        [JsonPropertyName("materialName")]
        public string MaterialName { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public string Category { get; set; } = "General";

        [JsonPropertyName("warehouseId")]
        public int WarehouseId { get; set; }

        [JsonPropertyName("rack")]
        public string? Rack { get; set; }

        [JsonPropertyName("unit")]
        public string Unit { get; set; } = "Nos";

        [JsonPropertyName("openingStock")]
        public decimal OpeningStock { get; set; }

        [JsonPropertyName("availableStock")]
        public decimal AvailableStock { get; set; }

        [JsonPropertyName("minimumStock")]
        public decimal MinimumStock { get; set; }

        [JsonPropertyName("maximumStock")]
        public decimal MaximumStock { get; set; }

        [JsonPropertyName("reorderLevel")]
        public decimal ReorderLevel { get; set; }

        [JsonPropertyName("unitCost")]
        public decimal UnitCost { get; set; }

        [JsonPropertyName("supplier")]
        public string? Supplier { get; set; }

        [JsonPropertyName("linkedGrnId")]
        public int? LinkedGRNId { get; set; }

        [JsonPropertyName("linkedGrnNumber")]
        public string? LinkedGRNNumber { get; set; }

        [JsonPropertyName("linkedPoId")]
        public int? LinkedPOId { get; set; }

        [JsonPropertyName("linkedPoNumber")]
        public string? LinkedPONumber { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }

    public class FinishedGoodCreateRequestDto
    {
        [JsonPropertyName("productCode")]
        public string? ProductCode { get; set; }

        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("warehouseId")]
        public int WarehouseId { get; set; }

        [JsonPropertyName("finishedQuantity")]
        public decimal FinishedQuantity { get; set; }

        [JsonPropertyName("reservedQuantity")]
        public decimal ReservedQuantity { get; set; }

        [JsonPropertyName("availableQuantity")]
        public decimal AvailableQuantity { get; set; }

        [JsonPropertyName("batchNumber")]
        public string? BatchNumber { get; set; }

        [JsonPropertyName("productionReference")]
        public string? ProductionReference { get; set; }

        [JsonPropertyName("unitPrice")]
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("manufacturingDate")]
        public DateTime? ManufacturingDate { get; set; }

        [JsonPropertyName("expiryDate")]
        public DateTime? ExpiryDate { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }

    public class InventoryBatchCreateRequestDto
    {
        [JsonPropertyName("batchNumber")]
        public string? BatchNumber { get; set; }

        [JsonPropertyName("materialId")]
        public int MaterialId { get; set; }

        [JsonPropertyName("warehouseId")]
        public int WarehouseId { get; set; }

        [JsonPropertyName("supplier")]
        public string? Supplier { get; set; }

        [JsonPropertyName("grnId")]
        public int? GRNId { get; set; }

        [JsonPropertyName("grnNumber")]
        public string? GRNNumber { get; set; }

        [JsonPropertyName("manufacturingDate")]
        public DateTime? ManufacturingDate { get; set; }

        [JsonPropertyName("expiryDate")]
        public DateTime? ExpiryDate { get; set; }

        [JsonPropertyName("availableQuantity")]
        public decimal AvailableQuantity { get; set; }

        [JsonPropertyName("unitCost")]
        public decimal UnitCost { get; set; }

        [JsonPropertyName("unit")]
        public string Unit { get; set; } = "Nos";

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }
}
