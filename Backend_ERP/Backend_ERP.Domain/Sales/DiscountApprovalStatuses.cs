namespace ERP.Domain.Sales
{
    public static class DiscountApprovalStatuses
    {
        public const string Pending = "Pending";
        public const string UnderReview = "UnderReview";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
        public const string Returned = "Returned";
        public const string Cancelled = "Cancelled";

        public static readonly string[] All =
        [
            Pending,
            UnderReview,
            Approved,
            Rejected,
            Returned,
            Cancelled
        ];
    }

    public static class DiscountApprovalPriorities
    {
        public const string Normal = "Normal";
        public const string High = "High";
        public const string Urgent = "Urgent";

        public static readonly string[] All =
        [
            Normal,
            High,
            Urgent
        ];
    }

    public static class DiscountApprovalLevels
    {
        public const string Auto = "Auto";
        public const string Manager = "Manager";
        public const string Admin = "Admin";
        public const string Escalated = "Escalated";

        public static readonly string[] All =
        [
            Auto,
            Manager,
            Admin,
            Escalated
        ];
    }

    public static class DiscountApprovalSourceTypes
    {
        public const string Quotation = "Quotation";
        public const string SalesOrder = "SalesOrder";

        public static readonly string[] All =
        [
            Quotation,
            SalesOrder
        ];
    }

    public static class DiscountApprovalHistoryActions
    {
        public const string Created = "Created";
        public const string Updated = "Updated";
        public const string Submitted = "Submitted";
        public const string UnderReview = "UnderReview";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
        public const string Returned = "Returned";
        public const string Cancelled = "Cancelled";
        public const string Resubmitted = "Resubmitted";
        public const string CommentAdded = "CommentAdded";
        public const string Deleted = "Deleted";
    }
}
