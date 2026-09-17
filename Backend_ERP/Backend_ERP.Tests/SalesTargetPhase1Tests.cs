using System;
using System.Linq;
using System.Threading.Tasks;
using ERP.Application.Sales;
using ERP.Domain.Sales;
using ERP.Domain.Sales.Events;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Sales.Events;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend_ERP.Tests;

public class SalesTargetPhase1Tests
{
    private static ERPDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ERPDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ERPDbContext(options);
    }

    [Fact]
    public void Test_Target_Does_Not_Close_At_100_Percent()
    {
        var target = new SalesTarget
        {
            Id = 1,
            TargetName = "FY26 Q2 Rep Target",
            Status = SalesTargetStatuses.Active,
            TargetValue = 100_000m,
            AchievedValue = 125_000m,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20))
        };

        SalesTargetRules.RecalculateProgress(target);

        Assert.Equal(SalesTargetStatuses.Active, target.Status);
        Assert.Equal(0m, target.RemainingValue);
        Assert.Equal(25_000m, target.OverAchievementValue);
        Assert.Equal(125m, target.AchievementPercentage);
    }

    [Fact]
    public async Task Test_Sales_Order_Confirmed_Event_Sync()
    {
        using var db = CreateInMemoryDbContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var target = new SalesTarget
        {
            Id = 10,
            TargetName = "Revenue Target 10",
            TargetCategory = "Revenue",
            SalesPersonUserId = 5,
            Status = SalesTargetStatuses.Active,
            TargetValue = 100_000m,
            AchievedValue = 0m,
            StartDate = today.AddDays(-5),
            EndDate = today.AddDays(25)
        };
        db.SalesTargets.Add(target);

        var order = new SalesOrder
        {
            Id = 101,
            SalesOrderNumber = "SO-2026-0101",
            SalesPersonUserId = 5,
            GrandTotal = 40_000m,
            CreatedDate = DateTimeOffset.UtcNow
        };
        db.SalesOrders.Add(order);
        await db.SaveChangesAsync();

        var handler = new SalesOrderConfirmedEventHandler(db);
        await handler.Handle(new SalesOrderConfirmedDomainEvent(order), default);

        var updated = await db.SalesTargets
            .Include(t => t.Realizations)
            .FirstAsync(t => t.Id == 10);

        Assert.Equal(40_000m, updated.AchievedValue);
        Assert.Equal(60_000m, updated.RemainingValue);
        Assert.Equal(0m, updated.OverAchievementValue);
        Assert.Single(updated.Realizations);

        var realization = updated.Realizations.First();
        Assert.Equal(101, realization.SalesOrderId);
        Assert.Equal(40_000m, realization.RealizedAmount);
        Assert.False(realization.IsReversal);
    }

    [Fact]
    public async Task Test_Deduplication_Prevented()
    {
        using var db = CreateInMemoryDbContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var target = new SalesTarget
        {
            Id = 20,
            TargetName = "Revenue Target 20",
            TargetCategory = "Revenue",
            SalesPersonUserId = 7,
            Status = SalesTargetStatuses.Active,
            TargetValue = 100_000m,
            AchievedValue = 0m,
            StartDate = today.AddDays(-5),
            EndDate = today.AddDays(25)
        };
        db.SalesTargets.Add(target);

        var order = new SalesOrder
        {
            Id = 202,
            SalesOrderNumber = "SO-2026-0202",
            SalesPersonUserId = 7,
            GrandTotal = 30_000m,
            CreatedDate = DateTimeOffset.UtcNow
        };
        db.SalesOrders.Add(order);
        await db.SaveChangesAsync();

        var handler = new SalesOrderConfirmedEventHandler(db);
        // First emit
        await handler.Handle(new SalesOrderConfirmedDomainEvent(order), default);
        // Duplicate emit
        await handler.Handle(new SalesOrderConfirmedDomainEvent(order), default);

        var updated = await db.SalesTargets
            .Include(t => t.Realizations)
            .FirstAsync(t => t.Id == 20);

        Assert.Equal(30_000m, updated.AchievedValue);
        Assert.Single(updated.Realizations);
    }

    [Fact]
    public async Task Test_Cancellation_Reversal()
    {
        using var db = CreateInMemoryDbContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var target = new SalesTarget
        {
            Id = 30,
            TargetName = "Revenue Target 30",
            TargetCategory = "Revenue",
            SalesPersonUserId = 9,
            Status = SalesTargetStatuses.Active,
            TargetValue = 100_000m,
            AchievedValue = 0m,
            StartDate = today.AddDays(-5),
            EndDate = today.AddDays(25)
        };
        db.SalesTargets.Add(target);

        var order = new SalesOrder
        {
            Id = 303,
            SalesOrderNumber = "SO-2026-0303",
            SalesPersonUserId = 9,
            GrandTotal = 50_000m,
            CreatedDate = DateTimeOffset.UtcNow
        };
        db.SalesOrders.Add(order);
        await db.SaveChangesAsync();

        var confirmHandler = new SalesOrderConfirmedEventHandler(db);
        await confirmHandler.Handle(new SalesOrderConfirmedDomainEvent(order), default);

        var cancelHandler = new SalesOrderCancelledEventHandler(db);
        await cancelHandler.Handle(new SalesOrderCancelledDomainEvent(order.Id), default);

        var updated = await db.SalesTargets
            .Include(t => t.Realizations)
            .FirstAsync(t => t.Id == 30);

        Assert.Equal(0m, updated.AchievedValue);
        Assert.Equal(100_000m, updated.RemainingValue);
        Assert.Equal(2, updated.Realizations.Count);

        var reversal = updated.Realizations.FirstOrDefault(r => r.IsReversal);
        Assert.NotNull(reversal);
        Assert.Equal(-50_000m, reversal!.RealizedAmount);
        Assert.Equal(303, reversal.SalesOrderId);
    }
}
