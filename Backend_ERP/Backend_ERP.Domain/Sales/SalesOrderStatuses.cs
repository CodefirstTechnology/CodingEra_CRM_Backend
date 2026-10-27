namespace ERP.Domain.Sales
{
    public static class SalesOrderStatuses
    {
        public const string Draft = "Draft";
        public const string Submitted = "Submitted";
        public const string OnCreditHold = "On Credit Hold";
        public const string Confirmed = "Confirmed";
        public const string Processing = "Processing";
        public const string ReadyForDispatch = "Ready For Dispatch";
        public const string PartiallyDelivered = "Partially Delivered";
        public const string Completed = "Completed";
        public const string Closed = "Closed";
        public const string Cancelled = "Cancelled";

        public static readonly string[] All =
        [
            Draft,
            Submitted,
            OnCreditHold,
            Confirmed,
            Processing,
            ReadyForDispatch,
            PartiallyDelivered,
            Completed,
            Closed,
            Cancelled
        ];

        public static readonly string[] Lifecycle =
        [
            Draft,
            Submitted,
            Confirmed,
            Processing,
            ReadyForDispatch,
            PartiallyDelivered,
            Completed
        ];
    }

    public static class SalesOrderSourceTypes
    {
        public const string Manual = "Manual";
        public const string Quotation = "Quotation";
    }
}
