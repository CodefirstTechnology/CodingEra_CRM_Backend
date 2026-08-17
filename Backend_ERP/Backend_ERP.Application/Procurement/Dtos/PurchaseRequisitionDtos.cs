using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ERP.Domain.Procurement;

namespace ERP.Application.Procurement.Dtos
{
    public class PurchaseRequisitionLineDto
    {
        public int Id { get; set; }

        public int PurchaseRequisitionId { get; set; }

        [JsonPropertyName("itemName")]
        public string ItemName { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("uom")]
        public string Uom { get; set; } = "PCS";

        [JsonPropertyName("estimatedPrice")]
        public decimal EstimatedPrice { get; set; }

        [JsonPropertyName("totalAmount")]
        public decimal TotalAmount { get; set; }
    }

    public class PurchaseRequisitionStatusHistoryDto
    {
        public int Id { get; set; }

        public int PurchaseRequisitionId { get; set; }

        public PurchaseRequisitionStatus Status { get; set; }

        public PurchaseRequisitionStatus? PreviousStatus { get; set; }

        public string User { get; set; } = string.Empty;

        public string Remarks { get; set; } = string.Empty;

        public DateTime Date { get; set; }
    }

    public class PurchaseRequisitionListItemDto
    {
        public int Id { get; set; }

        [JsonPropertyName("prNumber")]
        public string PRNumber { get; set; } = string.Empty;

        [JsonPropertyName("department")]
        public string Department { get; set; } = string.Empty;

        [JsonPropertyName("requestor")]
        public string Requestor { get; set; } = string.Empty;

        [JsonPropertyName("requiredDate")]
        public DateTime RequiredDate { get; set; }

        public PurchaseRequisitionPriority Priority { get; set; }

        public PurchaseRequisitionStatus Status { get; set; }

        [JsonPropertyName("totalEstimatedAmount")]
        public decimal TotalEstimatedAmount { get; set; }

        [JsonPropertyName("rfqId")]
        public int? RFQId { get; set; }

        [JsonPropertyName("rfqNumber")]
        public string RFQNumber { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
    }

    public class PurchaseRequisitionDto
    {
        public int Id { get; set; }

        [JsonPropertyName("prNumber")]
        public string PRNumber { get; set; } = string.Empty;

        [JsonPropertyName("department")]
        public string Department { get; set; } = string.Empty;

        [JsonPropertyName("requestor")]
        public string Requestor { get; set; } = string.Empty;

        [JsonPropertyName("requiredDate")]
        public DateTime RequiredDate { get; set; }

        public PurchaseRequisitionPriority Priority { get; set; }

        public PurchaseRequisitionStatus Status { get; set; }

        public string Remarks { get; set; } = string.Empty;

        public string InternalNotes { get; set; } = string.Empty;

        [JsonPropertyName("totalEstimatedAmount")]
        public decimal TotalEstimatedAmount { get; set; }

        [JsonPropertyName("rfqId")]
        public int? RFQId { get; set; }

        [JsonPropertyName("rfqNumber")]
        public string RFQNumber { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; }

        public string UpdatedBy { get; set; } = string.Empty;

        public List<PurchaseRequisitionLineDto> Lines { get; set; } = new();

        public List<PurchaseRequisitionStatusHistoryDto> History { get; set; } = new();
    }

    public class PurchaseRequisitionListQueryDto
    {
        public string? Search { get; set; }

        public string? Status { get; set; }

        public string? Department { get; set; }

        public string? Priority { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public string? SortBy { get; set; }

        public bool SortDescending { get; set; }
    }

    public class PurchaseRequisitionCreateRequestDto
    {
        public string Department { get; set; } = string.Empty;

        public string Requestor { get; set; } = string.Empty;

        public DateTime RequiredDate { get; set; } = DateTime.UtcNow.AddDays(14);

        public PurchaseRequisitionPriority Priority { get; set; } = PurchaseRequisitionPriority.Normal;

        public string Remarks { get; set; } = string.Empty;

        public string InternalNotes { get; set; } = string.Empty;

        public List<PurchaseRequisitionLineDto> Lines { get; set; } = new();
    }

    public class PurchaseRequisitionUpdateRequestDto
    {
        public string Department { get; set; } = string.Empty;

        public string Requestor { get; set; } = string.Empty;

        public DateTime RequiredDate { get; set; }

        public PurchaseRequisitionPriority Priority { get; set; }

        public string Remarks { get; set; } = string.Empty;

        public string InternalNotes { get; set; } = string.Empty;

        public List<PurchaseRequisitionLineDto> Lines { get; set; } = new();
    }

    public class PurchaseRequisitionStatusUpdateRequestDto
    {
        public PurchaseRequisitionStatus TargetStatus { get; set; }

        public string Remarks { get; set; } = string.Empty;
    }
}
