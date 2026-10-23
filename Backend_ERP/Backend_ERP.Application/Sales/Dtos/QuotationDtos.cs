using System;
using System.Collections.Generic;
using ERP.Domain.Sales;

namespace ERP.Application.Sales.Dtos
{
    public class QuotationDto
    {
        public int Id { get; set; }
        public string TenantId { get; set; } = "default";
        public string QuotationNumber { get; set; } = string.Empty;
        public int? ParentQuotationId { get; set; }
        public int? RootQuotationId { get; set; }
        public int RevisionNumber { get; set; } = 0;
        public bool IsCurrentRevision { get; set; } = true;
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerCategory { get; set; } = "Standard";
        public string ContactPerson { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public string BillingAddress { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string SalesPerson { get; set; } = string.Empty;
        public int? SalesPersonUserId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string QuotationDate { get; set; } = string.Empty;
        public string ValidUntil { get; set; } = string.Empty;
        public string Currency { get; set; } = "INR";
        public decimal ExchangeRate { get; set; } = 1.0m;
        public decimal Subtotal { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal TaxTotal { get; set; }
        public decimal FreightAmount { get; set; }
        public decimal PackagingAmount { get; set; }
        public decimal RoundOff { get; set; }
        public decimal GrandTotal { get; set; }
        public string PaymentTerms { get; set; } = string.Empty;
        public string DeliveryTerms { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string? ClientPoNumber { get; set; }
        public string? ClientAcceptedAt { get; set; }
        public string? ClientPoAttachmentUrl { get; set; }
        public int? ConvertedSalesOrderId { get; set; }
        public string? ConvertedSalesOrderNumber { get; set; }
        public string? ConvertedOn { get; set; }
        public int? DiscountApprovalId { get; set; }
        public string DiscountApprovalStatus { get; set; } = "None";
        public string? ApprovalToken { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTimeOffset CreatedDate { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
        public DateTimeOffset UpdatedDate { get; set; }
        public List<QuotationItemDto> Items { get; set; } = new();
    }

    public class QuotationItemDto
    {
        public int Id { get; set; }
        public string ItemType { get; set; } = "FinishedGood";
        public int? ItemId { get; set; }
        public int? ProductId { get; set; }
        public int? WarehouseId { get; set; }
        public string BaseStockUom { get; set; } = "Nos";
        public int LineNumber { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "NOS";
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxPercent { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal LineTotal { get; set; }
        public int SortOrder { get; set; }

        // Frozen Snapshot Fields
        public int? ResolvedPriceListId { get; set; }
        public string PriceListNumberSnapshot { get; set; } = string.Empty;
        public string ResolutionStageSnapshot { get; set; } = string.Empty;
        public decimal OriginalSellingRate { get; set; }
        public decimal FloorPriceSnapshot { get; set; }
        public decimal MaxDiscountPctSnapshot { get; set; }
        public decimal TaxSlabPercentSnapshot { get; set; }
        public DateTimeOffset SnapshotTimestampUtc { get; set; }
        public bool IsCommercialDeviation { get; set; }
    }

    public class QuotationCreateRequestDto
    {
        public string? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerCategory { get; set; }
        public string? ContactPerson { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public string? BillingAddress { get; set; }
        public string? ShippingAddress { get; set; }
        public string? SalesPerson { get; set; }
        public int? SalesPersonUserId { get; set; }
        public DateOnly? QuotationDate { get; set; }
        public DateOnly? ValidUntil { get; set; }
        public string? Currency { get; set; }
        public decimal? ExchangeRate { get; set; }
        public decimal? FreightAmount { get; set; }
        public decimal? PackagingAmount { get; set; }
        public decimal? RoundOff { get; set; }
        public string? PaymentTerms { get; set; }
        public string? DeliveryTerms { get; set; }
        public string? Notes { get; set; }
        public List<QuotationItemUpsertDto> Items { get; set; } = new();
    }

    public class QuotationUpdateRequestDto : QuotationCreateRequestDto
    {
    }

    public class QuotationItemUpsertDto
    {
        public string? ItemType { get; set; } = "FinishedGood";
        public int? ItemId { get; set; }
        public int? ProductId { get; set; }
        public int? WarehouseId { get; set; }
        public string? BaseStockUom { get; set; } = "Nos";
        public string? ItemCode { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Quantity { get; set; } = 1;
        public string? Unit { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal TaxPercent { get; set; } = 18;
        public int SortOrder { get; set; } = 1;
    }

    public class QuotationClientAcceptRequestDto
    {
        public string ClientPoNumber { get; set; } = string.Empty;
        public DateTimeOffset? ClientAcceptedAt { get; set; }
        public string? ClientPoAttachmentUrl { get; set; }
        public string? Remarks { get; set; }
    }

    public class QuotationReviseRequestDto
    {
        public string? Reason { get; set; }
    }

    public class QuotationStatusChangeRequestDto
    {
        public string Status { get; set; } = string.Empty;
        public string? Remarks { get; set; }
    }

    public class QuotationSubmitForApprovalRequestDto
    {
        public string? Reason { get; set; }
        public string Priority { get; set; } = "Normal";
        public string? ApprovalLevel { get; set; }
    }

    public class QuotationAtpCheckRequestDto
    {
        public int WarehouseId { get; set; }
        public int? ItemId { get; set; }
        public string? ItemCode { get; set; }
        public decimal RequestedQuantity { get; set; } = 1m;
    }

    public class QuotationAtpCheckResponseDto
    {
        public int? ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public decimal PhysicalStock { get; set; }
        public decimal ReservedStock { get; set; }
        public decimal AvailableQuantity { get; set; }
        public bool IsAvailable { get; set; }
        public decimal ShortageQuantity { get; set; }
        public DateTime? EarliestAvailableDateUtc { get; set; }
    }

    public class QuotationListQueryDto
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? Customer { get; set; }
        public DateOnly? DateFrom { get; set; }
        public DateOnly? DateTo { get; set; }
        public bool CurrentOnly { get; set; } = true;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }
}
