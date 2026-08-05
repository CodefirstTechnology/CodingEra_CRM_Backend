using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using Xunit;

namespace Backend_ERP.Tests;

public class PerformanceDashboardContractTests
{
    [Theory]
    [InlineData(50, 100, 50)]
    [InlineData(1, 4, 25)]
    [InlineData(100, 0, 0)]
    public void Percentage_calculation_is_safe(decimal numerator, decimal denominator, decimal expected)
    {
        Assert.Equal(expected, PerformanceCalculator.Percentage(numerator, denominator));
    }

    [Fact]
    public void Dashboard_contract_contains_summary_people_and_trends()
    {
        var dashboard = new PerformanceDashboardDto
        {
            Summary = new PerformanceSummaryDto { SalesRevenue = 100000m },
            Salespersons = [new PerformanceSalesPersonDto { SalesPersonUserId = 1 }],
            Trends = [new PerformanceTrendDto { Period = "2026-08" }]
        };

        Assert.Equal(100000m, dashboard.Summary.SalesRevenue);
        Assert.Single(dashboard.Salespersons);
        Assert.Single(dashboard.Trends);
    }

    [Fact]
    public void Leaderboard_contract_supports_top_and_bottom_ranking()
    {
        var row = new PerformanceLeaderboardDto
        {
            Rank = 1,
            SalesPersonUserId = 1,
            AchievementPercentage = 92.5m,
            IsTopPerformer = true
        };

        Assert.True(row.IsTopPerformer);
        Assert.Equal(1, row.Rank);
    }

    [Fact]
    public void Analytics_contract_contains_monthly_and_quarterly_trends()
    {
        var analytics = new PerformanceAnalyticsDto
        {
            MonthlyTrends = [new PerformanceTrendDto { PeriodType = PerformancePeriodTypes.Monthly }],
            QuarterlyTrends = [new PerformanceTrendDto { PeriodType = PerformancePeriodTypes.Quarterly }]
        };

        Assert.Single(analytics.MonthlyTrends);
        Assert.Single(analytics.QuarterlyTrends);
    }

    [Fact]
    public void Report_and_export_contracts_are_available()
    {
        var report = new PerformanceReportDto
        {
            Rows = [new PerformanceSalesPersonDto { SalesPersonUserId = 2 }]
        };
        var export = new PerformanceExportDto
        {
            ReportName = "Monthly Performance",
            ExportType = "csv"
        };

        Assert.Single(report.Rows);
        Assert.Equal("csv", export.ExportType);
    }

    [Fact]
    public void Timeline_and_history_contracts_preserve_metric_data()
    {
        var timeline = new PerformanceTimelineDto
        {
            EventType = "SalesOrder",
            ReferenceNumber = "SO-2026-0001",
            Value = 250000m
        };
        var history = new PerformanceHistoryDto
        {
            MetricName = "RevenueTarget",
            OldValue = 100000m,
            NewValue = 125000m
        };

        Assert.Equal("SalesOrder", timeline.EventType);
        Assert.True(history.NewValue > history.OldValue);
    }

    [Theory]
    [InlineData(PerformancePeriodTypes.Daily)]
    [InlineData(PerformancePeriodTypes.Weekly)]
    [InlineData(PerformancePeriodTypes.Monthly)]
    [InlineData(PerformancePeriodTypes.Quarterly)]
    [InlineData(PerformancePeriodTypes.HalfYearly)]
    [InlineData(PerformancePeriodTypes.Yearly)]
    [InlineData(PerformancePeriodTypes.Custom)]
    public void All_supported_periods_are_exposed(string period)
    {
        Assert.Contains(period, PerformancePeriodTypes.All);
    }
}
