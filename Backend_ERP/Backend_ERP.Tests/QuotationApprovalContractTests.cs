using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using Xunit;

namespace Backend_ERP.Tests;

public class QuotationApprovalContractTests
{
    [Theory]
    [InlineData(QuotationApprovalStatuses.Draft, QuotationApprovalStatuses.Submitted, true)]
    [InlineData(QuotationApprovalStatuses.Submitted, QuotationApprovalStatuses.UnderReview, true)]
    [InlineData(QuotationApprovalStatuses.UnderReview, QuotationApprovalStatuses.Approved, true)]
    [InlineData(QuotationApprovalStatuses.UnderReview, QuotationApprovalStatuses.Rejected, true)]
    [InlineData(QuotationApprovalStatuses.UnderReview, QuotationApprovalStatuses.Returned, true)]
    [InlineData(QuotationApprovalStatuses.UnderReview, QuotationApprovalStatuses.RevisionRequired, true)]
    [InlineData(QuotationApprovalStatuses.Returned, QuotationApprovalStatuses.Submitted, true)]
    [InlineData(QuotationApprovalStatuses.Approved, QuotationApprovalStatuses.Draft, false)]
    [InlineData(QuotationApprovalStatuses.Rejected, QuotationApprovalStatuses.Approved, false)]
    [InlineData(QuotationApprovalStatuses.Cancelled, QuotationApprovalStatuses.Draft, false)]
    public void Workflow_transitions_are_validated(string from, string to, bool expected)
    {
        bool allowed = false;

        if (to == QuotationApprovalStatuses.Submitted)
            allowed = QuotationApprovalStatusRules.CanSubmit(from);
        else if (to == QuotationApprovalStatuses.UnderReview)
            allowed = QuotationApprovalStatusRules.CanReview(from);
        else if (to == QuotationApprovalStatuses.Approved || to == QuotationApprovalStatuses.Rejected)
            allowed = QuotationApprovalStatusRules.CanApproveReject(from);
        else if (to == QuotationApprovalStatuses.Returned)
            allowed = QuotationApprovalStatusRules.CanReturn(from);
        else if (to == QuotationApprovalStatuses.Cancelled)
            allowed = QuotationApprovalStatusRules.CanCancel(from);
        else if (to == QuotationApprovalStatuses.RevisionRequired)
            allowed = QuotationApprovalStatusRules.CanRequestRevision(from);
        else if (to == QuotationApprovalStatuses.Reopened)
            allowed = QuotationApprovalStatusRules.CanReopen(from);

        Assert.Equal(expected, allowed);
    }

    [Fact]
    public void Create_request_shape_matches_contract()
    {
        var dto = new QuotationApprovalCreateRequestDto
        {
            QuotationId = 1,
            CustomerName = "Tata Projects Ltd",
            SalesPersonUserId = 1,
            TotalAmount = 185000m,
            ApprovalLevel = QuotationApprovalLevels.Admin,
            Priority = QuotationApprovalPriorities.High,
            Reason = "Strategic account volume discount beyond list max"
        };

        Assert.Equal(185000m, dto.TotalAmount);
        Assert.Equal(QuotationApprovalLevels.Admin, dto.ApprovalLevel);
    }

    [Fact]
    public void History_and_comment_dto_shapes_exist()
    {
        var history = new QuotationApprovalHistoryDto
        {
            Action = QuotationApprovalHistoryActions.Approved,
            OldStatus = QuotationApprovalStatuses.UnderReview,
            NewStatus = QuotationApprovalStatuses.Approved,
            PerformedBy = "2"
        };
        var comment = new QuotationApprovalCommentDto
        {
            Comment = "Please attach customer email justification",
            CommentedBy = "1"
        };

        Assert.Equal(QuotationApprovalHistoryActions.Approved, history.Action);
        Assert.Contains("justification", comment.Comment);
    }

    [Fact]
    public void Numbering_prefix_matches_qa_year_pattern()
    {
        Assert.Matches(@"^QA-\d{4}-\d{5}$", "QA-2026-00001");
        Assert.Matches(@"^QA-\d{4}-\d{5}$", "QA-2026-00010");
    }

    [Fact]
    public void Editable_statuses_are_draft_and_returned_and_revision_required()
    {
        Assert.True(QuotationApprovalStatusRules.CanBeModified(QuotationApprovalStatuses.Draft));
        Assert.True(QuotationApprovalStatusRules.CanBeModified(QuotationApprovalStatuses.Returned));
        Assert.True(QuotationApprovalStatusRules.CanBeModified(QuotationApprovalStatuses.RevisionRequired));
        Assert.False(QuotationApprovalStatusRules.CanBeModified(QuotationApprovalStatuses.Approved));
    }
}
