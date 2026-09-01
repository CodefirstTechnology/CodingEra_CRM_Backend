using System.Collections.Generic;

namespace ERP.Application.Dashboard.Dtos
{
    public class AdminDashboardSummaryDto
    {
        public List<DashboardKpiDto> Kpis { get; set; } = new();
        public List<DashboardQuickActionDto> QuickActions { get; set; } = new();
        public List<DashboardListRowDto> RecentSales { get; set; } = new();
        public List<DashboardListRowDto> RecentPurchase { get; set; } = new();
        public List<DashboardListRowDto> LowStock { get; set; } = new();
        public List<DashboardListRowDto> PendingProduction { get; set; } = new();
        public List<DashboardListRowDto> UpcomingDispatches { get; set; } = new();
        public List<DashboardListRowDto> OutstandingPayments { get; set; } = new();
        public List<DashboardActivityDto> Activities { get; set; } = new();
        public List<DashboardChartBarDto> SalesTrend { get; set; } = new();
        public List<DashboardChartBarDto> PurchaseTrend { get; set; } = new();
        public List<DashboardChartBarDto> InventoryStatus { get; set; } = new();
        public List<DashboardChartBarDto> RevenueOverview { get; set; } = new();
        public decimal AchievedRevenue { get; set; }
        public decimal TargetRevenue { get; set; }
        public int TargetPct { get; set; }
    }

    public class DashboardKpiDto
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Hint { get; set; }
        public string Route { get; set; } = string.Empty;
    }

    public class DashboardQuickActionDto
    {
        public string Label { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
    }

    public class DashboardListRowDto
    {
        public string Primary { get; set; } = string.Empty;
        public string Secondary { get; set; } = string.Empty;
        public string Meta { get; set; } = string.Empty;
        public string Tone { get; set; } = "muted";
    }

    public class DashboardActivityDto
    {
        public string Title { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
    }

    public class DashboardChartBarDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }
}
