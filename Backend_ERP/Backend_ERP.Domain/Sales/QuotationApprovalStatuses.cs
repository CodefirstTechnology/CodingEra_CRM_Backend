namespace ERP.Domain.Sales
{
    public static class QuotationApprovalStatuses
    {
        public const string Draft = "Draft";
        public const string Submitted = "Submitted";
        public const string UnderReview = "UnderReview";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
        public const string Returned = "Returned";
        public const string Cancelled = "Cancelled";
        public const string RevisionRequired = "RevisionRequired";
        public const string Reopened = "Reopened";

        public static readonly string[] All =
        [
            Draft,
            Submitted,
            UnderReview,
            Approved,
            Rejected,
            Returned,
            Cancelled,
            RevisionRequired,
            Reopened
        ];
    }

    public static class QuotationApprovalPriorities
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

    public static class QuotationApprovalLevels
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

    public static class QuotationApprovalHistoryActions
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
        public const string RevisionRequired = "RevisionRequired";
        public const string Reopened = "Reopened";
        public const string CommentAdded = "CommentAdded";
        public const string Deleted = "Deleted";
    }
}
