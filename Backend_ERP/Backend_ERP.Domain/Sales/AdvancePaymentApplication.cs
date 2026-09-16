namespace ERP.Domain.Sales
{
    public class AdvancePaymentApplication
    {
        public int Id { get; set; }

        public int AdvancePaymentId { get; set; }

        public AdvancePayment AdvancePayment { get; set; } = null!;

        public int SalesOrderId { get; set; }

        public string SalesOrderNumber { get; set; } = string.Empty;

        public decimal ApplyAmount { get; set; }

        public bool IsReversal { get; set; } = false;

        public string? ReversalReason { get; set; }

        public int? OriginalApplicationId { get; set; }

        public AdvancePaymentApplication? OriginalApplication { get; set; }

        public string Remarks { get; set; } = string.Empty;

        public string AppliedBy { get; set; } = string.Empty;

        public DateTimeOffset AppliedOn { get; set; }
    }
}
