namespace ERP.Domain.Sales
{
    public static class SalesTargetStatuses
    {
        public const string Draft = "Draft";
        public const string PendingApproval = "PendingApproval";
        public const string Approved = "Approved";
        public const string Active = "Active";
        public const string Completed = "Completed";
        public const string Expired = "Expired";
        public const string Cancelled = "Cancelled";
        public const string Closed = "Closed";
        public const string Revised = "Revised";
        public const string Superseded = "Superseded";

        public static readonly string[] All =
        [
            Draft,
            PendingApproval,
            Approved,
            Active,
            Completed,
            Expired,
            Cancelled,
            Closed,
            Revised,
            Superseded
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
        public const string Volume = "Volume";
        public const string GrossMargin = "GrossMargin";
        public const string Composite = "Composite";
        public const string SalesOrder = "SalesOrder";
        public const string Quotation = "Quotation";
        public const string ProformaInvoice = "ProformaInvoice";
        public const string CustomerAcquisition = "CustomerAcquisition";
        public const string Collection = "Collection";
        public const string Conversion = "Conversion";

        public static readonly string[] All =
        [
            Revenue,
            Volume,
            GrossMargin,
            Composite,
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
        public const string Territory = "Territory";
        public const string RegionalManager = "RegionalManager";
        public const string CompanyWide = "CompanyWide";

        public static readonly string[] All =
        [
            IndividualSalesperson,
            SalesTeam,
            Branch,
            Territory,
            RegionalManager,
            CompanyWide
        ];
    }

    public static class SalesTargetRealizationTypes
    {
        public const string BookingNet = "BookingNet";
        public const string CashCollected = "CashCollected";
        public const string BilledDispatched = "BilledDispatched";
        public const string PipelineWeighted = "PipelineWeighted";
        public const string ManualAdjustment = "ManualAdjustment";

        public static readonly string[] All =
        [
            BookingNet,
            CashCollected,
            BilledDispatched,
            PipelineWeighted,
            ManualAdjustment
        ];
    }

    public static class SalesTargetProgressActions
    {
        public const string ManualUpdate = "ManualUpdate";
        public const string AutoRecalc = "AutoRecalc";
        public const string Activate = "Activate";
        public const string Complete = "Complete";
        public const string Duplicate = "Duplicate";
        public const string Revise = "Revise";
        public const string Realization = "Realization";
        public const string Reversal = "Reversal";
    }
}
