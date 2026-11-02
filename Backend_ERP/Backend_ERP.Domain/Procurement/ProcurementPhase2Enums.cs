namespace ERP.Domain.Procurement
{
    public enum PurchaseRequisitionStatus
    {
        Draft,
        Submitted,
        PendingApproval,
        Approved,
        RfqIssued,
        ConvertedToRFQ,
        PartiallyOrdered,
        FullyOrdered,
        Closed,
        Rejected,
        Cancelled
    }

    public enum PurchaseRequisitionPriority
    {
        Low,
        Normal,
        High,
        Urgent
    }

    public enum PurchaseRequisitionLineStatus
    {
        PendingSourcing,
        RfqIssued,
        UnderEvaluation,
        PartiallyOrdered,
        FullyOrdered,
        Closed,
        Cancelled
    }

    public enum RFQSourcingType
    {
        SingleSource,
        OpenCompetitive,
        RestrictedAVL
    }

    public enum RFQStatus
    {
        Draft,
        Published,
        Sent,
        SentToVendors,
        VendorResponsesReceived,
        ComparisonReady,
        UnderComparison,
        Awarded,
        Closed,
        Cancelled
    }

    public enum RFQVendorStatus
    {
        Pending,
        Invited,
        Acknowledged,
        Responded,
        BidSubmitted,
        Declined
    }
}
