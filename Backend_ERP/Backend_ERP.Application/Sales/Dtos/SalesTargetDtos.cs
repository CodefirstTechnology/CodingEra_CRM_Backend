using System;
using System.Collections.Generic;

namespace ERP.Application.Sales.Dtos
{
    public class SalesTargetDto
    {
        public int Id { get; set; }
        public string TargetNumber { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public string TargetPeriod { get; set; } = string.Empty;
        public int FiscalYear { get; set; } = 2026;
        public int FinancialYear
        {
            get => FiscalYear;
            set => FiscalYear = value;
        }
        public string PeriodType { get; set; } = "Monthly";
        public string TargetType
        {
            get => PeriodType;
            set => PeriodType = value;
        }
        public string TargetCategory { get; set; } = string.Empty;
        public string AssignmentType { get; set; } = string.Empty;
        public int? SalesPersonUserId { get; set; }
        public string SalesTeam { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public string RegionalManager { get; set; } = string.Empty;
        public int? TerritoryId { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;

        public decimal RevenueTarget { get; set; }
        public decimal TargetValue
        {
            get => RevenueTarget;
            set => RevenueTarget = value;
        }
        public decimal VolumeTarget { get; set; }
        public decimal AchievedVolume { get; set; }
        public decimal MarginHurdleTarget { get; set; }

        public decimal RealizedRevenue { get; set; }
        public decimal RealizedCash { get; set; }
        public decimal RealizedBilled { get; set; }
        public decimal RealizedMarginPercentage { get; set; }

        public decimal AchievedValue { get; set; }
        public decimal RemainingValue { get; set; }
        public decimal OverAchievementValue { get; set; }
        public decimal AchievementPercentage { get; set; }
        public bool IsLocked { get; set; }
        public int RevisionNumber { get; set; } = 1;
        public int? PredecessorTargetId { get; set; }
        public string? ApprovalToken { get; set; }
        public string Currency { get; set; } = "INR";
        public decimal LockedExchangeRate { get; set; } = 1.000000m;
        public string Status { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedDate { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public string UpdatedDate { get; set; } = string.Empty;
        public int? ParentTargetId { get; set; }
        public bool IsAutoAggregated { get; set; }
        public decimal? OriginalTargetValue { get; set; }
        public decimal ProrationFactor { get; set; } = 1.0m;
        public decimal CalculatedCommissionAmount { get; set; }
        public ForecastMetricsDto? Forecast { get; set; }

        public List<SalesTargetListItemDto> ChildTargets { get; set; } = new();
        public List<SalesTargetItemDto> Items { get; set; } = new();
        public List<SalesTargetAssignmentDto> Assignments { get; set; } = new();
        public List<SalesTargetProgressDto> ProgressHistory { get; set; } = new();
        public List<SalesTargetHistoryDto> StatusHistory { get; set; } = new();
        public List<SalesTargetRealizationDto> Realizations { get; set; } = new();
        public List<SalesTargetApprovalHistoryDto> ApprovalHistories { get; set; } = new();
    }

    public class SalesTargetListItemDto
    {
        public int Id { get; set; }
        public string TargetNumber { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public string TargetPeriod { get; set; } = string.Empty;
        public string PeriodType { get; set; } = string.Empty;
        public string TargetType
        {
            get => PeriodType;
            set => PeriodType = value;
        }
        public string TargetCategory { get; set; } = string.Empty;
        public string AssignmentType { get; set; } = string.Empty;
        public int? SalesPersonUserId { get; set; }
        public string SalesTeam { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public int FinancialYear { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public decimal RevenueTarget { get; set; }
        public decimal TargetValue
        {
            get => RevenueTarget;
            set => RevenueTarget = value;
        }
        public decimal VolumeTarget { get; set; }
        public decimal AchievedVolume { get; set; }
        public decimal MarginHurdleTarget { get; set; }
        public decimal RealizedRevenue { get; set; }
        public decimal RealizedCash { get; set; }
        public decimal RealizedBilled { get; set; }
        public decimal AchievedValue { get; set; }
        public decimal RemainingValue { get; set; }
        public decimal OverAchievementValue { get; set; }
        public decimal AchievementPercentage { get; set; }
        public bool IsLocked { get; set; }
        public int RevisionNumber { get; set; } = 1;
        public string Currency { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int? ParentTargetId { get; set; }
    }

    public class SalesTargetItemDto
    {
        public int Id { get; set; }
        public int SalesTargetId { get; set; }
        public int? ProductCategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int? ItemMasterId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemDescription { get; set; } = string.Empty;
        public decimal TargetQuantity { get; set; }
        public decimal TargetRate { get; set; }
        public decimal TargetValue { get; set; }
        public decimal RealizedQuantity { get; set; }
        public decimal RealizedValue { get; set; }
        public decimal MinimumMarginPercentage { get; set; }
    }

    public class SalesTargetItemCreateDto
    {
        public int? ProductCategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int? ItemMasterId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemDescription { get; set; } = string.Empty;
        public decimal TargetQuantity { get; set; }
        public decimal TargetRate { get; set; }
        public decimal TargetValue { get; set; }
        public decimal MinimumMarginPercentage { get; set; }
    }

    public class SalesTargetRealizationDto
    {
        public int Id { get; set; }
        public int SalesTargetId { get; set; }
        public int? SalesOrderId { get; set; }
        public int? AdvancePaymentId { get; set; }
        public int? SalesInvoiceId { get; set; }
        public int? InvoiceId
        {
            get => SalesInvoiceId;
            set => SalesInvoiceId = value;
        }
        public int? QuotationId { get; set; }
        public string RealizationType { get; set; } = "BookingNet";
        public string TransactionType
        {
            get => RealizationType;
            set => RealizationType = value;
        }
        public decimal RealizedAmount { get; set; }
        public decimal RealizedQuantity { get; set; }
        public decimal MarginContributionAmount { get; set; }
        public string SourceCurrency { get; set; } = "INR";
        public decimal SourceAmount { get; set; }
        public decimal LockedFXRate { get; set; } = 1.000000m;
        public bool IsReversal { get; set; }
        public int? ReversalOfRealizationId { get; set; }
        public string ReversalReason { get; set; } = string.Empty;
        public string TransactionDate { get; set; } = string.Empty;
        public string AppliedOn { get; set; } = string.Empty;
        public string AppliedBy { get; set; } = "SYSTEM_EVENT";
    }

    public class SalesTargetApprovalHistoryDto
    {
        public int Id { get; set; }
        public int SalesTargetId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string ApproverUserId { get; set; } = string.Empty;
        public string ApproverName { get; set; } = string.Empty;
        public string ApproverRole { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
        public string HmacSignatureToken { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
    }

    public class SalesTargetAssignmentDto
    {
        public int Id { get; set; }
        public int SalesPersonUserId { get; set; }
        public decimal AssignedTarget { get; set; }
        public decimal AchievedValue { get; set; }
        public decimal AchievementPercentage { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string AssignedBy { get; set; } = string.Empty;
        public string AssignedDate { get; set; } = string.Empty;
    }

    public class SalesTargetProgressDto
    {
        public int Id { get; set; }
        public decimal OldAchievedValue { get; set; }
        public decimal NewAchievedValue { get; set; }
        public decimal AchievementPercentage { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public string UpdatedOn { get; set; } = string.Empty;
    }

    public class SalesTargetHistoryDto
    {
        public int Id { get; set; }
        public string? OldStatus { get; set; }
        public string NewStatus { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string ChangedBy { get; set; } = string.Empty;
        public string ChangedOn { get; set; } = string.Empty;
    }

    public class SalesTargetCreateRequestDto
    {
        public string? TargetNumber { get; set; }
        public string TargetName { get; set; } = string.Empty;
        public string TargetPeriod { get; set; } = string.Empty;
        public string PeriodType { get; set; } = "Monthly";
        public string TargetType
        {
            get => PeriodType;
            set => PeriodType = value;
        }
        public string TargetCategory { get; set; } = "Revenue";
        public string AssignmentType { get; set; } = "IndividualSalesperson";
        public int? SalesPersonUserId { get; set; }
        public string? SalesTeam { get; set; }
        public string? Branch { get; set; }
        public string? RegionalManager { get; set; }
        public int? TerritoryId { get; set; }
        public int FinancialYear { get; set; } = 2026;
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public decimal TargetValue { get; set; }
        public decimal RevenueTarget
        {
            get => TargetValue;
            set => TargetValue = value;
        }
        public decimal? AchievedValue { get; set; }
        public decimal VolumeTarget { get; set; }
        public decimal MarginHurdleTarget { get; set; }
        public string Currency { get; set; } = "INR";
        public decimal LockedExchangeRate { get; set; } = 1.000000m;
        public string? Status { get; set; }
        public string? Remarks { get; set; }
        public int? ParentTargetId { get; set; }
        public bool? IsAutoAggregated { get; set; }
        public List<SalesTargetItemCreateDto>? Items { get; set; }
        public List<SalesTargetAssignmentRequestDto>? Assignments { get; set; }
    }

    public class SalesTargetUpdateRequestDto
    {
        public string? TargetNumber { get; set; }
        public string TargetName { get; set; } = string.Empty;
        public string TargetPeriod { get; set; } = string.Empty;
        public string PeriodType { get; set; } = "Monthly";
        public string TargetType
        {
            get => PeriodType;
            set => PeriodType = value;
        }
        public string TargetCategory { get; set; } = "Revenue";
        public string AssignmentType { get; set; } = "IndividualSalesperson";
        public int? SalesPersonUserId { get; set; }
        public string? SalesTeam { get; set; }
        public string? Branch { get; set; }
        public string? RegionalManager { get; set; }
        public int? TerritoryId { get; set; }
        public int FinancialYear { get; set; } = 2026;
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public decimal TargetValue { get; set; }
        public decimal RevenueTarget
        {
            get => TargetValue;
            set => TargetValue = value;
        }
        public decimal? AchievedValue { get; set; }
        public decimal VolumeTarget { get; set; }
        public decimal MarginHurdleTarget { get; set; }
        public string Currency { get; set; } = "INR";
        public decimal LockedExchangeRate { get; set; } = 1.000000m;
        public string? Status { get; set; }
        public string? Remarks { get; set; }
        public int? ParentTargetId { get; set; }
        public bool? IsAutoAggregated { get; set; }
        public List<SalesTargetItemCreateDto>? Items { get; set; }
        public List<SalesTargetAssignmentRequestDto>? Assignments { get; set; }
    }

    public class SalesTargetAssignmentRequestDto
    {
        public int SalesPersonUserId { get; set; }
        public decimal AssignedTarget { get; set; }
        public decimal? AchievedValue { get; set; }
        public string? Remarks { get; set; }
    }

    public class SalesTargetDuplicateRequestDto
    {
        public string? TargetName { get; set; }
        public int? FinancialYear { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public decimal? TargetValue { get; set; }
    }

    public class SalesTargetCopyPreviousRequestDto
    {
        public int? SourceSalesPersonUserId { get; set; }
        public int? SourceFinancialYear { get; set; }
        public string? TargetCategory { get; set; }
        public int TargetFinancialYear { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public string? TargetName { get; set; }
    }

    public class SalesTargetStatusUpdateRequestDto
    {
        public string Status { get; set; } = string.Empty;
        public string? Remarks { get; set; }
    }

    public class SalesTargetProgressUpdateRequestDto
    {
        public decimal AchievedValue { get; set; }
        public string? Remarks { get; set; }
    }

    public class SalesTargetRemarksRequestDto
    {
        public string? Remarks { get; set; }
    }

    public class SalesTargetRevisionRequestDto
    {
        public decimal NewRevenueTarget { get; set; }
        public decimal NewVolumeTarget { get; set; }
        public decimal NewMarginHurdleTarget { get; set; }
        public string RevisionReason { get; set; } = string.Empty;
        public bool ProrateExistingRealizations { get; set; } = true;
    }

    public class SalesTargetApprovalActionDto
    {
        public string Action { get; set; } = "Approved"; // "Approved" or "Rejected"
        public string Comments { get; set; } = string.Empty;
    }

    public class SalesTargetTreeDto
    {
        public int TargetId { get; set; }
        public string TargetNumber { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public string ScopeName { get; set; } = string.Empty;
        public string AssignmentType { get; set; } = string.Empty;
        public decimal QuotaValue { get; set; }
        public decimal RealizedRevenue { get; set; }
        public decimal RealizedCash { get; set; }
        public decimal RealizedBilled { get; set; }
        public decimal AchievedValue { get; set; }
        public decimal AttainmentPercentage { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<SalesTargetTreeDto> Children { get; set; } = new();
    }

    public class SalesTargetListQueryDto
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? TargetType { get; set; }
        public string? TargetCategory { get; set; }
        public string? Branch { get; set; }
        public int? SalesPersonUserId { get; set; }
        public int? FinancialYear { get; set; }
        public string? TargetPeriod { get; set; }
        public string? DateFrom { get; set; }
        public string? DateTo { get; set; }
    }

    public class SalesTargetReportFilterDto
    {
        public int? FiscalYear { get; set; } = 2026;
        public string? Period { get; set; }
        public string? Branch { get; set; }
        public int? SalesPersonUserId { get; set; }
        public string? TargetCategory { get; set; }
        public string? Status { get; set; }
    }

    public class SalesTargetDashboardDto
    {
        // Legacy summary fields
        public int TotalCount { get; set; }
        public int DraftCount { get; set; }
        public int ActiveCount { get; set; }
        public int CompletedCount { get; set; }
        public int ExpiredCount { get; set; }
        public int CancelledCount { get; set; }
        public decimal TotalTargetValue { get; set; }
        public decimal TotalAchievedValue { get; set; }
        public decimal AverageAchievementPercentage { get; set; }

        // Hardened Company-Wide & Telemetry Metrics
        public decimal CompanyRevenueQuota { get; set; }
        public decimal CompanyRealizedRevenue { get; set; }
        public decimal CompanyRealizedCash { get; set; }
        public decimal CompanyRealizedBilled { get; set; }
        public decimal CompanyAttainmentPercentage { get; set; }
        public decimal RequiredDailyRunRate { get; set; }
        public decimal CurrentDailyRunRate { get; set; }
        public decimal PaceIndex { get; set; }
        public int OnTrackCount { get; set; }
        public int DeficitCount { get; set; }

        public List<SalesTargetListItemDto> Recent { get; set; } = new();
        public List<TopPerformerCardDto> TopPerformers { get; set; } = new();
        public List<CategoryAchievementHeatmapDto> CategoryHeatmap { get; set; } = new();
    }

    public class TopPerformerCardDto
    {
        public int SalesPersonUserId { get; set; }
        public string SalesPersonName { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public decimal TargetQuota { get; set; }
        public decimal AchievedRevenue { get; set; }
        public decimal AttainmentPercentage { get; set; }
        public string BadgeLevel { get; set; } = "Bronze"; // Bronze, Silver, Gold, Platinum
    }

    public class CategoryAchievementHeatmapDto
    {
        public string Branch { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal TargetAmount { get; set; }
        public decimal RealizedAmount { get; set; }
        public decimal AttainmentPercentage { get; set; }
    }

    public class SalesTargetVarianceReportDto
    {
        public string GeneratedOn { get; set; } = string.Empty;
        public int FiscalYear { get; set; }
        public string Period { get; set; } = string.Empty;
        public decimal TotalTarget { get; set; }
        public decimal TotalBooked { get; set; }
        public decimal TotalBilled { get; set; }
        public decimal TotalCash { get; set; }
        public decimal OverallAttainmentPct { get; set; }
        public List<VarianceRowDto> Rows { get; set; } = new();
    }

    public class VarianceRowDto
    {
        public int TargetId { get; set; }
        public string TargetNumber { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public string SalesPersonName { get; set; } = string.Empty;
        public decimal TargetQuota { get; set; }
        public decimal ConfirmedBookings { get; set; }
        public decimal DispatchedBilled { get; set; }
        public decimal CashCollected { get; set; }
        public decimal BookingAttainmentPct { get; set; }
        public decimal CashConversionPct { get; set; }
        public decimal VarianceAmount { get; set; }
        public string PaceStatus { get; set; } = "OnTrack"; // "Ahead", "OnTrack", "Deficit"
    }

    public class SalesTargetRepLeaderboardDto
    {
        public string GeneratedOn { get; set; } = string.Empty;
        public int FiscalYear { get; set; }
        public List<RepLeaderboardRowDto> Rankings { get; set; } = new();
    }

    public class RepLeaderboardRowDto
    {
        public int Rank { get; set; }
        public int SalesPersonUserId { get; set; }
        public string SalesPersonName { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public decimal TargetQuota { get; set; }
        public decimal RealizedRevenue { get; set; }
        public decimal RealizedCash { get; set; }
        public decimal AttainmentPercentage { get; set; }
        public decimal DeficitAmount { get; set; }
        public decimal RequiredDailyRunRate { get; set; }
        public string AcceleratorTier { get; set; } = "Base"; // "Floor", "Base", "Accelerator1.5x", "Super2.0x"
    }

    public class SalesTargetCategoryMatrixDto
    {
        public string GeneratedOn { get; set; } = string.Empty;
        public int FiscalYear { get; set; }
        public List<CategoryMatrixRowDto> Rows { get; set; } = new();
    }

    public class CategoryMatrixRowDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal TargetAmount { get; set; }
        public decimal RealizedAmount { get; set; }
        public decimal AttainmentPercentage { get; set; }
        public decimal TargetQuantity { get; set; }
        public decimal RealizedQuantity { get; set; }
        public decimal ContributionPercentage { get; set; }
    }

    public class SalesTargetReportDto
    {
        public string GeneratedOn { get; set; } = string.Empty;
        public int RowCount { get; set; }
        public decimal TotalTargetValue { get; set; }
        public decimal TotalAchievedValue { get; set; }
        public List<SalesTargetListItemDto> Rows { get; set; } = new();
    }

    public class SalesTargetExportRequestDto
    {
        public string Format { get; set; } = "csv";
        public string? Status { get; set; }
        public string? TargetCategory { get; set; }
        public int? FinancialYear { get; set; }
        public string? Branch { get; set; }
    }

    public class SalesTargetExportMetadataDto
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string GeneratedOn { get; set; } = string.Empty;
    }

    public class SalesTargetLookupDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public record CommissionCalculationResultDto(
        int TargetId,
        string TargetNumber,
        string SalesPersonName,
        decimal AchievedValue,
        decimal AchievementPercentage,
        decimal CommissionAmount,
        string AppliedBracket,
        decimal AcceleratorRate
    );

    public record ProrateTargetRequestDto(
        int ActiveWorkingDays,
        int TotalWorkingDays,
        string Reason
    );

    public record ForecastMetricsDto(
        decimal RequiredDailyRunRate,
        decimal CurrentDailyRunRate,
        decimal ProjectedAttainmentPercentage,
        string HealthStatus // "OnTrack", "AtRisk", "Critical"
    );
}
