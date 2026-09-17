using System;
using System.Linq;
using System.Threading.Tasks;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Domain.Sales.Events;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Sales;
using ERP.Infrastructure.Sales.Events;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend_ERP.Tests;

public class SalesTargetPhase2Tests
{
    private static ERPDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ERPDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ERPDbContext(options);
    }

    [Fact]
    public void Test_Commission_Tier_Calculations()
    {
        const decimal target = 100_000m;

        // 75% attainment -> 0
        var c75 = SalesTargetAnalyticsService.CalculateCommission(target, 75_000m);
        Assert.Equal(0m, c75);

        // 90% attainment on ₹1,00,000 -> ₹450 (0.5% of achieved)
        var c90 = SalesTargetAnalyticsService.CalculateCommission(target, 90_000m);
        Assert.Equal(450m, c90);

        // 110% attainment on ₹1,00,000 -> ₹1,650 (1.5% of achieved)
        var c110 = SalesTargetAnalyticsService.CalculateCommission(target, 110_000m);
        Assert.Equal(1_650m, c110);

        // 130% attainment on ₹1,00,000 -> ₹1,500 base + ₹750 kicker = ₹2,250
        var c130 = SalesTargetAnalyticsService.CalculateCommission(target, 130_000m);
        Assert.Equal(2_250m, c130);
    }

    [Fact]
    public async Task Test_Bottom_Up_Rollup()
    {
        using var db = CreateInMemoryDbContext();
        var repo = new SalesTargetRepository(db);
        var numbering = new SalesTargetNumberingService(db);
        var service = new SalesTargetService(repo, numbering, db);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Parent Target (e.g. Team or Branch)
        var parent = new SalesTarget
        {
            Id = 100,
            TargetNumber = "TGT-PARENT-100",
            TargetName = "North Region Team",
            Status = SalesTargetStatuses.Active,
            IsAutoAggregated = true,
            TargetValue = 100_000m,
            AchievedValue = 0m,
            StartDate = today.AddDays(-10),
            EndDate = today.AddDays(20)
        };
        db.SalesTargets.Add(parent);

        // Child 1: ₹50,000
        var child1 = new SalesTarget
        {
            Id = 101,
            TargetNumber = "TGT-CHILD-101",
            TargetName = "Rep 1",
            Status = SalesTargetStatuses.Active,
            ParentTargetId = 100,
            TargetValue = 50_000m,
            AchievedValue = 0m,
            StartDate = today.AddDays(-10),
            EndDate = today.AddDays(20)
        };
        db.SalesTargets.Add(child1);

        // Child 2: ₹50,000
        var child2 = new SalesTarget
        {
            Id = 102,
            TargetNumber = "TGT-CHILD-102",
            TargetName = "Rep 2",
            Status = SalesTargetStatuses.Active,
            ParentTargetId = 100,
            TargetValue = 50_000m,
            AchievedValue = 0m,
            StartDate = today.AddDays(-10),
            EndDate = today.AddDays(20)
        };
        db.SalesTargets.Add(child2);
        await db.SaveChangesAsync();

        // Increment Child 1 achieved value by ₹20,000
        await service.UpdateProgressAsync(101, new SalesTargetProgressUpdateRequestDto
        {
            AchievedValue = 20_000m,
            Remarks = "First deal closed"
        }, "TEST_USER");

        var updatedParent = await db.SalesTargets.FindAsync(100);
        Assert.NotNull(updatedParent);
        Assert.Equal(20_000m, updatedParent!.AchievedValue);
        Assert.Equal(100_000m, updatedParent.TargetValue);
        Assert.Equal(80_000m, updatedParent.RemainingValue);
    }

    [Fact]
    public async Task Test_Proration_Recalculation()
    {
        using var db = CreateInMemoryDbContext();
        var repo = new SalesTargetRepository(db);
        var numbering = new SalesTargetNumberingService(db);
        var service = new SalesTargetService(repo, numbering, db);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var parent = new SalesTarget
        {
            Id = 200,
            TargetNumber = "TGT-PARENT-200",
            TargetName = "West Region",
            Status = SalesTargetStatuses.Active,
            IsAutoAggregated = true,
            TargetValue = 100_000m,
            AchievedValue = 0m,
            StartDate = today.AddDays(-10),
            EndDate = today.AddDays(80)
        };
        db.SalesTargets.Add(parent);

        var child = new SalesTarget
        {
            Id = 201,
            TargetNumber = "TGT-CHILD-201",
            TargetName = "Rep Onboarding",
            Status = SalesTargetStatuses.Active,
            ParentTargetId = 200,
            TargetValue = 100_000m,
            AchievedValue = 0m,
            StartDate = today.AddDays(-10),
            EndDate = today.AddDays(80)
        };
        db.SalesTargets.Add(child);
        await db.SaveChangesAsync();

        // Prorate 45/90 working days
        var prorated = await service.ProrateTargetAsync(201, new ProrateTargetRequestDto(45, 90, "Joined mid-quarter"), "TEST_USER");

        Assert.NotNull(prorated);
        Assert.Equal(50_000m, prorated!.TargetValue);
        Assert.Equal(0.5m, prorated.ProrationFactor);

        var updatedParent = await db.SalesTargets.FindAsync(200);
        Assert.NotNull(updatedParent);
        Assert.Equal(50_000m, updatedParent!.TargetValue);
    }

    [Fact]
    public void Test_Forecast_RunRate_Math()
    {
        var startDate = new DateOnly(2026, 1, 1);
        var endDate = new DateOnly(2026, 1, 31); // 31 days
        var asOfDate = new DateOnly(2026, 1, 16); // 16 days elapsed, 15 days remaining

        var target = new SalesTarget
        {
            TargetValue = 31_000m,
            AchievedValue = 16_000m,
            RemainingValue = 15_000m,
            StartDate = startDate,
            EndDate = endDate
        };

        var forecast = SalesTargetAnalyticsService.ComputeForecast(target, asOfDate);

        // Current daily run rate = 16,000 / 16 = 1,000/day
        Assert.Equal(1_000m, forecast.CurrentDailyRunRate);

        // Required daily run rate = 15,000 / 15 = 1,000/day
        Assert.Equal(1_000m, forecast.RequiredDailyRunRate);

        // Projected attainment: (1,000 * 31) / 31,000 = 100%
        Assert.Equal(100m, forecast.ProjectedAttainmentPercentage);
        Assert.Equal("OnTrack", forecast.HealthStatus);
    }
}
