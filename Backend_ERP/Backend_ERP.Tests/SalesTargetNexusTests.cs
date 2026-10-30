using System;
using System.Linq;
using System.Threading.Tasks;
using ERP.Application.Sales;
using ERP.Domain.Sales;
using ERP.Domain.Sales.Events;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Sales;
using ERP.Infrastructure.Sales.Caching;
using ERP.Infrastructure.Sales.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Backend_ERP.Tests;

public class SalesTargetNexusTests
{
    private static ERPDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ERPDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ERPDbContext(options);
    }

    [Fact]
    public async Task MultiTargetConcurrentAttribution_CreditsRepTeamAndCompanyTargets()
    {
        using var db = CreateInMemoryDbContext();
        var cache = new TargetRedisCacheManager(null);
        var engine = new SalesTargetRealizationEngine(db, cache, NullLogger<SalesTargetRealizationEngine>.Instance);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // 1. Rep Target
        var repTarget = new SalesTarget
        {
            Id = 1001,
            TenantId = "default",
            TargetNumber = "TGT-2026-REP-01",
            TargetName = "Rep Quota",
            AssignmentType = SalesTargetAssignmentTypes.IndividualSalesperson,
            SalesPersonUserId = 88,
            FinancialYear = 2026,
            RevenueTarget = 200_000m,
            StartDate = today.AddDays(-10),
            EndDate = today.AddDays(30),
            Status = SalesTargetStatuses.Active
        };

        // 2. Team Target
        var teamTarget = new SalesTarget
        {
            Id = 1002,
            TenantId = "default",
            TargetNumber = "TGT-2026-TEAM-01",
            TargetName = "Enterprise Team Alpha Quota",
            AssignmentType = SalesTargetAssignmentTypes.SalesTeam,
            SalesTeam = "Enterprise Team Alpha",
            FinancialYear = 2026,
            RevenueTarget = 1_000_000m,
            StartDate = today.AddDays(-10),
            EndDate = today.AddDays(30),
            Status = SalesTargetStatuses.Active
        };

        // 3. Company-Wide Target
        var companyTarget = new SalesTarget
        {
            Id = 1003,
            TenantId = "default",
            TargetNumber = "TGT-2026-CORP-01",
            TargetName = "Global Sales Target",
            AssignmentType = SalesTargetAssignmentTypes.CompanyWide,
            FinancialYear = 2026,
            RevenueTarget = 10_000_000m,
            StartDate = today.AddDays(-10),
            EndDate = today.AddDays(30),
            Status = SalesTargetStatuses.Active
        };

        db.SalesTargets.AddRange(repTarget, teamTarget, companyTarget);

        var order = new SalesOrder
        {
            Id = 5001,
            SalesOrderNumber = "SO-2026-5001",
            SalesPersonUserId = 88,
            SalesPerson = "Enterprise Team Alpha",
            Subtotal = 120_000m,
            DiscountTotal = 20_000m, // Net = 100,000m
            GrandTotal = 118_000m,
            GstTotal = 18_000m,
            Status = "Confirmed",
            OrderDate = today,
            CreatedDate = DateTimeOffset.UtcNow
        };
        db.SalesOrders.Add(order);
        await db.SaveChangesAsync();

        // Act: Process booking
        await engine.RecordOrderBookingAsync(order.Id);

        // Assert: All three targets must have received the 100,000m realization
        var updatedRep = await db.SalesTargets.Include(t => t.Realizations).FirstAsync(t => t.Id == 1001);
        var updatedTeam = await db.SalesTargets.Include(t => t.Realizations).FirstAsync(t => t.Id == 1002);
        var updatedCorp = await db.SalesTargets.Include(t => t.Realizations).FirstAsync(t => t.Id == 1003);

        Assert.Equal(100_000m, updatedRep.RealizedRevenue);
        Assert.Equal(100_000m, updatedRep.AchievedValue);
        Assert.Equal(50.0m, updatedRep.AchievementPercentage); // 100k / 200k = 50%
        Assert.Single(updatedRep.Realizations);

        Assert.Equal(100_000m, updatedTeam.RealizedRevenue);
        Assert.Equal(10.0m, updatedTeam.AchievementPercentage); // 100k / 1M = 10%
        Assert.Single(updatedTeam.Realizations);

        Assert.Equal(100_000m, updatedCorp.RealizedRevenue);
        Assert.Equal(1.0m, updatedCorp.AchievementPercentage); // 100k / 10M = 1%
        Assert.Single(updatedCorp.Realizations);

        // Verify Realization Ledger attributes
        var ledgerEntry = updatedRep.Realizations.First();
        Assert.Equal(SalesTargetRealizationTypes.BookingNet, ledgerEntry.RealizationType);
        Assert.Equal(100_000m, ledgerEntry.RealizedAmount);
        Assert.Equal("INR", ledgerEntry.SourceCurrency);
        Assert.False(ledgerEntry.IsReversal);
    }

    [Fact]
    public async Task OrderClawback_CorrectlyReversesTargetRealization()
    {
        using var db = CreateInMemoryDbContext();
        var cache = new TargetRedisCacheManager(null);
        var engine = new SalesTargetRealizationEngine(db, cache, NullLogger<SalesTargetRealizationEngine>.Instance);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var target = new SalesTarget
        {
            Id = 2001,
            TenantId = "default",
            TargetNumber = "TGT-2026-CLAW-01",
            TargetName = "Clawback Test Quota",
            AssignmentType = SalesTargetAssignmentTypes.IndividualSalesperson,
            SalesPersonUserId = 42,
            FinancialYear = 2026,
            RevenueTarget = 500_000m,
            StartDate = today.AddDays(-5),
            EndDate = today.AddDays(25),
            Status = SalesTargetStatuses.Active
        };
        db.SalesTargets.Add(target);

        var order = new SalesOrder
        {
            Id = 6001,
            SalesOrderNumber = "SO-2026-6001",
            SalesPersonUserId = 42,
            Subtotal = 150_000m,
            GrandTotal = 150_000m,
            Status = "Confirmed",
            OrderDate = today,
            CreatedDate = DateTimeOffset.UtcNow
        };
        db.SalesOrders.Add(order);
        await db.SaveChangesAsync();

        // 1. Credit booking
        await engine.RecordOrderBookingAsync(order.Id);

        var afterBooking = await db.SalesTargets.Include(t => t.Realizations).FirstAsync(t => t.Id == 2001);
        Assert.Equal(150_000m, afterBooking.RealizedRevenue);
        Assert.Single(afterBooking.Realizations);

        // 2. Clawback cancellation
        await engine.ProcessOrderClawbackAsync(order.Id, "Customer cancelled contract");

        var afterClawback = await db.SalesTargets.Include(t => t.Realizations).FirstAsync(t => t.Id == 2001);
        Assert.Equal(0m, afterClawback.RealizedRevenue);
        Assert.Equal(0m, afterClawback.AchievedValue);
        Assert.Equal(2, afterClawback.Realizations.Count);

        var reversalEntry = afterClawback.Realizations.First(r => r.IsReversal);
        Assert.True(reversalEntry.IsReversal);
        Assert.Equal("Customer cancelled contract", reversalEntry.ReversalReason);
        Assert.Equal(afterClawback.Realizations.First(r => !r.IsReversal).Id, reversalEntry.ReversalOfRealizationId);
    }

    [Fact]
    public async Task AdvanceCashRealization_RecordsCashInflowSeparatelyFromBooking()
    {
        using var db = CreateInMemoryDbContext();
        var cache = new TargetRedisCacheManager(null);
        var engine = new SalesTargetRealizationEngine(db, cache, NullLogger<SalesTargetRealizationEngine>.Instance);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var target = new SalesTarget
        {
            Id = 3001,
            TenantId = "default",
            TargetNumber = "TGT-2026-CASH-01",
            TargetName = "Cash Flow Tracking Quota",
            AssignmentType = SalesTargetAssignmentTypes.IndividualSalesperson,
            SalesPersonUserId = 99,
            FinancialYear = 2026,
            RevenueTarget = 1_000_000m,
            StartDate = today.AddDays(-10),
            EndDate = today.AddDays(20),
            Status = SalesTargetStatuses.Active
        };
        db.SalesTargets.Add(target);

        var order = new SalesOrder
        {
            Id = 7001,
            SalesOrderNumber = "SO-2026-7001",
            SalesPersonUserId = 99,
            Subtotal = 300_000m,
            GrandTotal = 300_000m,
            OrderDate = today,
            CreatedDate = DateTimeOffset.UtcNow
        };
        db.SalesOrders.Add(order);

        var advance = new AdvancePayment
        {
            Id = 8001,
            SalesOrderId = order.Id,
            SalesOrder = order,
            AdvanceAmount = 60_000m,
            PaymentDate = today,
            Currency = "INR",
            ExchangeRate = 1.0m,
            Status = "Realized"
        };
        db.AdvancePayments.Add(advance);
        await db.SaveChangesAsync();

        // 1. Credit booking
        await engine.RecordOrderBookingAsync(order.Id);

        // 2. Credit advance cash flow
        await engine.RecordAdvanceCashAsync(advance.Id);

        var targetState = await db.SalesTargets.Include(t => t.Realizations).FirstAsync(t => t.Id == 3001);

        // Both revenue booking (300k) and cash realization (60k) are maintained concurrently
        Assert.Equal(300_000m, targetState.RealizedRevenue);
        Assert.Equal(60_000m, targetState.RealizedCash);
        Assert.Equal(2, targetState.Realizations.Count);

        var cashRealization = targetState.Realizations.First(r => r.RealizationType == SalesTargetRealizationTypes.CashCollected);
        Assert.Equal(60_000m, cashRealization.RealizedAmount);
        Assert.Equal(8001, cashRealization.AdvancePaymentId);
    }
}

