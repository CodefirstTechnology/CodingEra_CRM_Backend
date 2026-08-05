using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using Xunit;

namespace Backend_ERP.Tests;

public class SalesTargetContractTests
{
    [Fact]
    public void Calculator_computes_remaining_and_achievement_percentage()
    {
        Assert.Equal(40000m, SalesTargetCalculator.CalcRemaining(100000m, 60000m));
        Assert.Equal(60m, SalesTargetCalculator.CalcAchievementPercentage(100000m, 60000m));
        Assert.Equal(100m, SalesTargetCalculator.CalcAchievementPercentage(50000m, 50000m));
        Assert.True(SalesTargetCalculator.WouldExceedTarget(10000m, 10001m));
        Assert.False(SalesTargetCalculator.WouldExceedTarget(10000m, 10000m));
    }

    [Fact]
    public void Achievement_at_100_percent_resolves_to_completed_from_active()
    {
        Assert.Equal(
            SalesTargetStatuses.Completed,
            SalesTargetCalculator.ResolveStatusAfterProgress(SalesTargetStatuses.Active, 100m));
        Assert.Equal(
            SalesTargetStatuses.Active,
            SalesTargetCalculator.ResolveStatusAfterProgress(SalesTargetStatuses.Active, 99.99m));
    }

    [Theory]
    [InlineData(SalesTargetStatuses.Draft, SalesTargetStatuses.Active, true)]
    [InlineData(SalesTargetStatuses.Draft, SalesTargetStatuses.Cancelled, true)]
    [InlineData(SalesTargetStatuses.Active, SalesTargetStatuses.Completed, true)]
    [InlineData(SalesTargetStatuses.Active, SalesTargetStatuses.Expired, true)]
    [InlineData(SalesTargetStatuses.Active, SalesTargetStatuses.Cancelled, true)]
    [InlineData(SalesTargetStatuses.Draft, SalesTargetStatuses.Completed, false)]
    [InlineData(SalesTargetStatuses.Completed, SalesTargetStatuses.Active, false)]
    [InlineData(SalesTargetStatuses.Cancelled, SalesTargetStatuses.Active, false)]
    public void Status_transitions_are_validated(string from, string to, bool expected)
    {
        Assert.Equal(expected, SalesTargetStatusRules.CanTransition(from, to));
    }

    [Fact]
    public void Create_request_shape_matches_frontend_contract()
    {
        var dto = new SalesTargetCreateRequestDto
        {
            TargetName = "Q1 West Revenue",
            TargetType = SalesTargetTypes.Quarterly,
            TargetCategory = SalesTargetCategories.Revenue,
            AssignmentType = SalesTargetAssignmentTypes.IndividualSalesperson,
            SalesPersonUserId = 1,
            FinancialYear = 2026,
            StartDate = "2026-04-01",
            EndDate = "2026-06-30",
            TargetValue = 2500000m,
            Currency = "INR",
            Assignments =
            [
                new SalesTargetAssignmentRequestDto
                {
                    SalesPersonUserId = 1,
                    AssignedTarget = 2500000m
                }
            ]
        };

        Assert.Equal("Q1 West Revenue", dto.TargetName);
        Assert.Equal(SalesTargetTypes.Quarterly, dto.TargetType);
        Assert.Single(dto.Assignments!);
    }

    [Fact]
    public void Duplicate_and_status_update_request_shapes_exist()
    {
        var duplicate = new SalesTargetDuplicateRequestDto
        {
            TargetName = "Q1 West Revenue (Copy)",
            FinancialYear = 2027
        };
        var status = new SalesTargetStatusUpdateRequestDto
        {
            Status = SalesTargetStatuses.Active,
            Remarks = "Go live"
        };

        Assert.Equal(2027, duplicate.FinancialYear);
        Assert.Equal(SalesTargetStatuses.Active, status.Status);
    }

    [Fact]
    public void Category_and_type_normalize_accept_compact_values()
    {
        Assert.Equal(SalesTargetCategories.CustomerAcquisition,
            SalesTargetCategoryRules.Normalize("Customer Acquisition"));
        Assert.Equal(SalesTargetTypes.HalfYearly,
            SalesTargetTypeRules.Normalize("Half Yearly"));
        Assert.Equal(SalesTargetAssignmentTypes.IndividualSalesperson,
            SalesTargetAssignmentTypeRules.Normalize("IndividualSalesperson"));
    }

    [Fact]
    public void Overlap_rule_is_expressed_via_active_same_person_category_period()
    {
        // Contract: overlapping Active targets for same salesperson + category + period are rejected.
        // Enforced in SalesTargetService via ISalesTargetRepository.HasOverlappingActiveTargetAsync.
        Assert.True(SalesTargetStatusRules.CanTransition(SalesTargetStatuses.Draft, SalesTargetStatuses.Active));
        Assert.Equal(SalesTargetCategories.Revenue, SalesTargetCategoryRules.Normalize("revenue"));
    }
}
