using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ERP.Application.Procurement.Dtos
{
    public class InventoryTransactionDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("itemName")]
        public string ItemName { get; set; } = string.Empty;

        [JsonPropertyName("quantityReceived")]
        public decimal QuantityReceived { get; set; }

        [JsonPropertyName("receiptDate")]
        public string ReceiptDate { get; set; } = string.Empty;

        [JsonPropertyName("purchaseOrderNumber")]
        public string PurchaseOrderNumber { get; set; } = string.Empty;

        [JsonPropertyName("goodsReceiptNumber")]
        public string GoodsReceiptNumber { get; set; } = string.Empty;

        [JsonPropertyName("goodsReceiptId")]
        public int GoodsReceiptId { get; set; }

        [JsonPropertyName("warehouse")]
        public string Warehouse { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public string CreatedAt { get; set; } = string.Empty;
    }

    public class RecordInventoryTransactionsRequestDto
    {
        [JsonPropertyName("transactions")]
        public List<InventoryTransactionDto> Transactions { get; set; } = new();
    }
}

