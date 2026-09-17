using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Sales;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend_ERP.Tests;

public class ProformaInvoicePhase2Tests
{
    private ERPDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ERPDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ERPDbContext(options);
    }

    private ProformaInvoiceService CreateService(ERPDbContext db)
    {
        var repo = new ProformaInvoiceRepository(db);
        return new ProformaInvoiceService(repo, db);
    }

    [Fact]
    public async Task Test_Partial_Conversion_Updates_Quantities()
    {
        using var db = CreateInMemoryDbContext();
        var service = CreateService(db);

        var pi = new ProformaInvoice
        {
            Id = 1,
            PiNumber = "PI-2026-00001",
            Status = ProformaInvoiceStatuses.Accepted,
            GrandTotal = 1000m,
            Items = new List<ProformaInvoiceItem>
            {
                new()
                {
                    Id = 10,
                    LineKey = "10",
                    ItemName = "Steel Pipe",
                    Quantity = 10m,
                    ConvertedQuantity = 0m,
                    Rate = 100m,
                    Amount = 1000m
                }
            }
        };
        db.ProformaInvoices.Add(pi);
        await db.SaveChangesAsync();

        var request = new ProformaInvoiceConvertRequestDto
        {
            TargetType = "TaxInvoice",
            Items = new List<ProformaInvoiceConversionItemDto>
            {
                new(10, 4m)
            }
        };

        var result = await service.ConvertAsync(1, request, "test-user");

        Assert.NotNull(result);
        Assert.Equal(ProformaInvoiceStatuses.PartiallyConverted, result.Status);
        Assert.Equal(4m, result.Items[0].ConvertedQuantity);
        Assert.Equal(6m, result.Items[0].RemainingQuantity);

        var stored = await db.ProformaInvoices.Include(x => x.Items).FirstAsync(x => x.Id == 1);
        Assert.Equal(ProformaInvoiceStatuses.PartiallyConverted, stored.Status);
        Assert.Equal(4m, stored.Items.First().ConvertedQuantity);
        Assert.Equal(6m, stored.Items.First().RemainingQuantity);
    }

    [Fact]
    public async Task Test_Over_Conversion_Throws_Error()
    {
        using var db = CreateInMemoryDbContext();
        var service = CreateService(db);

        var pi = new ProformaInvoice
        {
            Id = 2,
            PiNumber = "PI-2026-00002",
            Status = ProformaInvoiceStatuses.PartiallyConverted,
            GrandTotal = 1000m,
            Items = new List<ProformaInvoiceItem>
            {
                new()
                {
                    Id = 20,
                    LineKey = "20",
                    ItemName = "Copper Wire",
                    Quantity = 10m,
                    ConvertedQuantity = 4m,
                    Rate = 100m,
                    Amount = 1000m
                }
            }
        };
        db.ProformaInvoices.Add(pi);
        await db.SaveChangesAsync();

        var request = new ProformaInvoiceConvertRequestDto
        {
            TargetType = "TaxInvoice",
            Items = new List<ProformaInvoiceConversionItemDto>
            {
                new(20, 7m) // Exceeds remaining (6)
            }
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ConvertAsync(2, request, "test-user"));

        Assert.Contains("exceeds remaining unbilled qty", ex.Message);
    }

    [Fact]
    public async Task Test_Full_Conversion_Completes_State()
    {
        using var db = CreateInMemoryDbContext();
        var service = CreateService(db);

        var pi = new ProformaInvoice
        {
            Id = 3,
            PiNumber = "PI-2026-00003",
            Status = ProformaInvoiceStatuses.PartiallyConverted,
            GrandTotal = 1000m,
            Items = new List<ProformaInvoiceItem>
            {
                new()
                {
                    Id = 30,
                    LineKey = "30",
                    ItemName = "Aluminum Sheet",
                    Quantity = 10m,
                    ConvertedQuantity = 4m,
                    Rate = 100m,
                    Amount = 1000m
                }
            }
        };
        db.ProformaInvoices.Add(pi);
        await db.SaveChangesAsync();

        var request = new ProformaInvoiceConvertRequestDto
        {
            TargetType = "TaxInvoice",
            Items = new List<ProformaInvoiceConversionItemDto>
            {
                new(30, 6m)
            }
        };

        var result = await service.ConvertAsync(3, request, "test-user");

        Assert.NotNull(result);
        Assert.Equal(ProformaInvoiceStatuses.Converted, result.Status);
        Assert.Equal(10m, result.Items[0].ConvertedQuantity);
        Assert.Equal(0m, result.Items[0].RemainingQuantity);
    }

    [Fact]
    public async Task Test_Upi_Intent_Formatting()
    {
        using var db = CreateInMemoryDbContext();
        var service = CreateService(db);

        var pi = new ProformaInvoice
        {
            Id = 4,
            PiNumber = "PI-2026-00004",
            Status = ProformaInvoiceStatuses.Approved,
            GrandTotal = 2500.50m,
            AdvanceReceivedAmount = 500m
        };
        db.ProformaInvoices.Add(pi);
        await db.SaveChangesAsync();

        var upi = await service.GetUpiDetailsAsync(4, "billing@testcorp", "Test Corp India", default);

        Assert.NotNull(upi);
        Assert.Equal("billing@testcorp", upi.PayeeVpa);
        Assert.Equal("Test Corp India", upi.PayeeName);
        Assert.Equal(2000.50m, upi.NetPayableAmount);
        Assert.Equal("PI-2026-00004", upi.ReferenceNumber);

        Assert.StartsWith("upi://pay?", upi.UpiIntentString);
        Assert.Contains("pa=billing%40testcorp", upi.UpiIntentString);
        Assert.Contains("pn=Test%20Corp%20India", upi.UpiIntentString);
        Assert.Contains("am=2000.50", upi.UpiIntentString);
        Assert.Contains("cu=INR", upi.UpiIntentString);
        Assert.Contains("tn=Payment%20for%20PI-2026-00004", upi.UpiIntentString);
    }

    [Fact]
    public async Task Test_Full_Payment_Triggers_Production_Release()
    {
        using var db = CreateInMemoryDbContext();
        var repo = new AdvancePaymentRepository(db);
        var numbering = new AdvancePaymentNumberingService(db);
        var apService = new AdvancePaymentService(repo, numbering, db);

        var so = new SalesOrder
        {
            Id = 501,
            SalesOrderNumber = "SO-2026-00501",
            CustomerName = "Apex Ltd",
            OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Subtotal = 10000m,
            GrandTotal = 10000m,
            Remarks = "Payment Hold pending advance",
            Status = SalesOrderStatuses.Confirmed
        };
        db.SalesOrders.Add(so);

        var pi = new ProformaInvoice
        {
            Id = 5,
            PiNumber = "PI-2026-00005",
            SalesOrderId = 501,
            SalesOrderNumber = "SO-2026-00501",
            Status = ProformaInvoiceStatuses.Approved,
            GrandTotal = 10000m,
            AdvanceReceivedAmount = 0m,
            PaymentStatus = "Unpaid",
            IsProductionReleased = false
        };
        db.ProformaInvoices.Add(pi);

        var adv = new AdvancePayment
        {
            Id = 601,
            PaymentNumber = "ADV-2026-00601",
            CustomerId = "1",
            CustomerName = "Apex Ltd",
            PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PaymentMode = AdvancePaymentModes.NEFT,
            Currency = "INR",
            AdvanceAmount = 10000m,
            AppliedAmount = 0m,
            RemainingAmount = 10000m,
            Status = AdvancePaymentStatuses.Received
        };
        db.AdvancePayments.Add(adv);
        await db.SaveChangesAsync();

        var applyDto = new AdvancePaymentApplyRequestDto
        {
            SalesOrderId = 501,
            SalesOrderNumber = "SO-2026-00501",
            ApplyAmount = 10000m
        };

        var result = await apService.ApplyAsync(601, applyDto, "finance-user");

        Assert.NotNull(result);

        var updatedPi = await db.ProformaInvoices.FirstAsync(x => x.Id == 5);
        Assert.Equal("FullyPaid", updatedPi.PaymentStatus);
        Assert.True(updatedPi.IsProductionReleased);
        Assert.NotNull(updatedPi.ProductionReleasedOn);

        var updatedSo = await db.SalesOrders.FirstAsync(x => x.Id == 501);
        Assert.Contains("Payment Cleared", updatedSo.Remarks);
    }
}
