using ERP.Domain.Sales;

namespace ERP.Application.Sales
{
    public static class QuotationApprovalStatusRules
    {
        public static bool IsValid(string status) => QuotationApprovalStatuses.All.Contains(status);

        public static string? Normalize(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return null;
            var trimmed = status.Trim();
            if (string.Equals(trimmed, "Pending Approval", StringComparison.OrdinalIgnoreCase))
                return QuotationApprovalStatuses.Submitted;
            return QuotationApprovalStatuses.All.FirstOrDefault(x => string.Equals(x, trimmed, StringComparison.OrdinalIgnoreCase));
        }

        public static bool CanBeModified(string status) =>
            status == QuotationApprovalStatuses.Draft ||
            status == QuotationApprovalStatuses.Returned ||
            status == QuotationApprovalStatuses.RevisionRequired;

        public static bool CanBeDeleted(string status) =>
            status == QuotationApprovalStatuses.Draft;

        public static bool CanSubmit(string status) =>
            status == QuotationApprovalStatuses.Draft ||
            status == QuotationApprovalStatuses.Returned ||
            status == QuotationApprovalStatuses.RevisionRequired;

        public static bool CanReview(string status) =>
            status == QuotationApprovalStatuses.Submitted;

        public static bool CanApproveReject(string status) =>
            status == QuotationApprovalStatuses.Submitted ||
            status == QuotationApprovalStatuses.UnderReview ||
            string.Equals(status, "Pending Approval", StringComparison.OrdinalIgnoreCase);

        public static bool CanReturn(string status) =>
            status == QuotationApprovalStatuses.Submitted ||
            status == QuotationApprovalStatuses.UnderReview ||
            string.Equals(status, "Pending Approval", StringComparison.OrdinalIgnoreCase);

        public static bool CanCancel(string status) =>
            status == QuotationApprovalStatuses.Draft ||
            status == QuotationApprovalStatuses.Returned ||
            status == QuotationApprovalStatuses.RevisionRequired ||
            status == QuotationApprovalStatuses.Submitted ||
            status == QuotationApprovalStatuses.UnderReview;

        public static bool CanRequestRevision(string status) =>
            status == QuotationApprovalStatuses.Submitted ||
            status == QuotationApprovalStatuses.UnderReview ||
            string.Equals(status, "Pending Approval", StringComparison.OrdinalIgnoreCase);
            
        public static bool CanReopen(string status) =>
            status == QuotationApprovalStatuses.Cancelled ||
            status == QuotationApprovalStatuses.Rejected;
    }

    public static class QuotationApprovalPriorityRules
    {
        public static bool IsValid(string priority) => QuotationApprovalPriorities.All.Contains(priority);

        public static string? Normalize(string? priority)
        {
            if (string.IsNullOrWhiteSpace(priority))
                return null;
            return QuotationApprovalPriorities.All.FirstOrDefault(x => string.Equals(x, priority.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }

    public static class QuotationApprovalLevelRules
    {
        public static bool IsValid(string level) => QuotationApprovalLevels.All.Contains(level);

        public static string? Normalize(string? level)
        {
            if (string.IsNullOrWhiteSpace(level))
                return null;
            return QuotationApprovalLevels.All.FirstOrDefault(x => string.Equals(x, level.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }
}
