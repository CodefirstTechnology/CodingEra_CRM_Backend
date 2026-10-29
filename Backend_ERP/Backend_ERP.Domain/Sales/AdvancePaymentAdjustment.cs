using System;

namespace ERP.Domain.Sales
{
    public class AdvancePaymentAdjustment
    {
        public int Id { get; set; }

        public string TenantId { get; set; } = "default";

        public int AdvancePaymentId { get; set; }

        public AdvancePayment AdvancePayment { get; set; } = null!;

        public int? SalesInvoiceId { get; set; }

        public string InvoiceNumber { get; set; } = string.Empty;

        public decimal AdjustedAmount { get; set; }

        public DateOnly AdjustmentDate { get; set; }

        public decimal ExchangeRateAtAdjustment { get; set; } = 1.0000m;

        public decimal RealizedFxGainLoss { get; set; } = 0.00m;

        public string? Gstr1VoucherRef { get; set; }

        public string Remarks { get; set; } = string.Empty;

        public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;
    }
}
