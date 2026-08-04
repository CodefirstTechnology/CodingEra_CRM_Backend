namespace ERP.Domain.Sales
{
    public static class ProformaInvoiceStatuses
    {
        public const string Draft = "Draft";
        public const string Submitted = "Submitted";
        public const string PendingFinanceApproval = "Pending Finance Approval";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
        public const string Returned = "Returned";
        public const string Sent = "Sent";
        public const string Accepted = "Accepted";
        public const string Expired = "Expired";
        public const string Cancelled = "Cancelled";
        public const string Converted = "Converted";

        public static readonly string[] All =
        [
            Draft,
            Submitted,
            PendingFinanceApproval,
            Approved,
            Rejected,
            Returned,
            Sent,
            Accepted,
            Expired,
            Cancelled,
            Converted
        ];
    }

    public static class ProformaApprovalDecisions
    {
        public const string Approve = "Approve";
        public const string Reject = "Reject";
        public const string Return = "Return";
        public const string Submit = "Submit";
    }

    public static class ProformaApprovalLevels
    {
        public const string Manager = "Manager";
        public const string Finance = "Finance";
    }
}
