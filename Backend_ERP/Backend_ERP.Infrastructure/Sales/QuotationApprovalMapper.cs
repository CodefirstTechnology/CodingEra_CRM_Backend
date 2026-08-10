using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Shared.Helpers;

namespace ERP.Infrastructure.Sales
{
    /// <summary>
    /// Maps QuotationApproval domain entities to DTOs.
    /// Date helpers are delegated to the shared DateHelper to avoid duplication.
    /// </summary>
    internal static class QuotationApprovalMapper
    {
        public static QuotationApprovalListItemDto ToListItem(QuotationApproval e) => new()
        {
            Id = e.Id,
            ApprovalNumber = e.ApprovalNumber,
            RequestDate = DateHelper.FormatDate(e.RequestDate),
            CustomerName = e.CustomerName,
            QuotationNumber = e.QuotationNumber,
            SalesOrderNumber = e.SalesOrderNumber,
            TotalAmount = e.TotalAmount,
            ApprovalLevel = e.ApprovalLevel,
            Priority = e.Priority,
            Status = e.Status,
            SalesPersonUserId = e.SalesPersonUserId
        };

        public static QuotationApprovalHistoryDto ToHistoryDto(QuotationApprovalHistory h) => new()
        {
            Id = h.Id,
            Action = h.Action,
            OldStatus = h.OldStatus,
            NewStatus = h.NewStatus,
            Remarks = h.Remarks,
            PerformedBy = h.PerformedBy,
            PerformedOn = DateHelper.FormatDateTime(h.PerformedOn)
        };

        public static QuotationApprovalCommentDto ToCommentDto(QuotationApprovalComment c) => new()
        {
            Id = c.Id,
            Comment = c.Comment,
            CommentedBy = c.CommentedBy,
            CommentedOn = DateHelper.FormatDateTime(c.CommentedOn)
        };

        public static QuotationApprovalDto ToDto(QuotationApproval e) => new()
        {
            Id = e.Id,
            ApprovalNumber = e.ApprovalNumber,
            RequestDate = DateHelper.FormatDate(e.RequestDate),
            QuotationId = e.QuotationId,
            QuotationNumber = e.QuotationNumber,
            SalesOrderId = e.SalesOrderId,
            SalesOrderNumber = e.SalesOrderNumber,
            CustomerName = e.CustomerName,
            SalesPersonUserId = e.SalesPersonUserId,
            TotalAmount = e.TotalAmount,
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
