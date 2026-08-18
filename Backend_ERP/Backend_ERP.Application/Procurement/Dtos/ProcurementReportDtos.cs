using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ERP.Application.Procurement.Dtos
{
    public class ProcurementReportKpisDto
    {
        [JsonPropertyName("totalPurchaseOrders")]
        public int TotalPurchaseOrders { get; set; }

        [JsonPropertyName("pendingApprovals")]
        public int PendingApprovals { get; set; }

        [JsonPropertyName("completedPurchaseOrders")]
        public int CompletedPurchaseOrders { get; set; }

        [JsonPropertyName("openPurchaseOrders")]
        public int OpenPurchaseOrders { get; set; }

        [JsonPropertyName("totalGrns")]
        public int TotalGrns { get; set; }

        [JsonPropertyName("completedGrns")]
        public int CompletedGrns { get; set; }

        [JsonPropertyName("purchaseValue")]
        public decimal PurchaseValue { get; set; }

        [JsonPropertyName("goodsReceivedPercent")]
        public decimal GoodsReceivedPercent { get; set; }

        [JsonPropertyName("averageApprovalTimeHours")]
        public double AverageApprovalTimeHours { get; set; }

        [JsonPropertyName("averageReceiptTimeHours")]
        public double AverageReceiptTimeHours { get; set; }
    }

    public class ErpChartPointDto
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public decimal Value { get; set; }

        [JsonPropertyName("color")]
        public string? Color { get; set; }

        [JsonPropertyName("formattedValue")]
        public string? FormattedValue { get; set; }
    }

    public class ProcurementChartsDto
    {
        [JsonPropertyName("monthlyTrend")]
        public List<ErpChartPointDto> MonthlyTrend { get; set; } = new();

        [JsonPropertyName("statusDistribution")]
        public List<ErpChartPointDto> StatusDistribution { get; set; } = new();

        [JsonPropertyName("vendorDistribution")]
        public List<ErpChartPointDto> VendorDistribution { get; set; } = new();

        [JsonPropertyName("approvalStatistics")]
        public List<ErpChartPointDto> ApprovalStatistics { get; set; } = new();

        [JsonPropertyName("receiptCompletionPercent")]
        public decimal ReceiptCompletionPercent { get; set; }
    }

    public class PurchaseOrderReportRowDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("purchaseOrderNumber")]
        public string PurchaseOrderNumber { get; set; } = string.Empty;

        [JsonPropertyName("vendorName")]
        public string VendorName { get; set; } = string.Empty;

        [JsonPropertyName("orderDate")]
        public string OrderDate { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("approvalStatus")]
        public string ApprovalStatus { get; set; } = string.Empty;

        [JsonPropertyName("totalAmount")]
        public decimal TotalAmount { get; set; }

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("expectedDeliveryDate")]
        public string? ExpectedDeliveryDate { get; set; }
    }

    public class GoodsReceiptReportRowDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("grnNumber")]
        public string GrnNumber { get; set; } = string.Empty;

        [JsonPropertyName("purchaseOrderNumber")]
        public string PurchaseOrderNumber { get; set; } = string.Empty;

        [JsonPropertyName("vendorName")]
        public string VendorName { get; set; } = string.Empty;

        [JsonPropertyName("receiptDate")]
        public string ReceiptDate { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("receivedPercent")]
        public decimal ReceivedPercent { get; set; }

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class VendorPurchaseSummaryRowDto
    {
        [JsonPropertyName("vendorName")]
        public string VendorName { get; set; } = string.Empty;

        [JsonPropertyName("purchaseOrderCount")]
        public int PurchaseOrderCount { get; set; }

        [JsonPropertyName("totalValue")]
        public decimal TotalValue { get; set; }

        [JsonPropertyName("completedCount")]
        public int CompletedCount { get; set; }

        [JsonPropertyName("openCount")]
        public int OpenCount { get; set; }

        [JsonPropertyName("grnCount")]
        public int GrnCount { get; set; }
    }

    public class StatusSummaryRowDto
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("totalValue")]
        public decimal TotalValue { get; set; }

        [JsonPropertyName("percent")]
        public decimal Percent { get; set; }
    }

    public class MonthlyTrendRowDto
    {
        [JsonPropertyName("monthKey")]
        public string MonthKey { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("purchaseOrderCount")]
        public int PurchaseOrderCount { get; set; }

        [JsonPropertyName("purchaseValue")]
        public decimal PurchaseValue { get; set; }

        [JsonPropertyName("grnCount")]
        public int GrnCount { get; set; }
    }

    public class TopVendorRowDto
    {
        [JsonPropertyName("vendorName")]
        public string VendorName { get; set; } = string.Empty;

        [JsonPropertyName("totalValue")]
        public decimal TotalValue { get; set; }

        [JsonPropertyName("purchaseOrderCount")]
        public int PurchaseOrderCount { get; set; }
    }

    public class ProcurementReportFilterQueryDto
    {
        public string? Search { get; set; }
        public string? VendorName { get; set; }
        public string? Status { get; set; }
        public string? DateFrom { get; set; }
        public string? DateTo { get; set; }
        public string? PurchaseOrderNumber { get; set; }
        public string? GoodsReceiptNumber { get; set; }
        public string? CreatedBy { get; set; }
        public string? ApprovalStatus { get; set; }
    }
}
