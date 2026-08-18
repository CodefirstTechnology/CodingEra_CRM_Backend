namespace ERP.Domain.Procurement
{
    public enum PurchaseBillStatus
    {
        Draft,
        Approved,
        Posted,
        Paid,
        Void
    }

    public enum PurchaseBillPaymentStatus
    {
        Unpaid,
        Partial,
        Paid
    }
}
