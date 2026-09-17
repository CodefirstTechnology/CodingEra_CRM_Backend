namespace ERP.Domain.Sales
{
    public class SalesTargetRealization
    {
        public int Id { get; set; }

        public int SalesTargetId { get; set; }

        public SalesTarget Target { get; set; } = null!;

        public int? SalesOrderId { get; set; }

        public int? InvoiceId { get; set; }

        public string TransactionType { get; set; } = "SalesOrder";

        public decimal RealizedAmount { get; set; }

        public bool IsReversal { get; set; } = false;

        public DateTimeOffset AppliedOn { get; set; } = DateTimeOffset.UtcNow;

        public string AppliedBy { get; set; } = "SYSTEM_EVENT";
    }
}
