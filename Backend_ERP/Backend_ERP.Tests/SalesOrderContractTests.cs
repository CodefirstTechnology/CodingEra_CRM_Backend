using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using Xunit;

namespace Backend_ERP.Tests;

public class SalesOrderContractTests
{
    [Fact]
    public void Calculator_matches_frontend_line_and_totals_formula()
    {
        var items = new List<SalesOrderItemDto>
        {
            new()
            {
                ItemName = "Beam",
                Quantity = 2,
                Rate = 1000,
                Discount = 10,
                Gst = 18
            }
        };

        // base 2000, discount 200, after disc 1800, gst 324, line 2124
        Assert.Equal(2124m, SalesOrderCalculator.CalcLineAmount(2, 1000, 10, 18));
        Assert.Equal(200m, SalesOrderCalculator.CalcLineDiscountAmount(2, 1000, 10));
        Assert.Equal(324m, SalesOrderCalculator.CalcLineGstAmount(2, 1000, 10, 18));

        var totals = SalesOrderCalculator.Summarize(items);
        Assert.Equal(2000m, totals.Subtotal);
        Assert.Equal(200m, totals.DiscountTotal);
        Assert.Equal(324m, totals.GstTotal);
        Assert.Equal(2124m, totals.GrandTotal);
    }

    [Theory]
    [InlineData(SalesOrderStatuses.Draft, SalesOrderStatuses.Submitted, true)]
    [InlineData(SalesOrderStatuses.Draft, SalesOrderStatuses.Confirmed, false)]
    [InlineData(SalesOrderStatuses.Submitted, SalesOrderStatuses.Confirmed, true)]
    [InlineData(SalesOrderStatuses.Processing, SalesOrderStatuses.Completed, true)]
    [InlineData(SalesOrderStatuses.Completed, SalesOrderStatuses.Cancelled, false)]
    public void Status_transitions_are_validated(string from, string to, bool expected)
    {
        Assert.Equal(expected, SalesOrderStatusRules.CanTransition(from, to));
    }

    [Fact]
    public void Create_request_dto_shape_exposes_frontend_fields()
    {
        var dto = new SalesOrderCreateRequestDto
        {
            OrderDate = "2026-08-04",
            PaymentTerms = "Net 30",
            DeliveryTerms = "Ex-Works",
            Customer = new SalesOrderCustomerDto
            {
                CustomerName = "Acme",
                ContactPerson = "Riya",
                BillingAddress = "Pune",
                ShippingAddress = "Pune",
                CustomerEmail = "a@acme.test",
                CustomerPhone = "9999999999"
            },
            Items =
            [
                new SalesOrderItemDto
                {
                    Id = "line-1",
                    ItemName = "Item A",
                    Description = "Desc",
                    Quantity = 1,
                    Unit = "Nos",
                    Rate = 100,
                    Discount = 0,
                    Gst = 18,
                    Amount = 118
                }
            ]
        };

        Assert.Equal("Acme", dto.Customer.CustomerName);
        Assert.Single(dto.Items);
        Assert.Equal("line-1", dto.Items[0].Id);
    }
}
