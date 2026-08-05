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
    }
}
