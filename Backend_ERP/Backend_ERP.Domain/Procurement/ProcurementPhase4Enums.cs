namespace ERP.Domain.Procurement
{
    public enum PurchaseOrderStatus
    {
        Draft = 10,
        Submitted = 20,
        PendingApproval = 30,
        Approved = 40,
        Ordered = 45,
        SentToVendor = 50,
        Acknowledged = 60,
        PartiallyReceived = 70,
        FullyReceived = 80,
        Completed = 85,
        Closed = 90,
        Cancelled = 95,
        Rejected = 96,
        RevisionRequired = 84,
        Revised = 86,
        Superseded = 99
    }

    public enum PurchaseOrderApprovalTier
    {
        None = 0,
        Tier1_PlantManager = 1,
        Tier2_OperationsHead = 2,
        Tier3_VpSupplyChain = 3,
        Tier4_CfoMd = 4
    }

    public enum PurchaseOrderApprovalDecision
    {
        Pending,
        Approved,
        Rejected,
        Returned
    }

    public enum POItemType
    {
        Inventory,
        Consumable,
        Service,
        Asset
    }

    public enum PurchaseOrderLineStatus
    {
        PendingReceipt,
        PartiallyReceived,
        FullyReceived,
        Closed,
        Cancelled
    }

    public enum POAmendmentType
    {
        Administrative,
        Commercial
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
