namespace ERP.Domain.Accounting
{
    public enum PaymentEntryStatus
    {
        Draft = 0,
        Approved = 1,
        Posted = 2,
        Paid = 3,
        Cancelled = 4
    }

    public enum ReceiptEntryStatus
    {
        Draft = 0,
        Approved = 1,
        Posted = 2,
        Received = 3,
        Cancelled = 4
    }

    public enum PaymentMode
    {
        Cash = 0,
        BankTransfer = 1,
        Cheque = 2,
        UPI = 3,
        NEFT = 4,
        RTGS = 5,
        Card = 6
    }

    public enum GstReturnStatus
    {
        Draft = 0,
        Verified = 1,
        Filed = 2,
        Closed = 3
    }

    public enum GstTxnType
    {
        SalesInvoice = 0,
        PurchaseBill = 1,
        DebitNote = 2,
        CreditNote = 3
    }

    public enum BankReconStatus
    {
        Draft = 0,
        Verified = 1,
        Reconciled = 2,
        Closed = 3
    }

    public enum LedgerEntryType
    {
        Opening = 0,
        Invoice = 1,
        Receipt = 2,
        Payment = 3,
        DebitNote = 4,
        CreditNote = 5,
        Adjustment = 6
    }

    public enum OutstandingStatus
    {
        Open = 0,
        Partial = 1,
        Overdue = 2,
        Settled = 3
    }

    public enum PartyType
    {
        Customer = 0,
        Vendor = 1
    }
}
