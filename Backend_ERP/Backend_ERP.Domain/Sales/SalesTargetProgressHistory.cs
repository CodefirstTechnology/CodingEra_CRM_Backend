namespace ERP.Domain.Sales
{
    public class SalesTargetProgressHistory
    {
        public int Id { get; set; }

        public int SalesTargetId { get; set; }

        public SalesTarget SalesTarget { get; set; } = null!;

        public decimal OldAchievedValue { get; set; }

        public decimal NewAchievedValue { get; set; }

        public decimal AchievementPercentage { get; set; }

        public string Action { get; set; } = string.Empty;

        public string Remarks { get; set; } = string.Empty;

        public string UpdatedBy { get; set; } = string.Empty;

        public DateTimeOffset UpdatedOn { get; set; }
    }
}
