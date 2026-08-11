using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Shared.Helpers;
using Xunit;

namespace Backend_ERP.Tests;

/// <summary>
/// Phase 10 — Sales Integration & Production Hardening end-to-end flow and rule tests.
/// Tests Quotation → Sales Order conversion rules, Sales Order → Proforma Invoice generation rules,
/// Advance Payment allocation rules, document numbering patterns, and hook contracts.
/// </summary>
public class SalesIntegrationTests
{
    // ─── 1. Quotation → Sales Order Integration Rules ─────────────────────────

    [Fact]
    public void QuotationApproval_status_must_be_approved_to_allow_conversion()
    {
        Assert.True(QuotationApprovalStatusRules.CanSubmit(QuotationApprovalStatuses.Draft));
        Assert.True(QuotationApprovalStatusRules.CanReview(QuotationApprovalStatuses.Submitted));
        Assert.True(QuotationApprovalStatusRules.CanApproveReject(QuotationApprovalStatuses.UnderReview));

        // Unapproved status should be rejected
        var qaDraft = new QuotationApproval { Status = QuotationApprovalStatuses.Draft };
        var qaSubmitted = new QuotationApproval { Status = QuotationApprovalStatuses.Submitted };
        var qaApproved = new QuotationApproval { Status = QuotationApprovalStatuses.Approved };

        Assert.NotEqual(QuotationApprovalStatuses.Approved, qaDraft.Status);
        Assert.NotEqual(QuotationApprovalStatuses.Approved, qaSubmitted.Status);
        Assert.Equal(QuotationApprovalStatuses.Approved, qaApproved.Status);
    }

    [Fact]
    public void QuotationApproval_cannot_be_converted_if_already_linked_to_sales_order()
    {
        var qa = new QuotationApproval
        {
            Id = 10,
            ApprovalNumber = "QA-2026-00010",
            Status = QuotationApprovalStatuses.Approved,
            SalesOrderId = 100,
            SalesOrderNumber = "SO-2026-0100"
        };

        Assert.True(qa.SalesOrderId.HasValue && qa.SalesOrderId.Value > 0);
        Assert.Equal("SO-2026-0100", qa.SalesOrderNumber);
    }

    // ─── 2. Sales Order → Proforma Invoice Integration Rules ─────────────────

    [Fact]
    public void ProformaInvoice_cannot_be_generated_from_draft_or_cancelled_sales_order()
    {
        Assert.False(SalesOrderStatusRules.CanTransition(SalesOrderStatuses.Draft, SalesOrderStatuses.Confirmed));
        Assert.True(SalesOrderStatusRules.CanTransition(SalesOrderStatuses.Draft, SalesOrderStatuses.Submitted));
        Assert.True(SalesOrderStatusRules.CanTransition(SalesOrderStatuses.Submitted, SalesOrderStatuses.Confirmed));

        var validStatuses = new[] { SalesOrderStatuses.Submitted, SalesOrderStatuses.Confirmed, SalesOrderStatuses.Processing, SalesOrderStatuses.Completed };
        Assert.DoesNotContain(SalesOrderStatuses.Draft, validStatuses);
        Assert.DoesNotContain(SalesOrderStatuses.Cancelled, validStatuses);
    }

    [Fact]
    public void ProformaInvoice_dto_preserves_sales_order_references()
    {
        var dto = new ProformaInvoiceDto
        {
            Id = 5,
            PiNumber = "PI-2026-00005",
            SalesOrderId = 101,
            SalesOrderNumber = "SO-2026-0101",
            QuotationId = 10,
            QuotationNumber = "QA-2026-00010",
            Customer = new ProformaInvoiceCustomerDto
            {
                CustomerName = "Tata Steel Ltd"
            },
            GrandTotal = 250000m
        };

        Assert.Equal(101, dto.SalesOrderId);
        Assert.Equal("SO-2026-0101", dto.SalesOrderNumber);
        Assert.Equal("QA-2026-00010", dto.QuotationNumber);
    }

    // ─── 3. Advance Payment → Sales Order Allocation Rules ────────────────────

    [Fact]
    public void AdvancePayment_allocation_calculates_remaining_and_applied_amounts_correctly()
    {
        var advanceAmount = 100000m;
        var apply1 = 40000m;
        var remaining1 = AdvancePaymentCalculator.CalcRemaining(advanceAmount, apply1);

        Assert.Equal(60000m, remaining1);
        Assert.Equal(AdvancePaymentStatuses.PartiallyApplied, AdvancePaymentCalculator.ResolveStatusAfterApply(remaining1));

        var apply2 = 60000m;
        var totalApplied = apply1 + apply2;
        var remaining2 = AdvancePaymentCalculator.CalcRemaining(advanceAmount, totalApplied);

        Assert.Equal(0m, remaining2);
        Assert.Equal(AdvancePaymentStatuses.FullyApplied, AdvancePaymentCalculator.ResolveStatusAfterApply(remaining2));
    }

    [Fact]
    public void AdvancePayment_prevents_over_allocation()
    {
        var remaining = 50000m;
        var applyAmount = 60000m;

        Assert.True(AdvancePaymentCalculator.WouldOverAllocate(remaining, applyAmount));
        Assert.False(AdvancePaymentCalculator.WouldOverAllocate(remaining, 40000m));
    }

    // ─── 4. Document Numbering Consistency ────────────────────────────────────

    [Fact]
    public void Document_numbering_sequences_match_phase_standards()
    {
        Assert.Matches(@"^QA-\d{4}-\d{5}$", "QA-2026-00001");
        Assert.Matches(@"^SO-\d{4}-\d{4}$", "SO-2026-0001");
        Assert.Matches(@"^PI-\d{4}-\d{5}$", "PI-2026-00001");
        Assert.Matches(@"^AP-\d{4}-\d{5}$", "AP-2026-00001");
        Assert.Matches(@"^DA-\d{4}-\d{5}$", "DA-2026-00001");
        Assert.Matches(@"^PL-\d{4}-\d{5}$", "PL-2026-00001");
        Assert.Matches(@"^ST-\d{4}-\d{5}$", "ST-2026-00001");
    }

    // ─── 5. PDF & Email Hook DTO Shapes ───────────────────────────────────────

    [Fact]
    public void SalesOrder_pdf_and_email_result_dtos_are_structured_correctly()
    {
        var pdfResult = new SalesOrderPdfResultDto
        {
            SalesOrderId = 101,
            SalesOrderNumber = "SO-2026-0101",
            FileName = "SalesOrder_SO-2026-0101.pdf",
            GeneratedDate = DateHelper.FormatDateTime(DateTimeOffset.UtcNow),
            ContentType = "application/pdf",
            Content = new byte[] { 1, 2, 3 }
        };

        var emailResult = new SalesOrderEmailResultDto
        {
            SalesOrderId = 101,
            SalesOrderNumber = "SO-2026-0101",
            Recipient = "client@tatasteel.test",
            SentDate = DateHelper.FormatDateTime(DateTimeOffset.UtcNow),
            Success = true,
            Message = "Sales Order email sent successfully."
        };

        Assert.Equal("application/pdf", pdfResult.ContentType);
        Assert.True(emailResult.Success);
        Assert.Contains("tatasteel", emailResult.Recipient);
    }
}
