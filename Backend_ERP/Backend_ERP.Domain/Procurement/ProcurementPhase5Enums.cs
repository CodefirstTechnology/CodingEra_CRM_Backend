namespace ERP.Domain.Procurement
{
    public enum GoodsReceiptStatus
    {
        Draft = 0,
        Submitted = 1,
        Completed = 2,
        Cancelled = 3,
        Quarantined = 4,
        QcCompleted = 5,
        Posted = 6
    }

    public enum QualityInspectionStatus
    {
        Pending = 0,
        UnderInspection = 1,
        Quarantined = 2,
        Passed = 3,
        AcceptedWithDeviation = 4,
        Rejected = 5,
        Rework = 6
    }

    public enum ThreeWayMatchStatus
    {
        Pending = 0,
        Matched = 1,
        PriceVarianceHold = 2,
        QuantityVarianceHold = 3,
        TaxVarianceHold = 4,
        Blocked = 5,
        ManuallyApproved = 6
    }

    public enum ThreeWayMatchOutcome
    {
        Passed = 0,
        Warning = 1,
        Failed = 2,
        Blocked = 3
    }
}
