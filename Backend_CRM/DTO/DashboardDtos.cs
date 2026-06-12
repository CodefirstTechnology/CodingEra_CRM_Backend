namespace CRM.DTO
{
    public class UserDashboardPreferenceDto
    {
        public bool MorningBriefingEnabled { get; set; } = true;
        public string? LastBriefingPlayedDate { get; set; }
    }

    public class UserDashboardPreferenceUpdateDto
    {
        public bool MorningBriefingEnabled { get; set; }
    }

    public class DashboardPipelineSegmentDto
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal RevenueInr { get; set; }
    }

    /// <summary>Live CRM metrics aligned with the admin Performance Ledger dashboard.</summary>
    public class DashboardUserSummaryDto
    {
        public string UserName { get; set; } = string.Empty;
        public int TotalLeads { get; set; }
        public int QualifiedLeads { get; set; }
        public int ConvertedLeads { get; set; }
        public decimal ConversionRatePct { get; set; }
        public int NewLeadsThisMonth { get; set; }
        public int NewLeadsToday { get; set; }
        public int PendingFollowUps { get; set; }
        public int FollowUpsToday { get; set; }
        public int OverdueFollowUps { get; set; }
        public int ActiveDeals { get; set; }
        public decimal PipelineRevenueInr { get; set; }
        public int NewDealsToday { get; set; }
        public int DealsPendingClosure { get; set; }
        public int DealsWonToday { get; set; }
        public int DealsLostToday { get; set; }
        public int MeetingsToday { get; set; }
        public int TasksDueToday { get; set; }
        public int HighPriorityLeads { get; set; }
        public int StuckDealsCount { get; set; }
        public int QualifiedLeadsThisMonth { get; set; }
        public decimal MonthlyRevenueInr { get; set; }
        public decimal MonthlyTargetInr { get; set; }
        public decimal MonthlyTargetAchievedPct { get; set; }
        public decimal? RevenueToday { get; set; }
        public decimal? TargetProgressPercent { get; set; }
        public IReadOnlyList<DashboardPipelineSegmentDto> PipelineSegments { get; set; } =
            Array.Empty<DashboardPipelineSegmentDto>();
        public IReadOnlyList<DashboardTargetProgressDto> TargetProgress { get; set; } =
            Array.Empty<DashboardTargetProgressDto>();

        public int PendingTasks { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int CompletedTasks { get; set; }
        public decimal ConversionPercent { get; set; }
        public int ClosedDeals { get; set; }
    }

    public class DashboardTargetProgressDto
    {
        public string TargetTypeName { get; set; } = string.Empty;
        public decimal AchievementPercent { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal AchievedAmount { get; set; }
    }

    /// <summary>Daily executive briefing metrics from the admin dashboard.</summary>
    public class DailyBriefingMetricsDto
    {
        public string? AdminName { get; set; }
        public int TotalLeads { get; set; }
        public int ActiveDeals { get; set; }
        public int NewLeadsToday { get; set; }
        public int NewDealsToday { get; set; }
        public int PendingFollowUps { get; set; }
        public int FollowUpsToday { get; set; }
        public int OverdueFollowUps { get; set; }
        public int DealsPendingClosure { get; set; }
        public int DealsWonToday { get; set; }
        public int DealsLostToday { get; set; }
        public int MeetingsToday { get; set; }
        public int TasksDueToday { get; set; }
        public int HighPriorityLeads { get; set; }
        public int StuckDealsCount { get; set; }
        public int StuckLeadsCount { get; set; }
        public decimal? RevenueToday { get; set; }
    }

    public class MorningBriefingResponseDto
    {
        public string Message { get; set; } = string.Empty;
        public string Source { get; set; } = "fallback";
        public bool Cached { get; set; }
        public DailyBriefingMetricsDto Metrics { get; set; } = new();
    }
}
