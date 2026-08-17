namespace ERP.Domain.Procurement
{
    public enum PurchaseOrderStatus
    {
        Draft,
        Submitted,
        Approved,
        Ordered,
        PartiallyReceived,
        Completed,
        Cancelled,
        Rejected,
        RevisionRequired
    }

    public enum PurchaseOrderPriority
    {
        Low,
        Normal,
        High,
        Urgent
    }

    public enum PurchaseOrderSourceType
    {
        Manual,
        SalesOrder,
        Requisition
    }
}
