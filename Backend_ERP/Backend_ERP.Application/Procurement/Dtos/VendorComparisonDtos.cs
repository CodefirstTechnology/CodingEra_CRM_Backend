using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ERP.Domain.Procurement;

namespace ERP.Application.Procurement.Dtos
{
    public class VendorQuotationEntryDto
    {
        public int Id { get; set; }

        public int VendorComparisonId { get; set; }

        [JsonPropertyName("vendorId")]
        public int VendorId { get; set; }

        [JsonPropertyName("vendorName")]
        public string VendorName { get; set; } = string.Empty;

        [JsonPropertyName("quotationRef")]
        public string QuotationRef { get; set; } = string.Empty;

        [JsonPropertyName("vendorQuotationId")]
        public int? VendorQuotationId { get; set; }

        [JsonPropertyName("unitPrice")]
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("taxPercent")]
        public decimal TaxPercent { get; set; }

        [JsonPropertyName("discountPercent")]
        public decimal DiscountPercent { get; set; }

        [JsonPropertyName("deliveryTimeDays")]
        public int DeliveryTimeDays { get; set; }

        [JsonPropertyName("leadTime")]
        public string LeadTime { get; set; } = string.Empty;

        [JsonPropertyName("warrantyPeriod")]
        public string WarrantyPeriod { get; set; } = string.Empty;

        [JsonPropertyName("paymentTerms")]
        public string PaymentTerms { get; set; } = string.Empty;

        [JsonPropertyName("totalCost")]
        public decimal TotalCost { get; set; }

        [JsonPropertyName("ranking")]
        public int Ranking { get; set; }

        [JsonPropertyName("isLowestPrice")]
        public bool IsLowestPrice { get; set; }

        [JsonPropertyName("isBestDelivery")]
        public bool IsBestDelivery { get; set; }

        [JsonPropertyName("recommendationScore")]
        public double RecommendationScore { get; set; }

        [JsonPropertyName("remarks")]
        public string Remarks { get; set; } = string.Empty;

        [JsonPropertyName("lines")]
        public List<VendorQuotationLineDto> Lines { get; set; } = new();
    }

    public class VendorComparisonStatusHistoryDto
    {
        public int Id { get; set; }

        public int VendorComparisonId { get; set; }

        public VendorComparisonStatus Status { get; set; }

        public VendorComparisonStatus? PreviousStatus { get; set; }

        public string User { get; set; } = string.Empty;

        public string Remarks { get; set; } = string.Empty;

        public DateTime Date { get; set; }
    }

    public class VendorComparisonListItemDto
    {
        public int Id { get; set; }

        [JsonPropertyName("comparisonNumber")]
        public string ComparisonNumber { get; set; } = string.Empty;

        [JsonPropertyName("rfqId")]
        public int? RFQId { get; set; }

        [JsonPropertyName("rfqNumber")]
        public string RFQNumber { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("comparisonDate")]
        public DateTime ComparisonDate { get; set; }

        public VendorComparisonStatus Status { get; set; }

        [JsonPropertyName("recommendedVendorId")]
        public int RecommendedVendorId { get; set; }

        [JsonPropertyName("selectedWinnerVendorId")]
        public int? SelectedWinnerVendorId { get; set; }

        [JsonPropertyName("selectedWinnerVendorName")]
        public string SelectedWinnerVendorName { get; set; } = string.Empty;

        [JsonPropertyName("purchaseOrderId")]
        public int? PurchaseOrderId { get; set; }

        [JsonPropertyName("purchaseOrderNumber")]
        public string PurchaseOrderNumber { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
    }

    public class VendorComparisonDto
    {
        public int Id { get; set; }

        [JsonPropertyName("comparisonNumber")]
        public string ComparisonNumber { get; set; } = string.Empty;

        [JsonPropertyName("rfqId")]
        public int? RFQId { get; set; }

        [JsonPropertyName("rfqNumber")]
        public string RFQNumber { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("comparisonDate")]
        public DateTime ComparisonDate { get; set; }

        [JsonPropertyName("recommendationNotes")]
        public string RecommendationNotes { get; set; } = string.Empty;

        [JsonPropertyName("recommendedVendorId")]
        public int RecommendedVendorId { get; set; }

        [JsonPropertyName("selectedWinnerVendorId")]
        public int? SelectedWinnerVendorId { get; set; }

        [JsonPropertyName("selectedWinnerVendorName")]
        public string SelectedWinnerVendorName { get; set; } = string.Empty;

        [JsonPropertyName("purchaseOrderId")]
        public int? PurchaseOrderId { get; set; }

        [JsonPropertyName("purchaseOrderNumber")]
        public string PurchaseOrderNumber { get; set; } = string.Empty;

        public VendorComparisonStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; }

        public string UpdatedBy { get; set; } = string.Empty;

        [JsonPropertyName("vendors")]
        public List<VendorQuotationEntryDto> Vendors { get; set; } = new();

        [JsonPropertyName("history")]
        public List<VendorComparisonStatusHistoryDto> History { get; set; } = new();
    }

    public class VendorComparisonListQueryDto
    {
        public string? Search { get; set; }

        public string? Status { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public string? SortBy { get; set; }

        public bool SortDescending { get; set; }
    }

    public class VendorComparisonCreateRequestDto
    {
        public int? RFQId { get; set; }

        public string Title { get; set; } = string.Empty;

        public DateTime ComparisonDate { get; set; } = DateTime.UtcNow;

        public string RecommendationNotes { get; set; } = string.Empty;

        public int? RecommendedVendorId { get; set; }

        public int? SelectedWinnerVendorId { get; set; }

        public string SelectedWinnerVendorName { get; set; } = string.Empty;

        public List<VendorQuotationEntryDto> Vendors { get; set; } = new();
    }

    public class VendorComparisonUpdateRequestDto
    {
        public string Title { get; set; } = string.Empty;

        public DateTime ComparisonDate { get; set; }

        public string RecommendationNotes { get; set; } = string.Empty;

        public int RecommendedVendorId { get; set; }

        public int? SelectedWinnerVendorId { get; set; }

        public string SelectedWinnerVendorName { get; set; } = string.Empty;

        public List<VendorQuotationEntryDto> Vendors { get; set; } = new();
    }

    public class VendorAwardRequestDto
    {
        public int VendorId { get; set; }

        public string Remarks { get; set; } = string.Empty;

        public int? PurchaseOrderId { get; set; }

        public string PurchaseOrderNumber { get; set; } = string.Empty;
    }

    public class VendorComparisonStatusUpdateRequestDto
    {
        public VendorComparisonStatus TargetStatus { get; set; }

        public string Remarks { get; set; } = string.Empty;
    }
}
