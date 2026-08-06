using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;

namespace ERP.Infrastructure.Sales
{
    internal static class QuotationApprovalMapper
    {
        public static string FormatDate(DateOnly date) => date.ToString("yyyy-MM-dd");

        public static string FormatDateTime(DateTimeOffset value) =>
            value.UtcDateTime.ToString("O");

        public static DateOnly ParseDate(string? value, DateOnly fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            if (DateOnly.TryParse(value, out var date))
            {
                return date;
            }

            if (DateTimeOffset.TryParse(value, out var dto))
            {
                return DateOnly.FromDateTime(dto.UtcDateTime);
            }

            return fallback;
        }

        public static QuotationApprovalListItemDto ToListItem(QuotationApproval e) => new()
        {
            Id = e.Id,
            ApprovalNumber = e.ApprovalNumber,
            RequestDate = FormatDate(e.RequestDate),
            CustomerName = e.CustomerName,
            QuotationNumber = e.QuotationNumber,
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
            PerformedOn = FormatDateTime(h.PerformedOn)
        };

        public static QuotationApprovalCommentDto ToCommentDto(QuotationApprovalComment c) => new()
        {
            Id = c.Id,
            Comment = c.Comment,
            CommentedBy = c.CommentedBy,
            CommentedOn = FormatDateTime(c.CommentedOn)
        };

        public static QuotationApprovalDto ToDto(QuotationApproval e) => new()
        {
            Id = e.Id,
            ApprovalNumber = e.ApprovalNumber,
            RequestDate = FormatDate(e.RequestDate),
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
            CreatedDate = FormatDateTime(e.CreatedDate),
            UpdatedBy = e.UpdatedBy,
            UpdatedDate = FormatDateTime(e.UpdatedDate),
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
