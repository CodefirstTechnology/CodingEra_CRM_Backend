using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ERP.Domain.Procurement;

namespace ERP.Application.Procurement.Dtos
{
    public class RFQLineDto
    {
        public int Id { get; set; }

        public int RequestForQuotationId { get; set; }

        [JsonPropertyName("itemName")]
        public string ItemName { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("uom")]
        public string Uom { get; set; } = "PCS";

        [JsonPropertyName("targetUnitPrice")]
        public decimal? TargetUnitPrice { get; set; }
    }

    public class RFQVendorSelectionDto
    {
        public int Id { get; set; }

        public int RequestForQuotationId { get; set; }

        public int VendorId { get; set; }

        [JsonPropertyName("vendorName")]
        public string VendorName { get; set; } = string.Empty;

        [JsonPropertyName("contactEmail")]
        public string ContactEmail { get; set; } = string.Empty;

        [JsonPropertyName("contactPhone")]
        public string ContactPhone { get; set; } = string.Empty;

        public RFQVendorStatus Status { get; set; }

        [JsonPropertyName("respondedDate")]
        public DateTime? RespondedDate { get; set; }
    }

    public class RFQStatusHistoryDto
    {
        public int Id { get; set; }

        public int RequestForQuotationId { get; set; }

        public RFQStatus Status { get; set; }

        public RFQStatus? PreviousStatus { get; set; }

        public string User { get; set; } = string.Empty;

        public string Remarks { get; set; } = string.Empty;

        public DateTime Date { get; set; }
    }

    public class RFQListItemDto
    {
        public int Id { get; set; }

        [JsonPropertyName("rfqNumber")]
        public string RFQNumber { get; set; } = string.Empty;

        [JsonPropertyName("rfqDate")]
        public DateTime RFQDate { get; set; }

        [JsonPropertyName("dueDate")]
        public DateTime DueDate { get; set; }

        [JsonPropertyName("purchaseRequisitionId")]
        public int? PurchaseRequisitionId { get; set; }

        [JsonPropertyName("purchaseRequisitionNumber")]
        public string PurchaseRequisitionNumber { get; set; } = string.Empty;

        public RFQStatus Status { get; set; }

        public int VendorCount { get; set; }

        public int LineCount { get; set; }

        public DateTime CreatedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
    }

    public class RFQDto
    {
        public int Id { get; set; }

        [JsonPropertyName("rfqNumber")]
        public string RFQNumber { get; set; } = string.Empty;

        [JsonPropertyName("rfqDate")]
        public DateTime RFQDate { get; set; }

        [JsonPropertyName("dueDate")]
        public DateTime DueDate { get; set; }

        [JsonPropertyName("purchaseRequisitionId")]
        public int? PurchaseRequisitionId { get; set; }

        [JsonPropertyName("purchaseRequisitionNumber")]
        public string PurchaseRequisitionNumber { get; set; } = string.Empty;

        public RFQStatus Status { get; set; }

        public string DeliveryTerms { get; set; } = string.Empty;

        public string PaymentTerms { get; set; } = string.Empty;

        public string VendorNotes { get; set; } = string.Empty;

        public int? ComparisonId { get; set; }

        public DateTime CreatedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; }

        public string UpdatedBy { get; set; } = string.Empty;

        public List<RFQLineDto> Lines { get; set; } = new();

        public List<RFQVendorSelectionDto> Vendors { get; set; } = new();

        public List<RFQStatusHistoryDto> History { get; set; } = new();
    }

    public class RFQListQueryDto
    {
        public string? Search { get; set; }

        public string? Status { get; set; }

        public string? PRNumber { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public string? SortBy { get; set; }

        public bool SortDescending { get; set; }
    }

    public class RFQCreateRequestDto
    {
        public DateTime RFQDate { get; set; } = DateTime.UtcNow;

        public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(7);

        public int? PurchaseRequisitionId { get; set; }

        public string DeliveryTerms { get; set; } = string.Empty;

        public string PaymentTerms { get; set; } = string.Empty;

        public string VendorNotes { get; set; } = string.Empty;

        public List<RFQLineDto> Lines { get; set; } = new();

        public List<int> VendorIds { get; set; } = new();
    }

    public class RFQUpdateRequestDto
    {
        public DateTime RFQDate { get; set; }

        public DateTime DueDate { get; set; }

        public string DeliveryTerms { get; set; } = string.Empty;

        public string PaymentTerms { get; set; } = string.Empty;

        public string VendorNotes { get; set; } = string.Empty;

        public List<RFQLineDto> Lines { get; set; } = new();

        public List<int> VendorIds { get; set; } = new();
    }

    public class RFQStatusUpdateRequestDto
    {
        public RFQStatus TargetStatus { get; set; }

        public string Remarks { get; set; } = string.Empty;
    }
}
