using System;

namespace ERP.Domain.Sales
{
    public class ProformaInvoiceSettlementAllocation
    {
        public int Id { get; set; }

        public string TenantId { get; set; } = "default";

        public int ProformaInvoiceId { get; set; }

        public ProformaInvoice ProformaInvoice { get; set; } = null!;

        public int AdvancePaymentId { get; set; }

        public AdvancePayment AdvancePayment { get; set; } = null!;

        public decimal AllocatedAmount { get; set; }

        public decimal ExchangeRateAtSettlement { get; set; } = 1.0000m;

        public decimal BaseCurrencyAllocatedAmount { get; set; }

        public string ReceiptVoucherNumber { get; set; } = string.Empty;

        public DateTimeOffset AllocatedOn { get; set; } = DateTimeOffset.UtcNow;

        public string AllocatedBy { get; set; } = string.Empty;

        public string Remarks { get; set; } = string.Empty;

        public bool IsReversed { get; set; } = false;

        public DateTimeOffset? ReversedOn { get; set; }
    }
}
