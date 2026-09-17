namespace ERP.Domain.Sales
{
    public class SalespersonPerformanceSnapshot
    {
        public int Id { get; set; }
        public int SalesPersonUserId { get; set; }
        public string SalesPersonName { get; set; } = string.Empty;
        public string? Branch { get; set; }
        public string? SalesTeam { get; set; }
        public int FinancialYear { get; set; }
        public string PeriodKey { get; set; } = string.Empty; // e.g., '2026-M09' or '2026-Q2'
        public int Rank { get; set; }
        public decimal WeightedScore { get; set; }
        public decimal AttainmentPercentage { get; set; }
        public decimal TotalAchievedValue { get; set; }
        public decimal TotalTargetValue { get; set; }
        public decimal CalculatedCommission { get; set; }
        public string ActiveBadgesJson { get; set; } = "[]";
        public DateTimeOffset SnapshotDate { get; set; } = DateTimeOffset.UtcNow;
    }
}
