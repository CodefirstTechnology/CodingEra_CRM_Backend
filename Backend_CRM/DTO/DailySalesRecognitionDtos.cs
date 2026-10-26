using System;
using System.Collections.Generic;

namespace CRM.DTO
{
    public class DailySalesRecognitionLogDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int? ExecutiveUserId { get; set; }
        public string ExecutiveName { get; set; } = string.Empty;
        public int NewLeads { get; set; }
        public int CustomerCalls { get; set; }
        public int Meetings { get; set; }
        public int FollowUps { get; set; }
        public int Quotations { get; set; }
        public int LeadConversions { get; set; }
        public int OrdersBooked { get; set; }
        public int PaymentFollowUps { get; set; }
        public decimal ReportingPercentage { get; set; }
        public int TeamSupport { get; set; }
        public decimal LeadQualityPercentage { get; set; }
        public decimal DailyActivityScore { get; set; }
        public decimal BusinessResultScore { get; set; }
        public decimal OverallScore { get; set; }
        public string Recognition { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class DailySalesRecognitionUpsertDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int? ExecutiveUserId { get; set; }
        public string ExecutiveName { get; set; } = string.Empty;
        public int NewLeads { get; set; }
        public int CustomerCalls { get; set; }
        public int Meetings { get; set; }
        public int FollowUps { get; set; }
        public int Quotations { get; set; }
        public int LeadConversions { get; set; }
        public int OrdersBooked { get; set; }
        public int PaymentFollowUps { get; set; }
        public decimal ReportingPercentage { get; set; } = 100m;
        public int TeamSupport { get; set; } = 1;
        public decimal LeadQualityPercentage { get; set; } = 0m;
    }

    public class ExecutiveRankingDto
    {
        public string Executive { get; set; } = string.Empty;
        public int? ExecutiveUserId { get; set; }
        public decimal AvgOverallScore { get; set; }
        public int TotalLeads { get; set; }
        public int TotalMeetings { get; set; }
        public int TotalFollowUps { get; set; }
        public int TotalQuotations { get; set; }
        public int TotalConversions { get; set; }
        public int TotalOrders { get; set; }
        public decimal ReportingPercentage { get; set; }
        public string Recognition { get; set; } = string.Empty;
    }

    public class DailyKpiSummaryDto
    {
        public int TotalLeads { get; set; }
        public int TotalMeetings { get; set; }
        public int TotalFollowUps { get; set; }
        public int TotalQuotations { get; set; }
        public int TotalConversions { get; set; }
        public int TotalOrders { get; set; }
        public decimal TeamAverageOverallScore { get; set; }
    }

    public class LeaderboardChartItemDto
    {
        public string Executive { get; set; } = string.Empty;
        public decimal AvgOverallScore { get; set; }
    }

    public class DailySalesRecognitionDashboardResponseDto
    {
        public List<DailySalesRecognitionLogDto> Items { get; set; } = new();
        public DailyKpiSummaryDto KpiSummary { get; set; } = new();
        public List<ExecutiveRankingDto> Rankings { get; set; } = new();
        public List<LeaderboardChartItemDto> ChartSeries { get; set; } = new();
    }
}
