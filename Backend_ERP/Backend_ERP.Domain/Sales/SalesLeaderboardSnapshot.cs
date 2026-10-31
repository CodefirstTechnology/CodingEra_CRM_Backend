using System;

namespace ERP.Domain.Sales
{
    public class SalesLeaderboardSnapshot
    {
        public int Id { get; set; }
        public string TenantId { get; set; } = "default";
        public int FinancialYear { get; set; } = 2026;
        public string PeriodKey { get; set; } = string.Empty; // "2026-M10", "2026-Q3"
        public string PartitionType { get; set; } = "Global"; // Global, Territory, Team
        public string PartitionValue { get; set; } = "ALL";

        public int SalesPersonUserId { get; set; }
        public string SalesPersonName { get; set; } = string.Empty;
        public int Rank { get; set; }
        public decimal WeightedScore { get; set; }
        public decimal AttainmentPercentage { get; set; }
        public decimal TotalBookedRevenue { get; set; }
        public decimal TotalCashCollected { get; set; }
        public decimal GrossMarginPercentage { get; set; }
        public string TierBadge { get; set; } = "Silver";

        public DateTimeOffset SnapshotDate { get; set; } = DateTimeOffset.UtcNow;
    }
}

