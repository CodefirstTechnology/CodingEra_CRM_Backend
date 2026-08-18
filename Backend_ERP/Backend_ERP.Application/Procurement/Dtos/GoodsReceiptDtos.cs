using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ERP.Domain.Procurement;

namespace ERP.Application.Procurement.Dtos
{
    public class GoodsReceiptItemDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("purchaseOrderLineId")]
        public string PurchaseOrderLineId { get; set; } = string.Empty;

        [JsonPropertyName("itemName")]
        public string ItemName { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("unit")]
        public string Unit { get; set; } = "Nos";

        [JsonPropertyName("orderedQuantity")]
        public decimal OrderedQuantity { get; set; }

        [JsonPropertyName("previouslyReceivedQuantity")]
        public decimal PreviouslyReceivedQuantity { get; set; }

        [JsonPropertyName("remainingQuantity")]
        public decimal RemainingQuantity { get; set; }

        [JsonPropertyName("receivedQuantity")]
        public decimal ReceivedQuantity { get; set; }

        [JsonPropertyName("rejectedQuantity")]
        public decimal RejectedQuantity { get; set; }

        [JsonPropertyName("remarks")]
        public string Remarks { get; set; } = string.Empty;
    }

    public class GoodsReceiptSummaryDto
    {
        [JsonPropertyName("orderedQuantity")]
        public decimal OrderedQuantity { get; set; }

        [JsonPropertyName("receivedQuantity")]
        public decimal ReceivedQuantity { get; set; }

        [JsonPropertyName("remainingQuantity")]
        public decimal RemainingQuantity { get; set; }

        [JsonPropertyName("rejectedQuantity")]
        public decimal RejectedQuantity { get; set; }

        [JsonPropertyName("receiptPercentage")]
        public decimal ReceiptPercentage { get; set; }
    }

    public class GoodsReceiptHistoryDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public GoodsReceiptStatus Status { get; set; }

        [JsonPropertyName("previousStatus")]
        public GoodsReceiptStatus? PreviousStatus { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("user")]
        public string User { get; set; } = string.Empty;

        [JsonPropertyName("remarks")]
        public string Remarks { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;
    }

    public class GoodsReceiptListItemDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("grnNumber")]
        public string GRNNumber { get; set; } = string.Empty;

        [JsonPropertyName("purchaseOrderNumber")]
        public string PurchaseOrderNumber { get; set; } = string.Empty;

        [JsonPropertyName("purchaseOrderId")]
        public int PurchaseOrderId { get; set; }

        [JsonPropertyName("vendorName")]
        public string VendorName { get; set; } = string.Empty;

        [JsonPropertyName("receiptDate")]
        public DateTime ReceiptDate { get; set; }

        [JsonPropertyName("status")]
        public GoodsReceiptStatus Status { get; set; }

        [JsonPropertyName("receivedPercent")]
        public decimal ReceivedPercent { get; set; }

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class GoodsReceiptDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("grnNumber")]
        public string GRNNumber { get; set; } = string.Empty;

        [JsonPropertyName("purchaseOrderId")]
        public int PurchaseOrderId { get; set; }

        [JsonPropertyName("purchaseOrderNumber")]
        public string PurchaseOrderNumber { get; set; } = string.Empty;

        [JsonPropertyName("vendorName")]
        public string VendorName { get; set; } = string.Empty;

        [JsonPropertyName("receiptDate")]
        public DateTime ReceiptDate { get; set; }

        [JsonPropertyName("warehouse")]
        public string Warehouse { get; set; } = "Main Store — Sanand";

        [JsonPropertyName("status")]
        public GoodsReceiptStatus Status { get; set; }

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

        [JsonPropertyName("items")]
        public List<GoodsReceiptItemDto> Items { get; set; } = new();

        [JsonPropertyName("summary")]
        public GoodsReceiptSummaryDto Summary { get; set; } = new();

        [JsonPropertyName("history")]
        public List<GoodsReceiptHistoryDto> History { get; set; } = new();
    }

    public class GoodsReceiptCreateRequestDto
    {
        [JsonPropertyName("purchaseOrderId")]
        public int PurchaseOrderId { get; set; }

        [JsonPropertyName("receiptDate")]
        public DateTime ReceiptDate { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("warehouse")]
        public string Warehouse { get; set; } = "Main Store — Sanand";

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("remarks")]
        public string? Remarks { get; set; }

        [JsonPropertyName("status")]
        public GoodsReceiptStatus Status { get; set; } = GoodsReceiptStatus.Draft;

        [JsonPropertyName("items")]
        public List<GoodsReceiptItemDto> Items { get; set; } = new();
    }

    public class GoodsReceiptUpdateRequestDto
    {
        [JsonPropertyName("receiptDate")]
        public DateTime ReceiptDate { get; set; }

        [JsonPropertyName("warehouse")]
        public string Warehouse { get; set; } = string.Empty;

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("remarks")]
        public string? Remarks { get; set; }

        [JsonPropertyName("items")]
        public List<GoodsReceiptItemDto> Items { get; set; } = new();
    }

    public class GoodsReceiptStatusUpdateRequestDto
    {
        [JsonPropertyName("status")]
        public GoodsReceiptStatus Status { get; set; }

        [JsonPropertyName("remarks")]
        public string? Remarks { get; set; }
    }

    public class GoodsReceiptListQueryDto
    {
        public string? Search { get; set; }

        public string? Status { get; set; }

        public DateTime? DateFrom { get; set; }

        public DateTime? DateTo { get; set; }

        public int? PurchaseOrderId { get; set; }

        public string? VendorName { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }
}
