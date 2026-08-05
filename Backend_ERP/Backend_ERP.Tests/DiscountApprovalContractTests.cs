using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using Xunit;

namespace Backend_ERP.Tests;

public class DiscountApprovalContractTests
{
    [Theory]
    [InlineData(DiscountApprovalStatuses.Pending, DiscountApprovalStatuses.UnderReview, true)]
    [InlineData(DiscountApprovalStatuses.Pending, DiscountApprovalStatuses.Approved, true)]
    [InlineData(DiscountApprovalStatuses.Pending, DiscountApprovalStatuses.Rejected, true)]
    [InlineData(DiscountApprovalStatuses.Pending, DiscountApprovalStatuses.Returned, true)]
    [InlineData(DiscountApprovalStatuses.Pending, DiscountApprovalStatuses.Cancelled, true)]
    [InlineData(DiscountApprovalStatuses.UnderReview, DiscountApprovalStatuses.Approved, true)]
    [InlineData(DiscountApprovalStatuses.UnderReview, DiscountApprovalStatuses.Rejected, true)]
    [InlineData(DiscountApprovalStatuses.UnderReview, DiscountApprovalStatuses.Returned, true)]
    [InlineData(DiscountApprovalStatuses.Returned, DiscountApprovalStatuses.Pending, true)]
    [InlineData(DiscountApprovalStatuses.Approved, DiscountApprovalStatuses.Pending, false)]
    [InlineData(DiscountApprovalStatuses.Rejected, DiscountApprovalStatuses.Approved, false)]
    [InlineData(DiscountApprovalStatuses.Cancelled, DiscountApprovalStatuses.Pending, false)]
    public void Workflow_transitions_are_validated(string from, string to, bool expected)
    {
        Assert.Equal(expected, DiscountApprovalStatusRules.CanTransition(from, to));
    }

    [Fact]
    public void Create_request_shape_matches_contract()
    {
        var dto = new DiscountApprovalCreateRequestDto
        {
            SourceType = DiscountApprovalSourceTypes.SalesOrder,
            SalesOrderId = 1,
            PriceListId = 1,
            CustomerName = "Tata Projects Ltd",
            CustomerCategory = PriceListCustomerCategories.Enterprise,
            SalesPersonUserId = 1,
            RequestedDiscountPercentage = 12m,
            RequestedAmount = 185000m,
            ApprovalLevel = DiscountApprovalLevels.Admin,
            Priority = DiscountApprovalPriorities.High,
            Reason = "Strategic account volume discount beyond list max"
        };

        Assert.Equal(DiscountApprovalSourceTypes.SalesOrder, dto.SourceType);
        Assert.Equal(12m, dto.RequestedDiscountPercentage);
        Assert.Equal(DiscountApprovalLevels.Admin, dto.ApprovalLevel);
    }

    [Theory]
    [InlineData(3, DiscountApprovalLevels.Auto)]
    [InlineData(8, DiscountApprovalLevels.Manager)]
    [InlineData(15, DiscountApprovalLevels.Admin)]
    [InlineData(25, DiscountApprovalLevels.Escalated)]
    public void Approval_matrix_maps_discount_to_required_level(decimal discount, string expected)
    {
        Assert.Equal(expected, DiscountApprovalLevelRules.RequiredLevelForDiscount(discount));
    }

    [Fact]
    public void Approval_matrix_rejects_insufficient_level()
    {
        Assert.False(DiscountApprovalLevelRules.LevelSatisfies(
            DiscountApprovalLevels.Manager,
            DiscountApprovalLevels.Admin));
        Assert.True(DiscountApprovalLevelRules.LevelSatisfies(
            DiscountApprovalLevels.Admin,
            DiscountApprovalLevels.Manager));
        Assert.True(DiscountApprovalLevelRules.LevelSatisfies(
            DiscountApprovalLevels.Auto,
            DiscountApprovalLevels.Auto));
    }

    [Fact]
    public void Decision_and_statistics_dto_shapes_exist()
    {
        var decision = new DiscountApprovalDecisionRequestDto
        {
            ApprovedDiscountPercentage = 10m,
            ApprovedAmount = 150000m,
            Remarks = "Approved with conditions"
        };
        var stats = new DiscountApprovalStatisticsDto
        {
            TotalCount = 10,
            PendingCount = 2,
            ApprovedCount = 3
        };

        Assert.Equal(10m, decision.ApprovedDiscountPercentage);
        Assert.Equal(10, stats.TotalCount);
    }

    [Fact]
    public void History_and_comment_dto_shapes_exist()
    {
        var history = new DiscountApprovalHistoryDto
        {
            Action = DiscountApprovalHistoryActions.Approved,
            OldStatus = DiscountApprovalStatuses.UnderReview,
            NewStatus = DiscountApprovalStatuses.Approved,
            PerformedBy = "2"
        };
        var comment = new DiscountApprovalCommentDto
        {
            Comment = "Please attach customer email justification",
            CommentedBy = "1"
        };

        Assert.Equal(DiscountApprovalHistoryActions.Approved, history.Action);
        Assert.Contains("justification", comment.Comment);
    }

    [Fact]
    public void Resubmit_is_allowed_only_from_returned()
    {
        Assert.True(DiscountApprovalStatusRules.CanTransition(
            DiscountApprovalStatuses.Returned, DiscountApprovalStatuses.Pending));
        Assert.False(DiscountApprovalStatusRules.CanTransition(
            DiscountApprovalStatuses.Rejected, DiscountApprovalStatuses.Pending));
    }

    [Fact]
    public void Numbering_prefix_matches_da_year_pattern()
    {
        Assert.Matches(@"^DA-\d{4}-\d{5}$", "DA-2026-00001");
        Assert.Matches(@"^DA-\d{4}-\d{5}$", "DA-2026-00010");
    }

    [Fact]
    public void Editable_statuses_are_pending_and_returned()
    {
        Assert.True(DiscountApprovalStatusRules.IsEditable(DiscountApprovalStatuses.Pending));
        Assert.True(DiscountApprovalStatusRules.IsEditable(DiscountApprovalStatuses.Returned));
        Assert.False(DiscountApprovalStatusRules.IsEditable(DiscountApprovalStatuses.Approved));
        Assert.True(DiscountApprovalStatusRules.IsTerminal(DiscountApprovalStatuses.Rejected));
    }
}
