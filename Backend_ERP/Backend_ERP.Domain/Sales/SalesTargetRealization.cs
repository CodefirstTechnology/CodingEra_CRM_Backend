using System;

namespace ERP.Domain.Sales
{
    public class SalesTargetRealization
    {
        public int Id { get; set; }

        public string TenantId { get; set; } = "default";

        public int SalesTargetId { get; set; }

        public SalesTarget Target { get; set; } = null!;

        // Commercial Upstream Document Foreign Keys
        public int? SalesOrderId { get; set; }

        public int? AdvancePaymentId { get; set; }

        public int? SalesInvoiceId { get; set; }

        public int? InvoiceId
        {
            get => SalesInvoiceId;
            set => SalesInvoiceId = value;
        }

        public int? QuotationId { get; set; }

        // Realization Classification (Values: BookingNet, CashCollected, BilledDispatched, PipelineWeighted, ManualAdjustment)
        public string RealizationType { get; set; } = SalesTargetRealizationTypes.BookingNet;

        public string TransactionType
        {
            get => RealizationType;
            set => RealizationType = value;
        }

        // Realization Metrics (in Target Base Currency: INR)
        public decimal RealizedAmount { get; set; }

        public decimal RealizedQuantity { get; set; } = 0m;

        public decimal MarginContributionAmount { get; set; } = 0m;

        // Commercial Multi-Currency Context
        public string SourceCurrency { get; set; } = "INR";

        public decimal SourceAmount { get; set; } = 0m;

        public decimal LockedFXRate { get; set; } = 1.000000m;

        // Compensating Reversals & Clawback Audit
        public bool IsReversal { get; set; } = false;

        public int? ReversalOfRealizationId { get; set; }

        public string ReversalReason { get; set; } = string.Empty;

        public DateTimeOffset TransactionDate { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset AppliedOn { get; set; } = DateTimeOffset.UtcNow;

        public string AppliedBy { get; set; } = "SYSTEM_EVENT";
    }
}
