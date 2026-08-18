using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ERP.Domain.Procurement;

namespace ERP.Application.Procurement.Dtos
{
    public class PurchaseBillLineDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("itemName")]
        public string ItemName { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("unitPrice")]
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("discountAmount")]
        public decimal DiscountAmount { get; set; }

        [JsonPropertyName("taxPercent")]
        public decimal TaxPercent { get; set; }

        [JsonPropertyName("taxAmount")]
        public decimal TaxAmount { get; set; }

        [JsonPropertyName("totalAmount")]
        public decimal TotalAmount { get; set; }
    }

    public class PurchaseBillHistoryDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public PurchaseBillStatus Status { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("user")]
        public string User { get; set; } = string.Empty;

        [JsonPropertyName("remarks")]
        public string Remarks { get; set; } = string.Empty;
    }

    public class PurchaseBillDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("billNumber")]
        public string BillNumber { get; set; } = string.Empty;

        [JsonPropertyName("invoiceNumber")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [JsonPropertyName("vendorId")]
        public int VendorId { get; set; }

        [JsonPropertyName("vendorName")]
        public string VendorName { get; set; } = string.Empty;

        [JsonPropertyName("purchaseOrderId")]
        public int? PurchaseOrderId { get; set; }

        [JsonPropertyName("purchaseOrderNumber")]
        public string? PurchaseOrderNumber { get; set; }

        [JsonPropertyName("grnId")]
        public int? GRNId { get; set; }

        [JsonPropertyName("grnNumber")]
        public string? GRNNumber { get; set; }

        [JsonPropertyName("invoiceDate")]
        public DateTime InvoiceDate { get; set; }

        [JsonPropertyName("dueDate")]
        public DateTime DueDate { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "INR";

        [JsonPropertyName("subTotal")]
        public decimal SubTotal { get; set; }

        [JsonPropertyName("discountTotal")]
        public decimal DiscountTotal { get; set; }

        [JsonPropertyName("taxTotal")]
        public decimal TaxTotal { get; set; }

        [JsonPropertyName("roundOff")]
        public decimal RoundOff { get; set; }

        [JsonPropertyName("grandTotal")]
        public decimal GrandTotal { get; set; }

        [JsonPropertyName("paidAmount")]
        public decimal PaidAmount { get; set; }

        [JsonPropertyName("balanceAmount")]
        public decimal BalanceAmount { get; set; }

        [JsonPropertyName("paymentTerms")]
        public string PaymentTerms { get; set; } = "Net 30 Days";

        [JsonPropertyName("paymentStatus")]
        public PurchaseBillPaymentStatus PaymentStatus { get; set; }

        [JsonPropertyName("status")]
        public PurchaseBillStatus Status { get; set; }

        [JsonPropertyName("lines")]
        public List<PurchaseBillLineDto> Lines { get; set; } = new();

        [JsonPropertyName("remarks")]
        public string Remarks { get; set; } = string.Empty;

        [JsonPropertyName("history")]
        public List<PurchaseBillHistoryDto> History { get; set; } = new();

        [JsonPropertyName("attachmentsCount")]
        public int AttachmentsCount { get; set; }

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedBy")]
        public string UpdatedBy { get; set; } = string.Empty;

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }

    public class PurchaseBillCreateRequestDto
    {
        [JsonPropertyName("invoiceNumber")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [JsonPropertyName("vendorId")]
        public int VendorId { get; set; }

        [JsonPropertyName("vendorName")]
        public string VendorName { get; set; } = string.Empty;

        [JsonPropertyName("purchaseOrderId")]
        public int? PurchaseOrderId { get; set; }

        [JsonPropertyName("purchaseOrderNumber")]
        public string? PurchaseOrderNumber { get; set; }

        [JsonPropertyName("grnId")]
        public int? GRNId { get; set; }

        [JsonPropertyName("grnNumber")]
        public string? GRNNumber { get; set; }

        [JsonPropertyName("invoiceDate")]
        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("dueDate")]
        public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(30);

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("paymentTerms")]
        public string? PaymentTerms { get; set; }

        [JsonPropertyName("remarks")]
        public string? Remarks { get; set; }

        [JsonPropertyName("lines")]
        public List<PurchaseBillLineReqDto> Lines { get; set; } = new();
    }

    public class PurchaseBillLineReqDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("itemName")]
        public string ItemName { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("unitPrice")]
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("discountAmount")]
        public decimal DiscountAmount { get; set; }

        [JsonPropertyName("taxPercent")]
        public decimal TaxPercent { get; set; }
    }

    public class PurchaseBillDashboardDto
    {
        [JsonPropertyName("totalBills")]
        public int TotalBills { get; set; }

        [JsonPropertyName("draftCount")]
        public int DraftCount { get; set; }

        [JsonPropertyName("approvedCount")]
        public int ApprovedCount { get; set; }

        [JsonPropertyName("postedCount")]
        public int PostedCount { get; set; }

        [JsonPropertyName("paidCount")]
        public int PaidCount { get; set; }

        [JsonPropertyName("totalBilledAmount")]
        public decimal TotalBilledAmount { get; set; }

        [JsonPropertyName("totalOutstandingAmount")]
        public decimal TotalOutstandingAmount { get; set; }
    }

    public class PurchaseBillFilterQueryDto
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? PaymentStatus { get; set; }
        public string? VendorName { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
