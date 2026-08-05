namespace ERP.Domain.Sales
{
    public static class AdvancePaymentStatuses
    {
        public const string Draft = "Draft";
        public const string Submitted = "Submitted";
        public const string FinanceVerification = "FinanceVerification";
        public const string Received = "Received";
        public const string PartiallyApplied = "PartiallyApplied";
        public const string FullyApplied = "FullyApplied";
        public const string Cancelled = "Cancelled";
        public const string Rejected = "Rejected";

        public static readonly string[] All =
        [
            Draft,
            Submitted,
            FinanceVerification,
            Received,
            PartiallyApplied,
            FullyApplied,
            Cancelled,
            Rejected
        ];
    }

    public static class AdvancePaymentModes
    {
        public const string Cash = "Cash";
        public const string Cheque = "Cheque";
        public const string BankTransfer = "BankTransfer";
        public const string NEFT = "NEFT";
        public const string RTGS = "RTGS";
        public const string UPI = "UPI";
        public const string Card = "Card";

        public static readonly string[] All =
        [
            Cash,
            Cheque,
            BankTransfer,
            NEFT,
            RTGS,
            UPI,
            Card
        ];
    }

    public static class AdvancePaymentTimelineActions
    {
        public const string Created = "Created";
        public const string Updated = "Updated";
        public const string Submitted = "Submitted";
        public const string Verified = "Verified";
        public const string Received = "Received";
        public const string Rejected = "Rejected";
        public const string Cancelled = "Cancelled";
        public const string Applied = "Applied";
        public const string Deleted = "Deleted";
    }
}
