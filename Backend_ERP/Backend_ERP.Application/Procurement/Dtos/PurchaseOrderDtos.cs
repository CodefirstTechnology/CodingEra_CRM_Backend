using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ERP.Domain.Procurement;

namespace ERP.Application.Procurement.Dtos
{
    public class PurchaseOrderVendorDto
    {
        [JsonPropertyName("vendorName")]
        public string VendorName { get; set; } = string.Empty;

        [JsonPropertyName("vendorCode")]
        public string VendorCode { get; set; } = string.Empty;

        [JsonPropertyName("vendorContact")]
        public string VendorContact { get; set; } = string.Empty;

        [JsonPropertyName("billingAddress")]
        public string BillingAddress { get; set; } = string.Empty;

        [JsonPropertyName("shippingAddress")]
        public string ShippingAddress { get; set; } = string.Empty;

        [JsonPropertyName("vendorEmail")]
        public string VendorEmail { get; set; } = string.Empty;

        [JsonPropertyName("vendorPhone")]
        public string VendorPhone { get; set; } = string.Empty;

        [JsonPropertyName("gstNumber")]
        public string GstNumber { get; set; } = string.Empty;

        [JsonPropertyName("stateCode")]
        public string StateCode { get; set; } = string.Empty;

        [JsonPropertyName("pan")]
        public string Pan { get; set; } = string.Empty;
    }

    public class PurchaseOrderLineDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

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

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("receivedQuantity")]
        public decimal ReceivedQuantity { get; set; }

        [JsonPropertyName("rejectedQuantity")]
        public decimal RejectedQuantity { get; set; }

        [JsonPropertyName("invoicedQuantity")]
        public decimal InvoicedQuantity { get; set; }

        [JsonPropertyName("cancelledQuantity")]
        public decimal CancelledQuantity { get; set; }

        [JsonPropertyName("unit")]
        public string Unit { get; set; } = "Nos";

        [JsonPropertyName("baseUom")]
        public string BaseUom { get; set; } = "Nos";

        [JsonPropertyName("rate")]
        public decimal Rate { get; set; }

        [JsonPropertyName("discount")]
        public decimal Discount { get; set; }

        [JsonPropertyName("taxableAmount")]
        public decimal TaxableAmount { get; set; }

        [JsonPropertyName("gstRatePercentage")]
        public decimal GstRatePercentage { get; set; } = 18m;

        [JsonPropertyName("cgstAmount")]
        public decimal CgstAmount { get; set; }

        [JsonPropertyName("sgstAmount")]
        public decimal SgstAmount { get; set; }

        [JsonPropertyName("igstAmount")]
        public decimal IgstAmount { get; set; }

        [JsonPropertyName("tax")]
        public decimal Tax { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("overDeliveryTolerancePercent")]
        public decimal OverDeliveryTolerancePercent { get; set; }

        [JsonPropertyName("lineStatus")]
        public PurchaseOrderLineStatus LineStatus { get; set; } = PurchaseOrderLineStatus.Pending;

        [JsonPropertyName("expectedDeliveryDate")]
        public DateTime? ExpectedDeliveryDate { get; set; }

        [JsonPropertyName("deliveryScheduleJson")]
        public string DeliveryScheduleJson { get; set; } = "[]";
    }

    public class PurchaseOrderHistoryDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public PurchaseOrderStatus Status { get; set; }

        [JsonPropertyName("previousStatus")]
        public PurchaseOrderStatus? PreviousStatus { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("user")]
        public string User { get; set; } = string.Empty;

        [JsonPropertyName("remarks")]
        public string Remarks { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;
    }

    public class PurchaseOrderApprovalHistoryDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("eventKind")]
        public string EventKind { get; set; } = string.Empty;

        [JsonPropertyName("decision")]
        public string Decision { get; set; } = string.Empty;

        [JsonPropertyName("approver")]
        public string Approver { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role { get; set; } = "Procurement Manager";

        [JsonPropertyName("decisionDate")]
        public DateTime DecisionDate { get; set; }

        [JsonPropertyName("remarks")]
        public string Remarks { get; set; } = string.Empty;

        [JsonPropertyName("hmacSignatureToken")]
        public string? HmacSignatureToken { get; set; }
    }

    public class PurchaseOrderListItemDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("purchaseOrderNumber")]
        public string PurchaseOrderNumber { get; set; } = string.Empty;

        [JsonPropertyName("revisionNumber")]
        public int RevisionNumber { get; set; }

        [JsonPropertyName("isCurrentRevision")]
        public bool IsCurrentRevision { get; set; } = true;

        [JsonPropertyName("vendorName")]
        public string VendorName { get; set; } = string.Empty;

        [JsonPropertyName("vendorCode")]
        public string VendorCode { get; set; } = string.Empty;

        [JsonPropertyName("referenceNumber")]
        public string ReferenceNumber { get; set; } = string.Empty;

        [JsonPropertyName("orderDate")]
        public DateTime OrderDate { get; set; }

        [JsonPropertyName("expectedDeliveryDate")]
        public DateTime? ExpectedDeliveryDate { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "INR";

        [JsonPropertyName("status")]
        public PurchaseOrderStatus Status { get; set; }

        [JsonPropertyName("requiredApprovalTier")]
        public PurchaseOrderApprovalTier RequiredApprovalTier { get; set; }

        [JsonPropertyName("currentApprovalTier")]
        public PurchaseOrderApprovalTier CurrentApprovalTier { get; set; }

        [JsonPropertyName("isExceptionApproval")]
        public bool IsExceptionApproval { get; set; }

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("remarks")]
        public string Remarks { get; set; } = string.Empty;
    }

    public class PurchaseOrderDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("tenantId")]
        public int TenantId { get; set; } = 1;

        [JsonPropertyName("purchaseOrderNumber")]
        public string PurchaseOrderNumber { get; set; } = string.Empty;

        [JsonPropertyName("revisionNumber")]
        public int RevisionNumber { get; set; } = 0;

        [JsonPropertyName("isCurrentRevision")]
        public bool IsCurrentRevision { get; set; } = true;

        [JsonPropertyName("parentPOId")]
        public int? ParentPOId { get; set; }

        [JsonPropertyName("supersededAt")]
        public DateTime? SupersededAt { get; set; }

        [JsonPropertyName("referenceNumber")]
        public string ReferenceNumber { get; set; } = string.Empty;

        [JsonPropertyName("vendor")]
        public PurchaseOrderVendorDto Vendor { get; set; } = new();

        [JsonPropertyName("vendorCode")]
        public string VendorCode { get; set; } = string.Empty;

        [JsonPropertyName("vendorGstin")]
        public string VendorGstin { get; set; } = string.Empty;

        [JsonPropertyName("vendorStateCode")]
        public string VendorStateCode { get; set; } = string.Empty;

        [JsonPropertyName("buyerName")]
        public string BuyerName { get; set; } = string.Empty;

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonPropertyName("orderDate")]
        public DateTime OrderDate { get; set; }

        [JsonPropertyName("expectedDeliveryDate")]
        public DateTime? ExpectedDeliveryDate { get; set; }

        [JsonPropertyName("paymentTermsDays")]
        public int PaymentTermsDays { get; set; } = 30;

        [JsonPropertyName("paymentTerms")]
        public string PaymentTerms { get; set; } = "Net 30";

        [JsonPropertyName("incoterms")]
        public string Incoterms { get; set; } = "FOR Destination";

        [JsonPropertyName("deliveryTerms")]
        public string DeliveryTerms { get; set; } = "Door Delivery";

        [JsonPropertyName("sourceType")]
        public PurchaseOrderSourceType SourceType { get; set; } = PurchaseOrderSourceType.Manual;

        [JsonPropertyName("salesOrderNumber")]
        public string SalesOrderNumber { get; set; } = string.Empty;

        [JsonPropertyName("priority")]
        public PurchaseOrderPriority Priority { get; set; } = PurchaseOrderPriority.Normal;

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "INR";

        [JsonPropertyName("exchangeRate")]
        public decimal ExchangeRate { get; set; } = 1.000000m;

        [JsonPropertyName("subtotal")]
        public decimal Subtotal { get; set; }

        [JsonPropertyName("discountTotal")]
        public decimal DiscountTotal { get; set; }

        [JsonPropertyName("totalCgstAmount")]
        public decimal TotalCgstAmount { get; set; }

        [JsonPropertyName("totalSgstAmount")]
        public decimal TotalSgstAmount { get; set; }

        [JsonPropertyName("totalIgstAmount")]
        public decimal TotalIgstAmount { get; set; }

        [JsonPropertyName("taxTotal")]
        public decimal TaxTotal { get; set; }

        [JsonPropertyName("freightAmount")]
        public decimal FreightAmount { get; set; }

        [JsonPropertyName("insuranceAmount")]
        public decimal InsuranceAmount { get; set; }

        [JsonPropertyName("totalAmount")]
        public decimal TotalAmount { get; set; }

        [JsonPropertyName("grandTotalBaseCurrency")]
        public decimal GrandTotalBaseCurrency { get; set; }

        [JsonPropertyName("status")]
        public PurchaseOrderStatus Status { get; set; }

        [JsonPropertyName("requiredApprovalTier")]
        public PurchaseOrderApprovalTier RequiredApprovalTier { get; set; }

        [JsonPropertyName("currentApprovalTier")]
        public PurchaseOrderApprovalTier CurrentApprovalTier { get; set; }

        [JsonPropertyName("isExceptionApproval")]
        public bool IsExceptionApproval { get; set; }

        [JsonPropertyName("exceptionReasonsJson")]
        public string ExceptionReasonsJson { get; set; } = "[]";

        [JsonPropertyName("approvalToken")]
        public string? ApprovalToken { get; set; }

        [JsonPropertyName("approvedByUserId")]
        public string? ApprovedByUserId { get; set; }

        [JsonPropertyName("approvedAt")]
        public DateTime? ApprovedAt { get; set; }

        [JsonPropertyName("amendmentReason")]
        public string? AmendmentReason { get; set; }

        [JsonPropertyName("amendmentType")]
        public POAmendmentType? AmendmentType { get; set; }

        [JsonPropertyName("remarks")]
        public string Remarks { get; set; } = string.Empty;

        [JsonPropertyName("lines")]
        public List<PurchaseOrderLineDto> Lines { get; set; } = new();

        [JsonPropertyName("history")]
        public List<PurchaseOrderHistoryDto> History { get; set; } = new();

        [JsonPropertyName("approvalHistory")]
        public List<PurchaseOrderApprovalHistoryDto> ApprovalHistory { get; set; } = new();

        [JsonPropertyName("submittedDate")]
        public DateTime? SubmittedDate { get; set; }

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("createdDate")]
        public DateTime CreatedDate { get; set; }

        [JsonPropertyName("updatedBy")]
        public string UpdatedBy { get; set; } = string.Empty;

        [JsonPropertyName("updatedDate")]
        public DateTime UpdatedDate { get; set; }
    }

    public class PurchaseOrderCreateRequestDto
    {
        [JsonPropertyName("purchaseOrderNumber")]
        public string? PurchaseOrderNumber { get; set; }

        [JsonPropertyName("referenceNumber")]
        public string? ReferenceNumber { get; set; }

        [JsonPropertyName("vendor")]
        public PurchaseOrderVendorDto Vendor { get; set; } = new();

        [JsonPropertyName("buyerName")]
        public string? BuyerName { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("orderDate")]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("expectedDeliveryDate")]
        public DateTime? ExpectedDeliveryDate { get; set; }

        [JsonPropertyName("paymentTerms")]
        public string? PaymentTerms { get; set; }

        [JsonPropertyName("deliveryTerms")]
        public string? DeliveryTerms { get; set; }

        [JsonPropertyName("sourceType")]
        public PurchaseOrderSourceType SourceType { get; set; } = PurchaseOrderSourceType.Manual;

        [JsonPropertyName("salesOrderNumber")]
        public string? SalesOrderNumber { get; set; }

        [JsonPropertyName("priority")]
        public PurchaseOrderPriority Priority { get; set; } = PurchaseOrderPriority.Normal;

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "INR";

        [JsonPropertyName("lines")]
        public List<PurchaseOrderLineDto> Lines { get; set; } = new();

        [JsonPropertyName("status")]
        public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

        [JsonPropertyName("remarks")]
        public string? Remarks { get; set; }
    }

    public class PurchaseOrderUpdateRequestDto
    {
        [JsonPropertyName("purchaseOrderNumber")]
        public string? PurchaseOrderNumber { get; set; }

        [JsonPropertyName("referenceNumber")]
        public string? ReferenceNumber { get; set; }

        [JsonPropertyName("vendor")]
        public PurchaseOrderVendorDto Vendor { get; set; } = new();

        [JsonPropertyName("buyerName")]
        public string? BuyerName { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("orderDate")]
        public DateTime OrderDate { get; set; }

        [JsonPropertyName("expectedDeliveryDate")]
        public DateTime? ExpectedDeliveryDate { get; set; }

        [JsonPropertyName("paymentTerms")]
        public string? PaymentTerms { get; set; }

        [JsonPropertyName("deliveryTerms")]
        public string? DeliveryTerms { get; set; }

        [JsonPropertyName("sourceType")]
        public PurchaseOrderSourceType SourceType { get; set; }

        [JsonPropertyName("salesOrderNumber")]
        public string? SalesOrderNumber { get; set; }

        [JsonPropertyName("priority")]
        public PurchaseOrderPriority Priority { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "INR";

        [JsonPropertyName("lines")]
        public List<PurchaseOrderLineDto> Lines { get; set; } = new();

        [JsonPropertyName("remarks")]
        public string? Remarks { get; set; }
    }

    public class PurchaseOrderStatusUpdateRequestDto
    {
        [JsonPropertyName("status")]
        public PurchaseOrderStatus Status { get; set; }

        [JsonPropertyName("remarks")]
        public string? Remarks { get; set; }
    }

    public class PurchaseOrderApprovalActionRequestDto
    {
        [JsonPropertyName("remarks")]
        public string Remarks { get; set; } = string.Empty;
    }

    public class PurchaseOrderApprovalQueueItemDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("purchaseOrderNumber")]
        public string PurchaseOrderNumber { get; set; } = string.Empty;

        [JsonPropertyName("revisionNumber")]
        public int RevisionNumber { get; set; }

        [JsonPropertyName("vendorName")]
        public string VendorName { get; set; } = string.Empty;

        [JsonPropertyName("submittedBy")]
        public string SubmittedBy { get; set; } = string.Empty;

        [JsonPropertyName("submittedDate")]
        public DateTime SubmittedDate { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "INR";

        [JsonPropertyName("priority")]
        public PurchaseOrderPriority Priority { get; set; }

        [JsonPropertyName("status")]
        public PurchaseOrderStatus Status { get; set; }

        [JsonPropertyName("requiredApprovalTier")]
        public PurchaseOrderApprovalTier RequiredApprovalTier { get; set; }

        [JsonPropertyName("currentApprovalTier")]
        public PurchaseOrderApprovalTier CurrentApprovalTier { get; set; }

        [JsonPropertyName("isExceptionApproval")]
        public bool IsExceptionApproval { get; set; }

        [JsonPropertyName("approvalStatus")]
        public string ApprovalStatus { get; set; } = string.Empty;
    }

    public class PurchaseOrderApprovalMetricsDto
    {
        [JsonPropertyName("pendingApproval")]
        public int PendingApproval { get; set; }

        [JsonPropertyName("approvedToday")]
        public int ApprovedToday { get; set; }

        [JsonPropertyName("rejected")]
        public int Rejected { get; set; }

        [JsonPropertyName("revisionRequired")]
        public int RevisionRequired { get; set; }

        [JsonPropertyName("averageApprovalTimeLabel")]
        public string AverageApprovalTimeLabel { get; set; } = "1.5 hours";
    }

    public class PurchaseOrderListQueryDto
    {
        public string? Search { get; set; }

        public string? Status { get; set; }

        public DateTime? DateFrom { get; set; }

        public DateTime? DateTo { get; set; }

        public string? VendorName { get; set; }

        public string? Priority { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }
}
