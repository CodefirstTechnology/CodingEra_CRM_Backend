namespace ERP.Domain.Sales
{
    public class ProformaInvoice
    {
        public int Id { get; set; }

        public string TenantId { get; set; } = "default";

        public string PiNumber { get; set; } = string.Empty;

        public DateOnly InvoiceDate { get; set; }

        public DateOnly ValidUntil { get; set; }

        public string CustomerId { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string CustomerGstin { get; set; } = string.Empty;

        public string CustomerCategory { get; set; } = "Standard";

        public string PlaceOfSupplyStateCode { get; set; } = "27";

        public string ContactPerson { get; set; } = string.Empty;

        public string BillingAddress { get; set; } = string.Empty;

        public string ShippingAddress { get; set; } = string.Empty;

        public string Incoterm { get; set; } = "EXW";

        public int SalesPersonUserId { get; set; }

        public string SalesPerson { get; set; } = string.Empty;

        public string Currency { get; set; } = "INR";

        public decimal ExchangeRate { get; set; } = 1m;

        public DateOnly? ExchangeRateDate { get; set; }

        public decimal BaseCurrencyEquivalentAmount { get; set; } = 0m;

        public int? QuotationId { get; set; }

        public string QuotationNumber { get; set; } = string.Empty;

        public int? SalesOrderId { get; set; }

        public SalesOrder? SalesOrder { get; set; }

        public string SalesOrderNumber { get; set; } = string.Empty;

        public string PaymentTerms { get; set; } = string.Empty;

        public string DeliveryTerms { get; set; } = string.Empty;

        public string CustomerNotes { get; set; } = string.Empty;

        public string InternalNotes { get; set; } = string.Empty;

        public decimal Subtotal { get; set; }

        public decimal DiscountTotal { get; set; }

        public decimal TaxTotal { get; set; }

        public decimal GrandTotal { get; set; }

        public decimal AdvanceReceivedAmount { get; set; } = 0m;

        public string PaymentStatus { get; set; } = "Unpaid";

        public string BillingType { get; set; } = "Full";

        public decimal? MilestonePercentage { get; set; }

        public bool RequiresFinanceCreditReview { get; set; } = false;

        public string? CreditReviewReason { get; set; }

        public string? ApprovalToken { get; set; }

        public string? ApprovedBy { get; set; }

        public DateTimeOffset? ApprovedDate { get; set; }

        public string Status { get; set; } = ProformaInvoiceStatuses.Draft;

        public string Remarks { get; set; } = string.Empty;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTimeOffset CreatedDate { get; set; }

        public string UpdatedBy { get; set; } = string.Empty;

        public DateTimeOffset UpdatedDate { get; set; }

        public bool IsDeleted { get; set; }

        public string? ConvertedInvoiceNumber { get; set; }

        public DateTimeOffset? ConvertedOn { get; set; }

        /// <summary>True once production/MPS work order has been released against this PI.</summary>
        public bool IsProductionReleased { get; set; } = false;

        /// <summary>UTC timestamp when production was released.</summary>
        public DateTimeOffset? ProductionReleasedOn { get; set; }

        public string? ProductionReleasedBy { get; set; }

        /// <summary>True once dispatch staging & gate-pass has been released against this PI.</summary>
        public bool IsDispatchReleased { get; set; } = false;

        /// <summary>UTC timestamp when dispatch was released.</summary>
        public DateTimeOffset? DispatchReleasedOn { get; set; }

        public string? DispatchReleasedBy { get; set; }

        /// <summary>PostgreSQL xmin concurrency token for optimistic locking.</summary>
        public uint ConcurrencyVersion { get; set; }

        public ICollection<ProformaInvoiceItem> Items { get; set; } = new List<ProformaInvoiceItem>();

        public ICollection<ProformaInvoiceStatusHistory> StatusHistory { get; set; } = new List<ProformaInvoiceStatusHistory>();

        public ICollection<ProformaInvoiceApprovalHistory> ApprovalHistory { get; set; } = new List<ProformaInvoiceApprovalHistory>();

        public ICollection<ProformaInvoiceSettlementAllocation> SettlementAllocations { get; set; } = new List<ProformaInvoiceSettlementAllocation>();
    }
}
