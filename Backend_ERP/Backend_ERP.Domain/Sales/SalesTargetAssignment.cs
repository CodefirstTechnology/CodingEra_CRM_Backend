namespace ERP.Domain.Sales
{
    public class SalesTargetAssignment
    {
        public int Id { get; set; }

        public int SalesTargetId { get; set; }

        public SalesTarget SalesTarget { get; set; } = null!;

        public int SalesPersonUserId { get; set; }

        public decimal AssignedTarget { get; set; }

        public decimal AchievedValue { get; set; }

        public decimal AchievementPercentage { get; set; }

        public string Status { get; set; } = SalesTargetStatuses.Draft;

        public string Remarks { get; set; } = string.Empty;

        public string AssignedBy { get; set; } = string.Empty;

        public DateTimeOffset AssignedDate { get; set; }
    }
}
