namespace ERP.Domain.Sales
{
    public class AdvancePayment
    {
        public int Id { get; set; }

        public string PaymentNumber { get; set; } = string.Empty;

        public string CustomerId { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public int? SalesOrderId { get; set; }

        public SalesOrder? SalesOrder { get; set; }

        public string SalesOrderNumber { get; set; } = string.Empty;

        public int? QuotationId { get; set; }

        public string QuotationNumber { get; set; } = string.Empty;

        public DateOnly PaymentDate { get; set; }

        public string PaymentMode { get; set; } = AdvancePaymentModes.BankTransfer;

        public string ReferenceNumber { get; set; } = string.Empty;

        public string Currency { get; set; } = "INR";

        public decimal ExchangeRate { get; set; } = 1m;

        public decimal AdvanceAmount { get; set; }

        public decimal AppliedAmount { get; set; }

        public decimal RemainingAmount { get; set; }

        public decimal RefundedAmount { get; set; }

        public decimal ForfeitedAmount { get; set; }

        public int? BankAccountId { get; set; }

        public ERP.Domain.Accounting.BankAccount? BankAccount { get; set; }

        public string ReconciliationStatus { get; set; } = "Unreconciled";

        public DateTimeOffset? BankReconciledOn { get; set; }

        public string? BankStatementReference { get; set; }

        public string PlaceOfSupply { get; set; } = "Maharashtra";

        public string? RefundReferenceNumber { get; set; }

        public string? RefundProcessedBy { get; set; }

        public DateTimeOffset? RefundProcessedOn { get; set; }

        public string Status { get; set; } = AdvancePaymentStatuses.Draft;

        public string Remarks { get; set; } = string.Empty;

        public string? AttachmentName { get; set; }

        public string? VerifiedBy { get; set; }

        public DateTimeOffset? VerifiedOn { get; set; }

        public string? ReceivedBy { get; set; }

        public DateTimeOffset? ReceivedOn { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTimeOffset CreatedDate { get; set; }

        public string UpdatedBy { get; set; } = string.Empty;

        public DateTimeOffset UpdatedDate { get; set; }

        public bool IsDeleted { get; set; }

        public ICollection<AdvancePaymentApplication> Applications { get; set; } =
            new List<AdvancePaymentApplication>();

        public ICollection<AdvancePaymentTimeline> Timeline { get; set; } =
            new List<AdvancePaymentTimeline>();

        public ICollection<AdvancePaymentReceiptVoucher> ReceiptVouchers { get; set; } =
            new List<AdvancePaymentReceiptVoucher>();

        public ICollection<AdvancePaymentRefundVoucher> RefundVouchers { get; set; } =
            new List<AdvancePaymentRefundVoucher>();
    }
}
