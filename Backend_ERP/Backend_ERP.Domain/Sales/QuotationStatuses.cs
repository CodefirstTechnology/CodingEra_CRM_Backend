namespace ERP.Domain.Sales
{
    public static class QuotationStatuses
    {
        public const string Draft = "Draft";
        public const string Sent = "Sent";
        public const string Approved = "Approved"; // Client Accepted
        public const string ConvertedToSO = "ConvertedToSO";
        public const string Rejected = "Rejected";
        public const string Expired = "Expired";
        public const string Revised = "Revised";

        public static readonly string[] All =
        [
            Draft,
            Sent,
            Approved,
            ConvertedToSO,
            Rejected,
            Expired,
            Revised
        ];
    }
}
