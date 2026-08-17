namespace ERP.Domain.Procurement
{
    public enum PurchaseRequisitionStatus
    {
        Draft,
        Submitted,
        Approved,
        ConvertedToRFQ,
        Closed,
        Rejected
    }

    public enum PurchaseRequisitionPriority
    {
        Low,
        Normal,
        High,
        Urgent
    }

    public enum RFQStatus
    {
        Draft,
        Sent,
        VendorResponsesReceived,
        ComparisonReady,
        Awarded,
        Closed,
        Cancelled
    }

    public enum RFQVendorStatus
    {
        Pending,
        Responded,
        Declined
    }
}
