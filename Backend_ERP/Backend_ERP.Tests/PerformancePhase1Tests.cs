using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Domain.Sales.Events;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Sales;
using ERP.Infrastructure.Sales.Events;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERP.Tests
{
    public class PerformancePhase1Tests
    {
        private ERPDbContext GetInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<ERPDbContext>()
                .UseInMemoryDatabase(databaseName: $"PerformancePhase1Db_{Guid.NewGuid()}")
                .Options;
            return new ERPDbContext(options);
        }

        [Fact]
        public void Test_WeightedScore_Calculation_Formula()
        {
            // Attainment 100%, Conversion 50%, Margin 20%
            // Formula: (100 * 0.5) + (50 * 0.3) + (20 * 0.2) = 50 + 15 + 4 = 69.00
            decimal attainment = 100m;
            decimal conversion = 50m;
            decimal margin = 20m;

            decimal weightedScore = Math.Round(
                (attainment * 0.50m) +
                (conversion * 0.30m) +
                (margin * 0.20m), 2, MidpointRounding.AwayFromZero);

            Assert.Equal(69.00m, weightedScore);
        }

        [Fact]
        public async Task Test_Leaderboard_Sorting_By_WeightedScore()
        {
            using var db = GetInMemoryDb();
            var repo = new PerformanceRepository(db);

            // Rep 1: Higher revenue, but lower WeightedScore
            db.SalespersonPerformances.Add(new SalespersonPerformance
            {
                SalesPersonUserId = 1,
                SalesPersonName = "Rep 1",
                FinancialYear = 2026,
                TotalAchievedValue = 500000m,
                AttainmentPercentage = 50m,
                ConversionRate = 10m,
                GrossMarginPercentage = 10m,
                WeightedScore = (50m * 0.50m) + (10m * 0.30m) + (10m * 0.20m), // 25 + 3 + 2 = 30.00
                Status = "Active"
            });

            // Rep 2: Lower revenue, but higher WeightedScore
            db.SalespersonPerformances.Add(new SalespersonPerformance
            {
                SalesPersonUserId = 2,
                SalesPersonName = "Rep 2",
                FinancialYear = 2026,
                TotalAchievedValue = 300000m,
                AttainmentPercentage = 100m,
                ConversionRate = 80m,
                GrossMarginPercentage = 20m,
                WeightedScore = (100m * 0.50m) + (80m * 0.30m) + (20m * 0.20m), // 50 + 24 + 4 = 78.00
                Status = "Active"
            });

            await db.SaveChangesAsync();

            var leaderboard = (await repo.GetLeaderboardAsync(2026)).ToList();

            Assert.Equal(2, leaderboard.Count);
            Assert.Equal(2, leaderboard[0].SalesPersonUserId); // Rep 2 should rank higher (#1)
            Assert.Equal(78.00m, leaderboard[0].WeightedScore);
            Assert.Equal(1, leaderboard[1].SalesPersonUserId); // Rep 1 should rank (#2)
            Assert.Equal(30.00m, leaderboard[1].WeightedScore);
        }

        [Fact]
        public async Task Test_Event_Driven_Recalculation()
        {
            using var db = GetInMemoryDb();
            var recalcService = new PerformanceRecalculationService(db);
            var confirmHandler = new SalesOrderConfirmedEventHandler(db, recalcService);

            // Setup Target for Salesperson 10
            db.SalesTargets.Add(new SalesTarget
            {
                Id = 1,
                SalesPersonUserId = 10,
                FinancialYear = 2026,
                TargetValue = 100000m,
                AchievedValue = 0m,
                TargetCategory = SalesTargetCategories.Revenue,
                Status = SalesTargetStatuses.Active,
                StartDate = new DateOnly(2026, 1, 1),
                EndDate = new DateOnly(2026, 12, 31)
            });

            // Setup initial Quotation and Performance record
            db.Quotations.Add(new Quotation
            {
                Id = 1,
                SalesPerson = "10",
                IsDeleted = false
            });

            db.SalespersonPerformances.Add(new SalespersonPerformance
            {
                SalesPersonUserId = 10,
                FinancialYear = 2026,
                Status = "Active"
            });

            await db.SaveChangesAsync();

            // Create Confirmed Sales Order
            var order = new SalesOrder
            {
                Id = 101,
                SalesPersonUserId = 10,
                GrandTotal = 50000m,
                Status = "Confirmed",
                OrderDate = new DateOnly(2026, 6, 1),
                CreatedDate = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero)
            };
            db.SalesOrders.Add(order);
            await db.SaveChangesAsync();

            // Trigger event
            await confirmHandler.Handle(new SalesOrderConfirmedDomainEvent(order), default);

            var updatedPerf = await db.SalespersonPerformances.FirstOrDefaultAsync(p => p.SalesPersonUserId == 10 && p.FinancialYear == 2026);

            Assert.NotNull(updatedPerf);
            Assert.Equal(1, updatedPerf.ConfirmedOrderCount);
            Assert.Equal(50000m, updatedPerf.TotalAchievedValue);
            Assert.Equal(50m, updatedPerf.AttainmentPercentage);
            Assert.Equal(100m, updatedPerf.ConversionRate);
            Assert.True(updatedPerf.WeightedScore > 0m);
        }
    }
}
