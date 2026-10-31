using System;
using System.Collections.Generic;

namespace ERP.Domain.Sales
{
    public class SalespersonPerformance
    {
        public int Id { get; set; }
        public string TenantId { get; set; } = "default";
        public int SalesPersonUserId { get; set; }
        public string SalesPersonName { get; set; } = string.Empty;
        public string? SalesTeam { get; set; }
        public string? Branch { get; set; }
        public string? RegionalManager { get; set; }
        public int? TerritoryId { get; set; }
        public int FinancialYear { get; set; } = 2026;
        public string PeriodType { get; set; } = "FiscalYear";

        // Quota & Target Commitments
        public decimal TotalTargetValue { get; set; } = 0m;

        // Multi-Stream Commercial Realization Accumulators
        public decimal TotalBookedRevenue { get; set; } = 0m;
        public decimal TotalAchievedValue
        {
            get => TotalBookedRevenue;
            set => TotalBookedRevenue = value;
        }

        public decimal TotalCashCollected { get; set; } = 0m;
        public decimal TotalDispatchedRevenue { get; set; } = 0m;
        public decimal TotalGrossMarginAmount { get; set; } = 0m;
        public decimal GrossMarginPercentage { get; set; } = 0m;
        public decimal AttainmentPercentage { get; set; } = 0m;
        public decimal PacingIndex { get; set; } = 0m;

        // Funnel & Velocity Telemetry
        public int ConfirmedOrderCount { get; set; } = 0;
        public int TotalQuotationCount { get; set; } = 0;
        public decimal ConversionRate { get; set; } = 0m;
        public decimal AvgDealVelocityDays { get; set; } = 0m;
        public decimal DiscountDisciplineScore { get; set; } = 100m;

        // Gamification & Rankings
        public decimal WeightedScore { get; set; } = 0m;
        public int CurrentRank { get; set; } = 0;
        public int PreviousRank { get; set; } = 0;
        public int RankDelta => PreviousRank > 0 ? (PreviousRank - CurrentRank) : 0;
        public string TierBadge { get; set; } = "Silver"; // Silver, Gold, Platinum, Diamond
        public int WinningStreakMonths { get; set; } = 0;
        public string ActiveBadgesJson { get; set; } = "[]";

        // Multi-Tier Commission & Incentive Totals
        public decimal TotalAccruedCommission { get; set; } = 0m;
        public decimal CalculatedCommission
        {
            get => TotalAccruedCommission;
            set => TotalAccruedCommission = value;
        }
        public decimal TotalClawbackCommission { get; set; } = 0m;
        public decimal TotalPaidCommission { get; set; } = 0m;
        public decimal NetPayableCommission => TotalAccruedCommission - TotalClawbackCommission - TotalPaidCommission;

        public string Status { get; set; } = "Active";
        public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedDate { get; set; } = DateTimeOffset.UtcNow;

        // PostgreSQL xmin Concurrency Token
        public uint Version { get; set; }

        // Navigation Collections
        public ICollection<SalesCommissionLedger> CommissionEntries { get; set; } = new List<SalesCommissionLedger>();
        public ICollection<SalesRepBadge> Badges { get; set; } = new List<SalesRepBadge>();
    }
}
