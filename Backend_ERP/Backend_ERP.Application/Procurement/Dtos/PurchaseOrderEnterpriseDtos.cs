using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ERP.Domain.Procurement;

namespace ERP.Application.Procurement.Dtos
{
    public class DeliveryScheduleItemDto
    {
        [JsonPropertyName("scheduleNumber")]
        public int ScheduleNumber { get; set; }

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("expectedDeliveryDate")]
        public DateTime ExpectedDeliveryDate { get; set; }

        [JsonPropertyName("deliveryLocation")]
        public string? DeliveryLocation { get; set; }

        [JsonPropertyName("remarks")]
        public string? Remarks { get; set; }
    }

    public class DirectPOCreateRequestDto
    {
        [JsonPropertyName("vendorId")]
        public int VendorId { get; set; }

        [JsonPropertyName("vendorCode")]
        public string VendorCode { get; set; } = string.Empty;

        [JsonPropertyName("vendorName")]
        public string VendorName { get; set; } = string.Empty;

        [JsonPropertyName("vendorGstin")]
        public string VendorGstin { get; set; } = string.Empty;

        [JsonPropertyName("vendorStateCode")]
        public string VendorStateCode { get; set; } = string.Empty;

        [JsonPropertyName("vendorPan")]
        public string VendorPan { get; set; } = string.Empty;

        [JsonPropertyName("orderDate")]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("expectedDeliveryDate")]
        public DateTime? ExpectedDeliveryDate { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "INR";

        [JsonPropertyName("exchangeRate")]
        public decimal ExchangeRate { get; set; } = 1.000000m;

        [JsonPropertyName("paymentTermsDays")]
        public int PaymentTermsDays { get; set; } = 30;

        [JsonPropertyName("paymentTerms")]
        public string PaymentTerms { get; set; } = "Net 30";

        [JsonPropertyName("incoterms")]
        public string Incoterms { get; set; } = "FOR Destination";

        [JsonPropertyName("deliveryTerms")]
        public string DeliveryTerms { get; set; } = "Door Delivery";

        [JsonPropertyName("priority")]
        public PurchaseOrderPriority Priority { get; set; } = PurchaseOrderPriority.Normal;

        [JsonPropertyName("buyerName")]
        public string? BuyerName { get; set; }

        [JsonPropertyName("billingAddress")]
        public string? BillingAddress { get; set; }

        [JsonPropertyName("shippingAddress")]
        public string? ShippingAddress { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("remarks")]
        public string? Remarks { get; set; }

        [JsonPropertyName("isUnbudgetedCapEx")]
        public bool IsUnbudgetedCapEx { get; set; } = false;

        [JsonPropertyName("isNonAvlEmergencyOrder")]
        public bool IsNonAvlEmergencyOrder { get; set; } = false;

        [JsonPropertyName("lines")]
        public List<EnterprisePOLineInputDto> Lines { get; set; } = new();
    }

    public class EnterprisePOLineInputDto
    {
        [JsonPropertyName("lineNumber")]
        public int LineNumber { get; set; }

        [JsonPropertyName("itemType")]
        public POItemType ItemType { get; set; } = POItemType.Goods;

        [JsonPropertyName("itemCode")]
        public string ItemCode { get; set; } = string.Empty;

        [JsonPropertyName("itemName")]
        public string ItemName { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("hsnCode")]
        public string HsnCode { get; set; } = string.Empty;

        [JsonPropertyName("unit")]
        public string Unit { get; set; } = "Nos";

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("rate")]
        public decimal Rate { get; set; }

        [JsonPropertyName("discountPercent")]
        public decimal DiscountPercent { get; set; }

        [JsonPropertyName("gstRatePercentage")]
        public decimal GstRatePercentage { get; set; } = 18m;

        [JsonPropertyName("overDeliveryTolerancePercent")]
        public decimal OverDeliveryTolerancePercent { get; set; } = 0m;

        [JsonPropertyName("expectedDeliveryDate")]
        public DateTime? ExpectedDeliveryDate { get; set; }

        [JsonPropertyName("targetWarehouseId")]
        public int? TargetWarehouseId { get; set; }

        [JsonPropertyName("purchaseRequisitionLineId")]
        public int? PurchaseRequisitionLineId { get; set; }

        [JsonPropertyName("rfqLineId")]
        public int? RFQLineId { get; set; }

        [JsonPropertyName("vendorQuotationLineId")]
        public int? VendorQuotationLineId { get; set; }

        [JsonPropertyName("deliverySchedules")]
        public List<DeliveryScheduleItemDto> DeliverySchedules { get; set; } = new();
    }

    public class POAmendmentRequestDto
    {
        [JsonPropertyName("amendmentReason")]
        public string AmendmentReason { get; set; } = string.Empty;

        [JsonPropertyName("amendmentType")]
        public POAmendmentType AmendmentType { get; set; } = POAmendmentType.Commercial;

        [JsonPropertyName("paymentTermsDays")]
        public int? PaymentTermsDays { get; set; }

        [JsonPropertyName("paymentTerms")]
        public string? PaymentTerms { get; set; }

        [JsonPropertyName("deliveryTerms")]
        public string? DeliveryTerms { get; set; }

        [JsonPropertyName("incoterms")]
        public string? Incoterms { get; set; }

        [JsonPropertyName("exchangeRate")]
        public decimal? ExchangeRate { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("remarks")]
        public string? Remarks { get; set; }

        [JsonPropertyName("lines")]
        public List<EnterprisePOLineInputDto> Lines { get; set; } = new();
    }

    public class POApprovalSignOffRequestDto
    {
        [JsonPropertyName("decision")]
        public PurchaseOrderApprovalDecision Decision { get; set; }

        [JsonPropertyName("comments")]
        public string? Comments { get; set; }

        [JsonPropertyName("approverUserId")]
        public string ApproverUserId { get; set; } = string.Empty;

        [JsonPropertyName("approverName")]
        public string ApproverName { get; set; } = string.Empty;

        [JsonPropertyName("approverRole")]
        public string ApproverRole { get; set; } = string.Empty;
    }

    public class POApprovalSignOffResultDto
    {
        [JsonPropertyName("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonPropertyName("purchaseOrderId")]
        public int PurchaseOrderId { get; set; }

        [JsonPropertyName("purchaseOrderNumber")]
        public string PurchaseOrderNumber { get; set; } = string.Empty;

        [JsonPropertyName("newStatus")]
        public PurchaseOrderStatus NewStatus { get; set; }

        [JsonPropertyName("currentTier")]
        public PurchaseOrderApprovalTier CurrentTier { get; set; }

        [JsonPropertyName("requiredTier")]
        public PurchaseOrderApprovalTier RequiredTier { get; set; }

        [JsonPropertyName("hmacSignatureToken")]
        public string? HmacSignatureToken { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }

    public class PurchaseOrderRevisionSummaryDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("purchaseOrderNumber")]
        public string PurchaseOrderNumber { get; set; } = string.Empty;

        [JsonPropertyName("revisionNumber")]
        public int RevisionNumber { get; set; }

        [JsonPropertyName("isCurrentRevision")]
        public bool IsCurrentRevision { get; set; }

        [JsonPropertyName("status")]
        public PurchaseOrderStatus Status { get; set; }

        [JsonPropertyName("totalAmount")]
        public decimal TotalAmount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "INR";

        [JsonPropertyName("amendmentReason")]
        public string? AmendmentReason { get; set; }

        [JsonPropertyName("amendmentType")]
        public string? AmendmentType { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("supersededAt")]
        public DateTime? SupersededAt { get; set; }

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class PurchaseOrderAmendmentDeltaDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("purchaseOrderId")]
        public int PurchaseOrderId { get; set; }

        [JsonPropertyName("revisionNumber")]
        public int RevisionNumber { get; set; }

        [JsonPropertyName("changedField")]
        public string ChangedField { get; set; } = string.Empty;

        [JsonPropertyName("oldValue")]
        public string? OldValue { get; set; }

        [JsonPropertyName("newValue")]
        public string? NewValue { get; set; }

        [JsonPropertyName("amendmentReason")]
        public string AmendmentReason { get; set; } = string.Empty;

        [JsonPropertyName("actionedByUserId")]
        public string ActionedByUserId { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
    }
}

