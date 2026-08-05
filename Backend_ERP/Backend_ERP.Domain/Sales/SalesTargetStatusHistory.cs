namespace ERP.Domain.Sales
{
    public class SalesTargetStatusHistory
    {
        public int Id { get; set; }

        public int SalesTargetId { get; set; }

        public SalesTarget SalesTarget { get; set; } = null!;

        public string? OldStatus { get; set; }

        public string NewStatus { get; set; } = string.Empty;

        public string Remarks { get; set; } = string.Empty;

        public string ChangedBy { get; set; } = string.Empty;

        public DateTimeOffset ChangedOn { get; set; }
    }
}
