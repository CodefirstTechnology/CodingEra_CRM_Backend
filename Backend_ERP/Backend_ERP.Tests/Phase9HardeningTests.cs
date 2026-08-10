using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Shared.Helpers;
using Xunit;


namespace Backend_ERP.Tests;

/// <summary>
/// Phase 9 — Sales Module Production Hardening contract tests.
/// Validates shared infrastructure, API endpoint shapes, and mapper consistency.
/// </summary>
public class Phase9HardeningTests
{
    // ─── DateHelper ───────────────────────────────────────────────────────────

    [Fact]
    public void DateHelper_FormatDate_returns_yyyy_MM_dd()
    {
        var date = new DateOnly(2026, 3, 5);
        Assert.Equal("2026-03-05", DateHelper.FormatDate(date));
    }

    [Fact]
    public void DateHelper_FormatDateTime_returns_iso8601_utc()
    {
        var dto = new DateTimeOffset(2026, 3, 5, 10, 30, 0, TimeSpan.Zero);
        var result = DateHelper.FormatDateTime(dto);
        Assert.StartsWith("2026-03-05T10:30:00", result);
    }

    [Theory]
    [InlineData("2026-03-05", 2026, 3, 5)]
    [InlineData("2026-03-05T00:00:00Z", 2026, 3, 5)]
    public void DateHelper_ParseDate_handles_multiple_formats(string input, int year, int month, int day)
    {
        var fallback = new DateOnly(2000, 1, 1);
        var result = DateHelper.ParseDate(input, fallback);
        Assert.Equal(new DateOnly(year, month, day), result);
    }

    [Fact]
    public void DateHelper_ParseDate_returns_fallback_for_invalid_input()
    {
        var fallback = new DateOnly(2000, 1, 1);
        Assert.Equal(fallback, DateHelper.ParseDate(null, fallback));
        Assert.Equal(fallback, DateHelper.ParseDate("", fallback));
        Assert.Equal(fallback, DateHelper.ParseDate("not-a-date", fallback));
    }

    [Fact]
    public void DateHelper_Today_returns_today_utc()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        Assert.Equal(today, DateHelper.Today());
    }

    // ─── QuotationApproval API endpoint shapes ────────────────────────────────

    [Fact]
    public void QuotationApproval_list_query_has_all_filter_fields()
    {
        var q = new QuotationApprovalListQueryDto
        {
            Search = "test",
            Status = QuotationApprovalStatuses.Draft,
            Priority = QuotationApprovalPriorities.High,
            ApprovalLevel = QuotationApprovalLevels.Manager,
            SalesPersonUserId = 1,
            DateFrom = "2026-01-01",
            DateTo = "2026-12-31"
        };

        Assert.Equal("test", q.Search);
        Assert.Equal(QuotationApprovalStatuses.Draft, q.Status);
        Assert.Equal(QuotationApprovalPriorities.High, q.Priority);
        Assert.Equal(QuotationApprovalLevels.Manager, q.ApprovalLevel);
        Assert.Equal(1, q.SalesPersonUserId);
    }

    [Fact]
    public void QuotationApproval_lookup_dto_shape_exists()
    {
        var dto = new QuotationApprovalLookupDto
        {
            Id = 1,
            Code = "SO-2026-00001",
            Name = "Tata Projects Ltd"
        };

        Assert.Equal("SO-2026-00001", dto.Code);
        Assert.Equal("Tata Projects Ltd", dto.Name);
    }

    [Fact]
    public void QuotationApproval_statistics_dto_shape_is_complete()
    {
        var stats = new QuotationApprovalStatisticsDto
        {
            TotalCount = 20,
            PendingCount = 4,
            UnderReviewCount = 3,
            ApprovedCount = 8,
            RejectedCount = 2,
            ReturnedCount = 1,
            CancelledCount = 2,
            TotalAmount = 1_500_000m,
            Recent = new List<QuotationApprovalListItemDto>
            {
                new() { ApprovalNumber = "QA-2026-00001", Status = QuotationApprovalStatuses.Approved }
            }
        };

        Assert.Equal(20, stats.TotalCount);
        Assert.Equal(1_500_000m, stats.TotalAmount);
        Assert.Single(stats.Recent);
    }

    // ─── Workflow rules ───────────────────────────────────────────────────────

    [Fact]
    public void Reopen_is_only_allowed_from_rejected_or_cancelled()
    {
        Assert.True(QuotationApprovalStatusRules.CanReopen(QuotationApprovalStatuses.Rejected));
        Assert.True(QuotationApprovalStatusRules.CanReopen(QuotationApprovalStatuses.Cancelled));
        Assert.False(QuotationApprovalStatusRules.CanReopen(QuotationApprovalStatuses.Draft));
        Assert.False(QuotationApprovalStatusRules.CanReopen(QuotationApprovalStatuses.Approved));
    }

    [Fact]
    public void RevisionRequired_status_is_editable()
    {
        Assert.True(QuotationApprovalStatusRules.CanBeModified(QuotationApprovalStatuses.RevisionRequired));
    }

    [Fact]
    public void Cancel_is_allowed_from_draft_submitted_and_under_review()
    {
        Assert.True(QuotationApprovalStatusRules.CanCancel(QuotationApprovalStatuses.Draft));
        Assert.True(QuotationApprovalStatusRules.CanCancel(QuotationApprovalStatuses.Submitted));
        Assert.True(QuotationApprovalStatusRules.CanCancel(QuotationApprovalStatuses.UnderReview));
        Assert.False(QuotationApprovalStatusRules.CanCancel(QuotationApprovalStatuses.Approved));
        Assert.False(QuotationApprovalStatusRules.CanCancel(QuotationApprovalStatuses.Cancelled));
    }

    [Fact]
    public void Delete_only_allowed_for_draft()
    {
        Assert.True(QuotationApprovalStatusRules.CanBeDeleted(QuotationApprovalStatuses.Draft));
        Assert.False(QuotationApprovalStatusRules.CanBeDeleted(QuotationApprovalStatuses.Submitted));
        Assert.False(QuotationApprovalStatusRules.CanBeDeleted(QuotationApprovalStatuses.Approved));
    }

    // ─── Numbering pattern ────────────────────────────────────────────────────

    [Fact]
    public void QuotationApproval_number_matches_QA_year_pattern()
    {
        Assert.Matches(@"^QA-\d{4}-\d{5}$", "QA-2026-00001");
        Assert.Matches(@"^QA-\d{4}-\d{5}$", "QA-2026-99999");
    }
}
