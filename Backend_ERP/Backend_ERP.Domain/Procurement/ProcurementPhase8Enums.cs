namespace ERP.Domain.Procurement
{
    public enum PurchaseBillStatus
    {
        Draft,
        PendingApproval,
        VarianceHold,
        Approved,
        Posted,
        Paid,
        Void
    }

    public enum PurchaseBillPaymentStatus
    {
        Unpaid,
        PartiallyPaid,
        Paid
    }
}
