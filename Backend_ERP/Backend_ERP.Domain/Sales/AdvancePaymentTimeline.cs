namespace ERP.Domain.Sales
{
    public class AdvancePaymentTimeline
    {
        public int Id { get; set; }

        public int AdvancePaymentId { get; set; }

        public AdvancePayment AdvancePayment { get; set; } = null!;

        public string Action { get; set; } = string.Empty;

        public string Remarks { get; set; } = string.Empty;

        public string PerformedBy { get; set; } = string.Empty;

        public DateTimeOffset PerformedOn { get; set; }
    }
}
