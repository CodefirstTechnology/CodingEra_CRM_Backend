namespace ERP.Application.Sales.Dtos
{
    public class DiscountApprovalDto
    {
        public int Id { get; set; }
        public string ApprovalNumber { get; set; } = string.Empty;
        public string RequestDate { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public int? QuotationId { get; set; }
        public string QuotationNumber { get; set; } = string.Empty;
        public int? SalesOrderId { get; set; }
        public string SalesOrderNumber { get; set; } = string.Empty;
        public int? PriceListId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerCategory { get; set; } = string.Empty;
        public int SalesPersonUserId { get; set; }
        public decimal RequestedDiscountPercentage { get; set; }
        public decimal? ApprovedDiscountPercentage { get; set; }
        public decimal RequestedAmount { get; set; }
        public decimal? ApprovedAmount { get; set; }
        public string ApprovalLevel { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedDate { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public string UpdatedDate { get; set; } = string.Empty;
        public List<DiscountApprovalHistoryDto> History { get; set; } = new();
        public List<DiscountApprovalCommentDto> Comments { get; set; } = new();
    }

    public class DiscountApprovalListItemDto
    {
        public int Id { get; set; }
        public string ApprovalNumber { get; set; } = string.Empty;
        public string RequestDate { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerCategory { get; set; } = string.Empty;
        public string SalesOrderNumber { get; set; } = string.Empty;
        public string QuotationNumber { get; set; } = string.Empty;
        public decimal RequestedDiscountPercentage { get; set; }
        public decimal RequestedAmount { get; set; }
        public string ApprovalLevel { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int SalesPersonUserId { get; set; }
    }

    public class DiscountApprovalHistoryDto
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string OldStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
        public string PerformedOn { get; set; } = string.Empty;
    }

    public class DiscountApprovalCommentDto
    {
        public int Id { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string CommentedBy { get; set; } = string.Empty;
        public string CommentedOn { get; set; } = string.Empty;
    }

    public class DiscountApprovalStatisticsDto
    {
        public int TotalCount { get; set; }
        public int PendingCount { get; set; }
        public int UnderReviewCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public int ReturnedCount { get; set; }
        public int CancelledCount { get; set; }
        public decimal TotalRequestedAmount { get; set; }
        public decimal TotalApprovedAmount { get; set; }
        public decimal AverageRequestedDiscount { get; set; }
        public List<DiscountApprovalListItemDto> Recent { get; set; } = new();
    }

    public class DiscountApprovalCreateRequestDto
    {
        public string? RequestDate { get; set; }
        public string SourceType { get; set; } = string.Empty;
        public int? QuotationId { get; set; }
        public string? QuotationNumber { get; set; }
        public int? SalesOrderId { get; set; }
        public string? SalesOrderNumber { get; set; }
        public int? PriceListId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerCategory { get; set; } = string.Empty;
        public int SalesPersonUserId { get; set; }
        public decimal RequestedDiscountPercentage { get; set; }
        public decimal RequestedAmount { get; set; }
        public string? ApprovalLevel { get; set; }
        public string? Priority { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Remarks { get; set; }
    }

    public class DiscountApprovalUpdateRequestDto
    {
        public string? RequestDate { get; set; }
        public string SourceType { get; set; } = string.Empty;
        public int? QuotationId { get; set; }
        public string? QuotationNumber { get; set; }
        public int? SalesOrderId { get; set; }
        public string? SalesOrderNumber { get; set; }
        public int? PriceListId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerCategory { get; set; } = string.Empty;
        public int SalesPersonUserId { get; set; }
        public decimal RequestedDiscountPercentage { get; set; }
        public decimal RequestedAmount { get; set; }
        public string? ApprovalLevel { get; set; }
        public string? Priority { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Remarks { get; set; }
    }

    public class DiscountApprovalDecisionRequestDto
    {
        public decimal? ApprovedDiscountPercentage { get; set; }
        public decimal? ApprovedAmount { get; set; }
        public string? Remarks { get; set; }
    }

    public class DiscountApprovalCommentRequestDto
    {
        public string Comment { get; set; } = string.Empty;
    }

    public class DiscountApprovalListQueryDto
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public string? ApprovalLevel { get; set; }
        public string? SourceType { get; set; }
        public string? CustomerCategory { get; set; }
        public int? SalesPersonUserId { get; set; }
        public string? DateFrom { get; set; }
        public string? DateTo { get; set; }
    }

    public class DiscountApprovalLookupDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
