namespace ERP.Application.Sales.Dtos
{
    public class PerformanceFilterDto
    {
        public int? SalesPersonUserId { get; set; }
        public string? SalesTeam { get; set; }
        public string? Branch { get; set; }
        public string? RegionalManager { get; set; }
        public int? FinancialYear { get; set; }
        public int? Month { get; set; }
        public int? Quarter { get; set; }
        public string? DateFrom { get; set; }
        public string? DateTo { get; set; }
        public string? PeriodType { get; set; }
    }

    public class PerformanceSummaryDto
    {
        public int TotalSalesOrders { get; set; }
        public int ConfirmedSalesOrders { get; set; }
        public int CompletedSalesOrders { get; set; }
        public int CancelledSalesOrders { get; set; }
        public decimal SalesRevenue { get; set; }
        public int ProformaInvoiceCount { get; set; }
        public int ApprovedProformaInvoices { get; set; }
        public int ConvertedProformaInvoices { get; set; }
        public decimal ProformaAmount { get; set; }
        public decimal AdvanceReceived { get; set; }
        public decimal AdvanceApplied { get; set; }
        public decimal OutstandingAdvance { get; set; }
        public decimal AssignedTarget { get; set; }
        public decimal AchievedTarget { get; set; }
        public decimal SalesAchievementPercentage { get; set; }
        public decimal RevenueAchievementPercentage { get; set; }
        public decimal CollectionPercentage { get; set; }
        public decimal ConversionPercentage { get; set; }
    }

    public class PerformanceSalesPersonDto : PerformanceSummaryDto
    {
        public int SalesPersonUserId { get; set; }
        public string SalesPerson { get; set; } = string.Empty;
        public string SalesTeam { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public string RegionalManager { get; set; } = string.Empty;
    }

    public class PerformanceLeaderboardDto
    {
        public int Rank { get; set; }
        public int SalesPersonUserId { get; set; }
        public string SalesPerson { get; set; } = string.Empty;
        public decimal AchievementPercentage { get; set; }
        public decimal SalesRevenue { get; set; }
        public bool IsTopPerformer { get; set; }
    }

    public class PerformanceTrendDto
    {
        public string Period { get; set; } = string.Empty;
        public string PeriodType { get; set; } = string.Empty;
        public decimal SalesRevenue { get; set; }
        public decimal Collections { get; set; }
        public decimal Target { get; set; }
        public decimal AchievementPercentage { get; set; }
    }

    public class PerformanceTimelineDto
    {
        public string Date { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }

    public class PerformanceHistoryDto
    {
        public int Id { get; set; }
        public int? SalesPersonUserId { get; set; }
        public string PeriodStart { get; set; } = string.Empty;
        public string PeriodEnd { get; set; } = string.Empty;
        public string MetricName { get; set; } = string.Empty;
        public decimal OldValue { get; set; }
        public decimal NewValue { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public string RecordedOn { get; set; } = string.Empty;
    }

    public class PerformanceAnalyticsDto
    {
        public PerformanceSummaryDto Summary { get; set; } = new();
        public List<PerformanceTrendDto> MonthlyTrends { get; set; } = new();
        public List<PerformanceTrendDto> QuarterlyTrends { get; set; } = new();
        public List<PerformanceLeaderboardDto> TopPerformers { get; set; } = new();
        public List<PerformanceLeaderboardDto> BottomPerformers { get; set; } = new();
    }

    public class PerformanceDashboardDto
    {
        public string GeneratedOn { get; set; } = string.Empty;
        public PerformanceSummaryDto Summary { get; set; } = new();
        public List<PerformanceSalesPersonDto> Salespersons { get; set; } = new();
        public List<PerformanceTrendDto> Trends { get; set; } = new();
    }

    public class PerformanceExportRequestDto
    {
        public string ReportName { get; set; } = "performance-report";
        public string ExportType { get; set; } = "csv";
        public PerformanceFilterDto? Filters { get; set; }
    }

    public class PerformanceExportDto
    {
        public int Id { get; set; }
        public string ReportName { get; set; } = string.Empty;
        public string GeneratedBy { get; set; } = string.Empty;
        public string GeneratedOn { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ExportType { get; set; } = string.Empty;
    }

    public class PerformanceReportDto
    {
        public string GeneratedOn { get; set; } = string.Empty;
        public PerformanceSummaryDto Summary { get; set; } = new();
        public List<PerformanceSalesPersonDto> Rows { get; set; } = new();
    }

    public class PerformanceLookupDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
