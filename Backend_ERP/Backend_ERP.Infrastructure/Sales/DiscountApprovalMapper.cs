using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Shared.Helpers;

namespace ERP.Infrastructure.Sales
{
    /// <summary>
    /// Maps DiscountApproval domain entities to DTOs.
    /// Date helpers are delegated to the shared DateHelper to avoid duplication.
    /// </summary>
    internal static class DiscountApprovalMapper
    {
        /// <summary>Kept for callers inside DiscountApprovalService that rely on ParseDate.</summary>
        public static DateOnly ParseDate(string? value, DateOnly fallback) =>
            DateHelper.ParseDate(value, fallback);

        public static DiscountApprovalListItemDto ToListItem(DiscountApproval e) => new()
        {
            Id = e.Id,
            ApprovalNumber = e.ApprovalNumber,
            RequestDate = DateHelper.FormatDate(e.RequestDate),
            SourceType = e.SourceType,
            CustomerName = e.CustomerName,
            CustomerCategory = e.CustomerCategory,
            SalesOrderNumber = e.SalesOrderNumber,
            QuotationNumber = e.QuotationNumber,
            RequestedDiscountPercentage = e.RequestedDiscountPercentage,
            RequestedAmount = e.RequestedAmount,
            ApprovalLevel = e.ApprovalLevel,
            Priority = e.Priority,
            Status = e.Status,
            SalesPersonUserId = e.SalesPersonUserId
        };

        public static DiscountApprovalHistoryDto ToHistoryDto(DiscountApprovalHistory h) => new()
        {
            Id = h.Id,
            Action = h.Action,
            OldStatus = h.OldStatus,
            NewStatus = h.NewStatus,
            Remarks = h.Remarks,
            PerformedBy = h.PerformedBy,
            PerformedOn = DateHelper.FormatDateTime(h.PerformedOn)
        };

        public static DiscountApprovalCommentDto ToCommentDto(DiscountApprovalComment c) => new()
        {
            Id = c.Id,
            Comment = c.Comment,
            CommentedBy = c.CommentedBy,
            CommentedOn = DateHelper.FormatDateTime(c.CommentedOn)
        };

        public static DiscountApprovalDto ToDto(DiscountApproval e) => new()
        {
            Id = e.Id,
            ApprovalNumber = e.ApprovalNumber,
            RequestDate = DateHelper.FormatDate(e.RequestDate),
            SourceType = e.SourceType,
            QuotationId = e.QuotationId,
            QuotationNumber = e.QuotationNumber,
            SalesOrderId = e.SalesOrderId,
            SalesOrderNumber = e.SalesOrderNumber,
            PriceListId = e.PriceListId,
            CustomerName = e.CustomerName,
            CustomerCategory = e.CustomerCategory,
            SalesPersonUserId = e.SalesPersonUserId,
            RequestedDiscountPercentage = e.RequestedDiscountPercentage,
            ApprovedDiscountPercentage = e.ApprovedDiscountPercentage,
            RequestedAmount = e.RequestedAmount,
            ApprovedAmount = e.ApprovedAmount,
            ApprovalLevel = e.ApprovalLevel,
            Priority = e.Priority,
            Status = e.Status,
            Reason = e.Reason,
            Remarks = e.Remarks,
            CreatedBy = e.CreatedBy,
            CreatedDate = DateHelper.FormatDateTime(e.CreatedDate),
            UpdatedBy = e.UpdatedBy,
            UpdatedDate = DateHelper.FormatDateTime(e.UpdatedDate),
            History = e.History
                .OrderBy(x => x.PerformedOn)
                .Select(ToHistoryDto)
                .ToList(),
            Comments = e.Comments
                .OrderBy(x => x.CommentedOn)
                .Select(ToCommentDto)
                .ToList()
        };
    }
}
