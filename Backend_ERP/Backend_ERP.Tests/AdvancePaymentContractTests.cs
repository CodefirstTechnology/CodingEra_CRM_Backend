using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using Xunit;

namespace Backend_ERP.Tests;

public class AdvancePaymentContractTests
{
    [Fact]
    public void Calculator_computes_remaining_and_prevents_over_allocation()
    {
        Assert.Equal(7000m, AdvancePaymentCalculator.CalcRemaining(10000m, 3000m));
        Assert.True(AdvancePaymentCalculator.WouldOverAllocate(2000m, 2500m));
        Assert.False(AdvancePaymentCalculator.WouldOverAllocate(2000m, 2000m));
        Assert.Equal(AdvancePaymentStatuses.FullyApplied, AdvancePaymentCalculator.ResolveStatusAfterApply(0m));
        Assert.Equal(AdvancePaymentStatuses.PartiallyApplied, AdvancePaymentCalculator.ResolveStatusAfterApply(100m));
    }

    [Theory]
    [InlineData(AdvancePaymentStatuses.Draft, AdvancePaymentStatuses.Submitted, true)]
    [InlineData(AdvancePaymentStatuses.Draft, AdvancePaymentStatuses.Cancelled, true)]
    [InlineData(AdvancePaymentStatuses.Submitted, AdvancePaymentStatuses.FinanceVerification, true)]
    [InlineData(AdvancePaymentStatuses.Submitted, AdvancePaymentStatuses.Rejected, true)]
    [InlineData(AdvancePaymentStatuses.FinanceVerification, AdvancePaymentStatuses.Received, true)]
    [InlineData(AdvancePaymentStatuses.FinanceVerification, AdvancePaymentStatuses.Rejected, true)]
    [InlineData(AdvancePaymentStatuses.Received, AdvancePaymentStatuses.PartiallyApplied, true)]
    [InlineData(AdvancePaymentStatuses.Received, AdvancePaymentStatuses.FullyApplied, true)]
    [InlineData(AdvancePaymentStatuses.PartiallyApplied, AdvancePaymentStatuses.FullyApplied, true)]
    [InlineData(AdvancePaymentStatuses.Draft, AdvancePaymentStatuses.Received, false)]
    [InlineData(AdvancePaymentStatuses.Received, AdvancePaymentStatuses.Rejected, false)]
    [InlineData(AdvancePaymentStatuses.FullyApplied, AdvancePaymentStatuses.Draft, false)]
    [InlineData(AdvancePaymentStatuses.Cancelled, AdvancePaymentStatuses.Submitted, false)]
    public void Status_transitions_are_validated(string from, string to, bool expected)
    {
        Assert.Equal(expected, AdvancePaymentStatusRules.CanTransition(from, to));
    }

    [Theory]
    [InlineData(AdvancePaymentStatuses.Received, true)]
    [InlineData(AdvancePaymentStatuses.PartiallyApplied, true)]
    [InlineData(AdvancePaymentStatuses.Draft, false)]
    [InlineData(AdvancePaymentStatuses.Submitted, false)]
    [InlineData(AdvancePaymentStatuses.FullyApplied, false)]
    public void Apply_is_only_allowed_from_received_or_partial(string status, bool expected)
    {
        Assert.Equal(expected, AdvancePaymentStatusRules.CanApply(status));
    }

    [Fact]
    public void Create_request_shape_matches_frontend_contract()
    {
        var dto = new AdvancePaymentCreateRequestDto
        {
            CustomerId = "c-1",
            CustomerName = "Tata Steel Ltd",
            PaymentDate = "2026-08-04",
            PaymentMode = AdvancePaymentModes.NEFT,
            ReferenceNumber = "NEFT-884421",
            Currency = "INR",
            AdvanceAmount = 250000m,
            SalesOrderId = 1,
            QuotationId = 10,
            QuotationNumber = "QT-2026-0010"
        };

        Assert.Equal("Tata Steel Ltd", dto.CustomerName);
        Assert.Equal(250000m, dto.AdvanceAmount);
        Assert.Equal(AdvancePaymentModes.NEFT, dto.PaymentMode);
    }

    [Fact]
    public void Payment_mode_normalize_accepts_compact_and_spaced_values()
    {
        Assert.Equal(AdvancePaymentModes.BankTransfer, AdvancePaymentModeRules.Normalize("Bank Transfer"));
        Assert.Equal(AdvancePaymentModes.BankTransfer, AdvancePaymentModeRules.Normalize("BankTransfer"));
        Assert.Equal(AdvancePaymentModes.UPI, AdvancePaymentModeRules.Normalize("upi"));
    }

    [Fact]
    public void Timeline_dto_shape_is_available()
    {
        var dto = new AdvancePaymentTimelineDto
        {
            Id = 1,
            Action = AdvancePaymentTimelineActions.Submitted,
            Remarks = "Draft → Submitted",
            PerformedBy = "1",
            PerformedOn = "2026-08-04T10:00:00.0000000Z"
        };

        Assert.Equal(AdvancePaymentTimelineActions.Submitted, dto.Action);
        Assert.Equal("1", dto.PerformedBy);
    }

    [Fact]
    public void Apply_request_requires_positive_amount_and_sales_order()
    {
        var dto = new AdvancePaymentApplyRequestDto
        {
            SalesOrderId = 5,
            ApplyAmount = 15000m,
            Remarks = "Against SO advance"
        };

        Assert.True(dto.SalesOrderId > 0);
        Assert.True(dto.ApplyAmount > 0);
    }
}
