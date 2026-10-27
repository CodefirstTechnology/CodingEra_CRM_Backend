using System;
using System.Collections.Generic;

namespace ERP.Application.Sales.Dtos
{
    public class SalesOrderCustomerDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerCategory { get; set; } = "Standard";
        public string ContactPerson { get; set; } = string.Empty;
        public string BillingAddress { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string PlaceOfSupplyStateCode { get; set; } = "27";
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
    }

    public class SalesOrderItemDto
    {
        public string Id { get; set; } = string.Empty;
        public int? ItemId { get; set; }
        public int? WarehouseId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal AllocatedQuantity { get; set; }
        public decimal DispatchedQuantity { get; set; }
        public decimal InvoicedQuantity { get; set; }
        public decimal ProformaInvoicedQuantity { get; set; }
        public string Unit { get; set; } = "Nos";
        public string BaseStockUom { get; set; } = "Nos";
        public decimal Rate { get; set; }
        public decimal Discount { get; set; }
        public decimal Gst { get; set; }
        public decimal Amount { get; set; }

        // Frozen snapshots
        public int? ResolvedPriceListId { get; set; }
        public string PriceListNumberSnapshot { get; set; } = string.Empty;
        public decimal OriginalSellingRate { get; set; }
        public decimal FloorPriceSnapshot { get; set; }
        public decimal MaxDiscountPctSnapshot { get; set; }
        public decimal TaxSlabPercentSnapshot { get; set; }
        public string PlaceOfSupplyStateCode { get; set; } = "27";
    }

    public class SalesOrderStatusHistoryDto
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string? Label { get; set; }
    }

    public class SalesOrderEmailHistoryDto
    {
        public string Id { get; set; } = string.Empty;
        public int SalesOrderId { get; set; }
        public string Recipient { get; set; } = string.Empty;
        public string SentDate { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class SalesOrderDto
    {
        public int Id { get; set; }
        public string SalesOrderNumber { get; set; } = string.Empty;
        public int? QuotationId { get; set; }
        public string QuotationNumber { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerCategory { get; set; } = "Standard";
        public string PlaceOfSupplyStateCode { get; set; } = "27";
        public SalesOrderCustomerDto Customer { get; set; } = new();
        public string SalesPerson { get; set; } = string.Empty;
        public int? SalesPersonUserId { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string OrderDate { get; set; } = string.Empty;
        public string? ExpectedDeliveryDate { get; set; }
        public string PaymentTerms { get; set; } = string.Empty;
        public string DeliveryTerms { get; set; } = string.Empty;
        public List<SalesOrderItemDto> Items { get; set; } = new();
        public decimal Amount { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal GstTotal { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal AdvanceAllocatedAmount { get; set; }
        public decimal ProformaInvoicedAmount { get; set; }
        public decimal FinalInvoicedAmount { get; set; }

        // Two-Tier Release Gates
        public bool IsProductionReleased { get; set; }
        public string? ProductionReleasedDate { get; set; }
        public string? ProductionReleasedBy { get; set; }
        public bool IsDispatchReleased { get; set; }
        public string? DispatchReleasedDate { get; set; }
        public string? DispatchReleasedBy { get; set; }

        // Lifecycle & Credit Management
        public string Status { get; set; } = string.Empty;
        public string? CreditHoldReason { get; set; }
        public string? CreditApprovedDate { get; set; }
        public string? CreditApprovedBy { get; set; }
        public string Remarks { get; set; } = string.Empty;

        public List<SalesOrderStatusHistoryDto> StatusHistory { get; set; } = new();
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedDate { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public string UpdatedDate { get; set; } = string.Empty;
        public string? PdfGeneratedDate { get; set; }
        public string? LastCommunicationDate { get; set; }
        public List<SalesOrderEmailHistoryDto>? EmailHistory { get; set; }
        public int? AuditCount { get; set; }
        public string? LastModified { get; set; }
    }

    public class SalesOrderListItemDto
    {
        public int Id { get; set; }
        public string SalesOrderNumber { get; set; } = string.Empty;
        public string QuotationNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string OrderDate { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal AllocatedPercentage { get; set; }
        public decimal DispatchedPercentage { get; set; }
        public bool IsProductionReleased { get; set; }
        public bool IsDispatchReleased { get; set; }
        public string? CreditHoldReason { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class SalesOrderCreateRequestDto
    {
        public string? SalesOrderNumber { get; set; }
        public int? QuotationId { get; set; }
        public string? QuotationNumber { get; set; }
        public string? SourceType { get; set; }
        public SalesOrderCustomerDto Customer { get; set; } = new();
        public string? SalesPerson { get; set; }
        public int? SalesPersonUserId { get; set; }
        public string? Notes { get; set; }
        public string OrderDate { get; set; } = string.Empty;
        public string? ExpectedDeliveryDate { get; set; }
        public string PaymentTerms { get; set; } = string.Empty;
        public string DeliveryTerms { get; set; } = string.Empty;
        public List<SalesOrderItemDto> Items { get; set; } = new();
        public string? Status { get; set; }
    }

    public class SalesOrderUpdateRequestDto
    {
        public string? SalesOrderNumber { get; set; }
        public int? QuotationId { get; set; }
        public string? QuotationNumber { get; set; }
        public string? SourceType { get; set; }
        public SalesOrderCustomerDto Customer { get; set; } = new();
        public string? SalesPerson { get; set; }
        public int? SalesPersonUserId { get; set; }
        public string? Notes { get; set; }
        public string OrderDate { get; set; } = string.Empty;
        public string? ExpectedDeliveryDate { get; set; }
        public string PaymentTerms { get; set; } = string.Empty;
        public string DeliveryTerms { get; set; } = string.Empty;
        public List<SalesOrderItemDto> Items { get; set; } = new();
    }

    public class SalesOrderStatusUpdateRequestDto
    {
        public string Status { get; set; } = string.Empty;
        public string? Remarks { get; set; }
    }

    public class SalesOrderCancelRequestDto
    {
        public string? Remarks { get; set; }
    }

    public class SalesOrderListQueryDto
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? DateFrom { get; set; }
        public string? DateTo { get; set; }
        public int? CustomerId { get; set; }
    }

    public class SalesOrderEmailRequestDto
    {
        public string Recipient { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public string? Body { get; set; }
    }

    public class SalesOrderPdfResultDto
    {
        public int SalesOrderId { get; set; }
        public string SalesOrderNumber { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string GeneratedDate { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/pdf";
        public byte[] Content { get; set; } = Array.Empty<byte>();
    }

    public class SalesOrderEmailResultDto
    {
        public int SalesOrderId { get; set; }
        public string SalesOrderNumber { get; set; } = string.Empty;
        public string Recipient { get; set; } = string.Empty;
        public string SentDate { get; set; } = string.Empty;
        public bool Success { get; set; } = true;
        public string Message { get; set; } = "Email sent successfully.";
    }

    // Allocation & Split-Warehouse Contracts
    public class SalesOrderAllocationDto
    {
        public int Id { get; set; }
        public int SalesOrderId { get; set; }
        public int SalesOrderItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public int FinishedGoodId { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public decimal AllocatedQuantity { get; set; }
        public string AllocationStatus { get; set; } = string.Empty;
        public string AllocatedAt { get; set; } = string.Empty;
        public string AllocatedBy { get; set; } = string.Empty;
        public string? ReleasedDate { get; set; }
        public string? ReleaseRemarks { get; set; }
    }

    public class SplitAllocationItemDto
    {
        public int SalesOrderItemId { get; set; }
        public int WarehouseId { get; set; }
        public decimal Quantity { get; set; }
    }

    public class SplitAllocationRequestDto
    {
        public List<SplitAllocationItemDto> Splits { get; set; } = new();
        public string Remarks { get; set; } = string.Empty;
    }

    public class LineAllocationStatusDto
    {
        public int SalesOrderItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public decimal OrderedQuantity { get; set; }
        public decimal AllocatedQuantity { get; set; }
        public decimal DeficitQuantity { get; set; }
    }

    public class AllocationResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<LineAllocationStatusDto> Lines { get; set; } = new();
    }

    // Credit Assessment & Override Contracts
    public class CreditAssessmentResultDto
    {
        public bool IsPassed { get; set; }
        public bool IsOnHold { get; set; }
        public string Reason { get; set; } = string.Empty;
        public decimal TotalExposure { get; set; }
        public decimal ApprovedCreditLimit { get; set; }

        public static CreditAssessmentResultDto Passed(decimal exposure, decimal limit) => new()
        {
            IsPassed = true,
            IsOnHold = false,
            TotalExposure = exposure,
            ApprovedCreditLimit = limit,
            Reason = "Credit exposure verified within approved customer credit limit."
        };

        public static CreditAssessmentResultDto Hold(string reason, decimal exposure = 0, decimal limit = 0) => new()
        {
            IsPassed = false,
            IsOnHold = true,
            Reason = reason,
            TotalExposure = exposure,
            ApprovedCreditLimit = limit
        };
    }

    public class CreditOverrideRequestDto
    {
        public string Justification { get; set; } = string.Empty;
    }

    // Commercial Advance Gate Check Contracts
    public class AdvanceGateCheckResultDto
    {
        public bool IsLocked { get; set; }
        public string LockReason { get; set; } = string.Empty;
        public decimal RequiredAmount { get; set; }
        public decimal AllocatedAmount { get; set; }

        public static AdvanceGateCheckResultDto Released() => new() { IsLocked = false };

        public static AdvanceGateCheckResultDto Locked(string reason, decimal required = 0, decimal allocated = 0) => new()
        {
            IsLocked = true,
            LockReason = reason,
            RequiredAmount = required,
            AllocatedAmount = allocated
        };
    }

    // MTO Net Deficit & Planning Contracts
    public class MtoEvaluationResultDto
    {
        public int SalesOrderId { get; set; }
        public bool IsMtoRequired { get; set; }
        public List<int> SpawnedProductionPlanIds { get; set; } = new();
        public List<string> Details { get; set; } = new();
    }
}
