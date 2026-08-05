namespace ERP.Domain.Sales
{
    public static class SalesTargetStatuses
    {
        public const string Draft = "Draft";
        public const string Active = "Active";
        public const string Completed = "Completed";
        public const string Expired = "Expired";
        public const string Cancelled = "Cancelled";

        public static readonly string[] All =
        [
            Draft,
            Active,
            Completed,
            Expired,
            Cancelled
        ];
    }

    public static class SalesTargetTypes
    {
        public const string Monthly = "Monthly";
        public const string Quarterly = "Quarterly";
        public const string HalfYearly = "HalfYearly";
        public const string Yearly = "Yearly";
        public const string CustomPeriod = "CustomPeriod";

        public static readonly string[] All =
        [
            Monthly,
            Quarterly,
            HalfYearly,
            Yearly,
            CustomPeriod
        ];
    }

    public static class SalesTargetCategories
    {
        public const string Revenue = "Revenue";
        public const string SalesOrder = "SalesOrder";
        public const string Quotation = "Quotation";
        public const string ProformaInvoice = "ProformaInvoice";
        public const string CustomerAcquisition = "CustomerAcquisition";
        public const string Collection = "Collection";
        public const string Conversion = "Conversion";

        public static readonly string[] All =
        [
            Revenue,
            SalesOrder,
            Quotation,
            ProformaInvoice,
            CustomerAcquisition,
            Collection,
            Conversion
        ];
    }

    public static class SalesTargetAssignmentTypes
    {
        public const string IndividualSalesperson = "IndividualSalesperson";
        public const string SalesTeam = "SalesTeam";
        public const string Branch = "Branch";
        public const string RegionalManager = "RegionalManager";

        public static readonly string[] All =
        [
            IndividualSalesperson,
            SalesTeam,
            Branch,
            RegionalManager
        ];
    }

    public static class SalesTargetProgressActions
    {
        public const string ManualUpdate = "ManualUpdate";
        public const string AutoRecalc = "AutoRecalc";
        public const string Activate = "Activate";
        public const string Complete = "Complete";
        public const string Duplicate = "Duplicate";
    }
}
