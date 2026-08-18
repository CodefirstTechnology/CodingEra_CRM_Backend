namespace ERP.Domain.Procurement
{
    public enum WarehouseStatus
    {
        Active,
        Inactive,
        Maintenance
    }

    public enum StockTxnType
    {
        StockIn,
        StockOut,
        PurchaseReceipt,
        ProductionConsumption,
        Dispatch,
        Adjustment,
        TransferIn,
        TransferOut
    }

    public enum StockReferenceType
    {
        GRN,
        PO,
        ProductionOrder,
        Dispatch,
        Transfer,
        Verification,
        Manual,
        None
    }

    public enum BatchStatus
    {
        Active,
        ExpiringSoon,
        Expired,
        Consumed
    }

    public enum TransferStatus
    {
        Draft,
        Approved,
        Transferred,
        Completed,
        Cancelled
    }

    public enum AlertPriority
    {
        Critical,
        Warning,
        Normal
    }

    public enum VerificationStatus
    {
        Scheduled,
        InProgress,
        Completed,
        Adjusted,
        Cancelled
    }

    public enum FgDispatchStatus
    {
        NotReady,
        Reserved,
        ReadyToDispatch,
        Dispatched,
        Partial
    }

    public enum StockAgeBand
    {
        Band0To30,
        Band31To60,
        Band61To90,
        Band90Plus
    }
}
