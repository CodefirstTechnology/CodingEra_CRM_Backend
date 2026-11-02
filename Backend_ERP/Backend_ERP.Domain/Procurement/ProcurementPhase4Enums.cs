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
        Tier1 = 1,
        Tier1_PlantManager = 1,
        Tier2 = 2,
        Tier2_OperationsHead = 2,
        Tier3 = 3,
        Tier3_VpSupplyChain = 3,
        Tier4 = 4,
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
        Inventory = 0,
        Goods = 0,
        Consumable = 1,
        Service = 2,
        Asset = 3
    }

    public enum PurchaseOrderLineStatus
    {
        PendingReceipt = 0,
        Pending = 0,
        PartiallyReceived = 1,
        FullyReceived = 2,
        Closed = 3,
        Cancelled = 4
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
