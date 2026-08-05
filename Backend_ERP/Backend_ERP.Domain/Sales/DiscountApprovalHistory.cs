namespace ERP.Domain.Sales
{
    public class DiscountApprovalHistory
    {
        public int Id { get; set; }

        public int DiscountApprovalId { get; set; }

        public DiscountApproval DiscountApproval { get; set; } = null!;

        public string Action { get; set; } = string.Empty;

        public string OldStatus { get; set; } = string.Empty;

        public string NewStatus { get; set; } = string.Empty;

        public string Remarks { get; set; } = string.Empty;

        public string PerformedBy { get; set; } = string.Empty;

        public DateTimeOffset PerformedOn { get; set; }
    }
}
