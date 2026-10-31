using System;
using System.Collections.Generic;
using ERP.Domain.Sales;

namespace ERP.Application.Sales.Dtos
{
    public class RepOverviewCockpitDto
    {
        public int SalesPersonUserId { get; set; }
        public string SalesPersonName { get; set; } = string.Empty;
        public string? SalesTeam { get; set; }
        public string? Branch { get; set; }
        public string? RegionalManager { get; set; }
        public int? TerritoryId { get; set; }
        public int FinancialYear { get; set; }

        // Revenue & Target Attainment
        public decimal TotalTargetValue { get; set; }
        public decimal TotalAchievedValue { get; set; }
        public decimal TotalBookedRevenue { get; set; }
        public decimal TotalCashCollected { get; set; }
        public decimal TotalDispatchedRevenue { get; set; }
        public decimal AttainmentPercentage { get; set; }
        public decimal PacingIndex { get; set; }
        public decimal ExpectedAttainmentPercentage { get; set; }

        // Quality, Margin & Discipline
        public decimal GrossMarginPercentage { get; set; }
        public decimal TotalGrossMarginAmount { get; set; }
        public decimal DiscountDisciplineScore { get; set; }
        public decimal AvgDealVelocityDays { get; set; }
        public decimal ConversionRate { get; set; }
        public int ConfirmedOrderCount { get; set; }
        public int TotalQuotationCount { get; set; }

        // Gamification & Rankings
        public int CurrentRank { get; set; }
        public int PreviousRank { get; set; }
        public int RankDelta => PreviousRank > 0 && CurrentRank > 0 ? PreviousRank - CurrentRank : 0;
        public string TierBadge { get; set; } = "Silver";
        public int WinningStreakMonths { get; set; }
        public decimal WeightedScore { get; set; }
        public List<SalesRepBadgeDto> Badges { get; set; } = new();

        // Skill Radar Scores (0 to 100)
        public RadarSkillMatrixDto SkillRadar { get; set; } = new();

        // Attainment Milestones
        public List<AttainmentMilestoneDto> Milestones { get; set; } = new();

        // Commission Ledger Summary
        public decimal TotalAccruedCommission { get; set; }
        public decimal TotalClawbackCommission { get; set; }
        public decimal TotalPaidCommission { get; set; }
        public decimal NetPayableCommission { get; set; }
        public decimal PendingCashGatedCommission { get; set; }

        // Recent Commission Ledger Entries
        public List<SalesCommissionLedgerDto> RecentCommissions { get; set; } = new();
    }

    public class RadarSkillMatrixDto
    {
        public decimal QuotaAttainmentScore { get; set; }
        public decimal CashVelocityScore { get; set; }
        public decimal MarginDisciplineScore { get; set; }
        public decimal DealVelocityScore { get; set; }
        public decimal WinRateScore { get; set; }
    }

    public class AttainmentMilestoneDto
    {
        public decimal TargetPercentage { get; set; }
        public string Label { get; set; } = string.Empty;
        public bool IsAchieved { get; set; }
        public decimal AchievedValue { get; set; }
        public decimal RequiredValue { get; set; }
        public DateTimeOffset? AchievedDate { get; set; }
    }

    public class LeaderboardFilterDto
    {
        public int FinancialYear { get; set; } = 2026;
        public string? SalesTeam { get; set; }
        public string? Branch { get; set; }
        public int? TerritoryId { get; set; }
        public string Metric { get; set; } = "WeightedScore"; // WeightedScore, AchievedValue, AttainmentPercentage, GrossMarginPercentage, CashCollected
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class LeaderboardItemDto
    {
        public int Rank { get; set; }
        public int PreviousRank { get; set; }
        public int RankDelta => PreviousRank > 0 && Rank > 0 ? PreviousRank - Rank : 0;
        public int SalesPersonUserId { get; set; }
        public string SalesPersonName { get; set; } = string.Empty;
        public string? SalesTeam { get; set; }
        public string? Branch { get; set; }
        public int? TerritoryId { get; set; }
        public decimal WeightedScore { get; set; }
        public decimal TotalTargetValue { get; set; }
        public decimal TotalAchievedValue { get; set; }
        public decimal AttainmentPercentage { get; set; }
        public decimal GrossMarginPercentage { get; set; }
        public decimal TotalCashCollected { get; set; }
        public int ConfirmedOrderCount { get; set; }
        public decimal AvgDealVelocityDays { get; set; }
        public string TierBadge { get; set; } = "Silver";
        public int WinningStreakMonths { get; set; }
        public List<SalesRepBadgeDto> Badges { get; set; } = new();
    }

    public class LeaderboardPodiumDto
    {
        public List<LeaderboardItemDto> PodiumTop3 { get; set; } = new();
        public List<LeaderboardItemDto> Rankings { get; set; } = new();
        public int TotalCount { get; set; }
        public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
        public string DataSource { get; set; } = "Database"; // "Redis" or "Database"
    }

    public class SalesCommissionLedgerDto
    {
        public int Id { get; set; }
        public int ProfileId { get; set; }
        public int SalesPersonUserId { get; set; }
        public string SalesPersonName { get; set; } = string.Empty;
        public string CommercialDocumentType { get; set; } = string.Empty;
        public int CommercialDocumentId { get; set; }
        public string CommercialDocumentNumber { get; set; } = string.Empty;
        public DateTimeOffset TransactionDate { get; set; }
        public decimal DocumentAmount { get; set; }
        public decimal GrossMarginAmount { get; set; }
        public decimal GrossMarginPercentage { get; set; }
        public decimal BaseRateApplied { get; set; }
        public decimal AcceleratorMultiplier { get; set; }
        public decimal AccruedCommission { get; set; }
        public bool CashGatePassed { get; set; }
        public bool IsClawback { get; set; }
        public string? ClawbackReason { get; set; }
        public string Status { get; set; } = "Accrued";
        public string TransactionType { get; set; } = "OrderConfirmedAccrual";
        public string? Notes { get; set; }
        public int? ApprovedByUserId { get; set; }
        public DateTimeOffset? ApprovedDate { get; set; }
    }

    public class CommissionApprovalRequestDto
    {
        public int LedgerId { get; set; }
        public bool Approved { get; set; } = true;
        public string? Notes { get; set; }
    }

    public class CommissionDisbursementRequestDto
    {
        public int SalesPersonUserId { get; set; }
        public int FinancialYear { get; set; } = 2026;
        public decimal Amount { get; set; }
        public string DisbursementReference { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class IncentiveSchemeDto
    {
        public int Id { get; set; }
        public string SchemeCode { get; set; } = string.Empty;
        public string SchemeName { get; set; } = string.Empty;
        public int FiscalYear { get; set; }
        public decimal BaseCommissionRate { get; set; }
        public decimal GrossMarginHurdlePercentage { get; set; }
        public bool RequireAdvanceReceiptForAccrual { get; set; }
        public decimal MinCashClearancePercentage { get; set; }
        public List<IncentiveAcceleratorTier> TierAccelerators { get; set; } = new();
        public bool IsActive { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateIncentiveSchemeDto
    {
        public string SchemeCode { get; set; } = string.Empty;
        public string SchemeName { get; set; } = string.Empty;
        public int FiscalYear { get; set; } = 2026;
        public decimal BaseCommissionRate { get; set; } = 2.50m;
        public decimal GrossMarginHurdlePercentage { get; set; } = 22.00m;
        public bool RequireAdvanceReceiptForAccrual { get; set; } = true;
        public decimal MinCashClearancePercentage { get; set; } = 50.00m;
        public List<IncentiveAcceleratorTier> TierAccelerators { get; set; } = new();
        public bool IsActive { get; set; } = true;
        public string? Notes { get; set; }
    }

    public class SalesRepBadgeDto
    {
        public int Id { get; set; }
        public string BadgeCode { get; set; } = string.Empty;
        public string BadgeName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconName { get; set; } = string.Empty;
        public string BadgeCategory { get; set; } = string.Empty;
        public string? CriteriaSnapshotJson { get; set; }
        public DateTimeOffset AwardedDate { get; set; }
        public int FinancialYear { get; set; }
    }

    // Reports DTOs
    public class ExecutiveCommissionPayoutReportDto
    {
        public int FinancialYear { get; set; }
        public decimal TotalCompanyAccruedCommission { get; set; }
        public decimal TotalCompanyClawbackCommission { get; set; }
        public decimal TotalCompanyPaidCommission { get; set; }
        public decimal TotalCompanyNetPayableCommission { get; set; }
        public decimal TotalCashGatedPendingCommission { get; set; }
        public List<CommissionPayoutRowDto> Rows { get; set; } = new();
    }

    public class CommissionPayoutRowDto
    {
        public int SalesPersonUserId { get; set; }
        public string SalesPersonName { get; set; } = string.Empty;
        public string? SalesTeam { get; set; }
        public string? Branch { get; set; }
        public decimal TargetValue { get; set; }
        public decimal AchievedRevenue { get; set; }
        public decimal AttainmentPercentage { get; set; }
        public decimal GrossMarginPercentage { get; set; }
        public decimal TotalAccrued { get; set; }
        public decimal TotalClawback { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal NetPayable { get; set; }
        public decimal GatedPendingCash { get; set; }
        public string Status { get; set; } = "Active";
    }

    public class RepCohortVelocityMatrixReportDto
    {
        public int FinancialYear { get; set; }
        public List<RepCohortVelocityRowDto> Rows { get; set; } = new();
    }

    public class RepCohortVelocityRowDto
    {
        public int SalesPersonUserId { get; set; }
        public string SalesPersonName { get; set; } = string.Empty;
        public string? SalesTeam { get; set; }
        public int TotalQuotations { get; set; }
        public int TotalOrdersWon { get; set; }
        public decimal ConversionRatePercentage { get; set; }
        public decimal AvgDealVelocityDays { get; set; }
        public decimal BookedRevenue { get; set; }
        public decimal AverageDealSize { get; set; }
        public string VelocityCohort { get; set; } = "Standard"; // Fast (<10d), Medium (10-30d), Slow (>30d)
    }

    public class DiscountMarginErosionReportDto
    {
        public int FinancialYear { get; set; }
        public List<DiscountMarginErosionRowDto> Rows { get; set; } = new();
    }

    public class DiscountMarginErosionRowDto
    {
        public int SalesPersonUserId { get; set; }
        public string SalesPersonName { get; set; } = string.Empty;
        public string? SalesTeam { get; set; }
        public decimal TotalBookedRevenue { get; set; }
        public decimal AvgDiscountOfferedPercentage { get; set; }
        public decimal DiscountDisciplineScore { get; set; }
        public decimal RealizedGrossMarginPercentage { get; set; }
        public decimal MarginHurdleSpread { get; set; } // RealizedGrossMargin - 22.0%
        public decimal EstimatedMarginErosionAmount { get; set; }
        public bool AcceleratorHurdlePassed { get; set; }
    }

    public class PipEarlyWarningAuditReportDto
    {
        public int FinancialYear { get; set; }
        public List<PipEarlyWarningRowDto> AtRiskReps { get; set; } = new();
    }

    public class PipEarlyWarningRowDto
    {
        public int SalesPersonUserId { get; set; }
        public string SalesPersonName { get; set; } = string.Empty;
        public string? SalesTeam { get; set; }
        public string? Branch { get; set; }
        public decimal TargetValue { get; set; }
        public decimal AchievedValue { get; set; }
        public decimal AttainmentPercentage { get; set; }
        public decimal ExpectedAttainmentPercentage { get; set; }
        public decimal PacingIndex { get; set; }
        public decimal ShortfallAmount { get; set; }
        public string RiskLevel { get; set; } = "Low"; // Critical (Pacing < 0.60), Warning (0.60 - 0.85), Low (>= 0.85)
        public string RecommendedRemediation { get; set; } = string.Empty;
    }
}

