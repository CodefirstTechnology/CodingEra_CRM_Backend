using System;

namespace ERP.Domain.Sales
{
    public class SalesCommissionLedger
    {
        public int Id { get; set; }
        public string TenantId { get; set; } = "default";
        public int ProfileId { get; set; }
        public SalespersonPerformance? Profile { get; set; }

        public int SalesPersonUserId { get; set; }
        public int? IncentiveSchemeId { get; set; }
        public SalesIncentiveScheme? IncentiveScheme { get; set; }

        // Upstream Commercial Event Links
        public int? SalesOrderId { get; set; }
        public int? AdvancePaymentId { get; set; }
        public int? SalesInvoiceId { get; set; }

        public string TransactionType { get; set; } = "BookingAccrual"; // BookingAccrual, CashRelease, Clawback, PayoutDisbursement
        public decimal TransactionAmount { get; set; } = 0m;
        public decimal CommissionRate { get; set; } = 0m;
        public decimal AcceleratorMultiplier { get; set; } = 1.00m;
        public decimal AccruedCommission { get; set; } = 0m;

        // Clawback Accounting
        public bool IsClawback { get; set; } = false;
        public int? ReversalOfLedgerId { get; set; }
        public string? ClawbackReason { get; set; }

        // Governance & Audit
        public string PayoutStatus { get; set; } = "Accrued"; // Accrued, GatedPendingCash, ApprovedForPayment, Disbursed
        public string? ApprovalToken { get; set; } // HMAC-SHA256 signature
        public string? ApprovedByUserId { get; set; }
        public string? PaymentVoucherReference { get; set; }

        public DateTimeOffset AccruedDate { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? PayoutDate { get; set; }
    }
}

