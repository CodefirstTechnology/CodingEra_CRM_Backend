using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using Xunit;

namespace Backend_ERP.Tests;

public class ProformaInvoiceContractTests
{
    [Fact]
    public void Calculator_computes_line_tax_and_totals()
    {
        var items = new List<ProformaInvoiceItemDto>
        {
            new()
            {
                ItemName = "MS Plate",
                Quantity = 2,
                Rate = 1000,
                Discount = 10,
                Gst = 18
            }
        };

        Assert.Equal(324m, ProformaInvoiceCalculator.CalcLineTaxAmount(2, 1000, 10, 18));
        Assert.Equal(2124m, ProformaInvoiceCalculator.CalcLineAmount(2, 1000, 10, 18));

        var totals = ProformaInvoiceCalculator.Summarize(items);
        Assert.Equal(2000m, totals.Subtotal);
        Assert.Equal(200m, totals.DiscountTotal);
        Assert.Equal(324m, totals.TaxTotal);
        Assert.Equal(2124m, totals.GrandTotal);
    }

    [Theory]
    [InlineData(ProformaInvoiceStatuses.Draft, ProformaInvoiceStatuses.Submitted, true)]
    [InlineData(ProformaInvoiceStatuses.Submitted, ProformaInvoiceStatuses.PendingFinanceApproval, true)]
    [InlineData(ProformaInvoiceStatuses.PendingFinanceApproval, ProformaInvoiceStatuses.Approved, true)]
    [InlineData(ProformaInvoiceStatuses.Approved, ProformaInvoiceStatuses.Sent, true)]
    [InlineData(ProformaInvoiceStatuses.Accepted, ProformaInvoiceStatuses.Converted, true)]
    [InlineData(ProformaInvoiceStatuses.Draft, ProformaInvoiceStatuses.Converted, false)]
    [InlineData(ProformaInvoiceStatuses.Converted, ProformaInvoiceStatuses.Draft, false)]
    public void Status_transitions_are_validated(string from, string to, bool expected)
    {
        Assert.Equal(expected, ProformaInvoiceStatusRules.CanTransition(from, to));
    }

    [Fact]
    public void Normalize_accepts_spaced_and_compact_pending_finance_status()
    {
        Assert.Equal(
            ProformaInvoiceStatuses.PendingFinanceApproval,
            ProformaInvoiceStatusRules.Normalize("PendingFinanceApproval"));
        Assert.Equal(
            ProformaInvoiceStatuses.PendingFinanceApproval,
            ProformaInvoiceStatusRules.Normalize("Pending Finance Approval"));
    }

    [Fact]
    public void Create_request_shape_matches_frontend_contract()
    {
        var dto = new ProformaInvoiceCreateRequestDto
        {
            InvoiceDate = "2026-08-04",
            ValidUntil = "2026-09-04",
            Currency = "INR",
            Customer = new ProformaInvoiceCustomerDto
            {
                CustomerId = "c-1",
                CustomerName = "ABC Steel Pvt Ltd",
                ContactPerson = "Rajesh",
                BillingAddress = "Pune",
                ShippingAddress = "Pune"
            },
            Items =
            [
                new ProformaInvoiceItemDto
                {
                    Id = "line-1",
                    ItemName = "GI Coil",
                    Quantity = 1,
                    Unit = "Kg",
                    Rate = 100,
                    Discount = 0,
                    Gst = 18
                }
            ]
        };

        Assert.Equal("ABC Steel Pvt Ltd", dto.Customer.CustomerName);
        Assert.Single(dto.Items);
    }
}
