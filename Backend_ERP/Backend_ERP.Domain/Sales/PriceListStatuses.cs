namespace ERP.Domain.Sales
{
    public static class PriceListStatuses
    {
        public const string Draft = "Draft";
        public const string Active = "Active";
        public const string Expired = "Expired";
        public const string Archived = "Archived";

        public static readonly string[] All =
        [
            Draft,
            Active,
            Expired,
            Archived
        ];
    }

    public static class PriceListCustomerCategories
    {
        public const string Standard = "Standard";
        public const string Premium = "Premium";
        public const string Enterprise = "Enterprise";
        public const string Strategic = "Strategic";
        public const string Distributor = "Distributor";
        public const string Retail = "Retail";
        public const string Wholesale = "Wholesale";
        public const string OEM = "OEM";
        public const string Government = "Government";

        public static readonly string[] All =
        [
            Standard,
            Premium,
            Enterprise,
            Strategic,
            Distributor,
            Retail,
            Wholesale,
            OEM,
            Government
        ];
    }

    public static class PriceListHistoryActions
    {
        public const string Created = "Created";
        public const string Updated = "Updated";
        public const string Activated = "Activated";
        public const string Expired = "Expired";
        public const string Archived = "Archived";
        public const string Cloned = "Cloned";
        public const string Deleted = "Deleted";
    }
}
