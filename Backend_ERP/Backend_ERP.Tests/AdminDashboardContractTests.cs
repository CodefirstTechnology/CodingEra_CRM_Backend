using System;
using System.Threading.Tasks;
using ERP.API.Controllers;
using ERP.Application.Dashboard.Dtos;
using ERP.Domain.Procurement;
using ERP.Domain.Production;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Dashboard;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERP.Tests
{
    public class AdminDashboardContractTests
    {
        [Fact]
        public void AdminDashboardSummaryDto_ExposesExpectedProperties()
        {
            var dto = new AdminDashboardSummaryDto
            {
                AchievedRevenue = 1500000m,
                TargetRevenue = 2000000m,
                TargetPct = 75
            };

            Assert.Equal(1500000m, dto.AchievedRevenue);
            Assert.Equal(2000000m, dto.TargetRevenue);
            Assert.Equal(75, dto.TargetPct);
            Assert.NotNull(dto.Kpis);
            Assert.NotNull(dto.QuickActions);
            Assert.NotNull(dto.RecentSales);
            Assert.NotNull(dto.RecentPurchase);
            Assert.NotNull(dto.LowStock);
            Assert.NotNull(dto.PendingProduction);
            Assert.NotNull(dto.UpcomingDispatches);
            Assert.NotNull(dto.OutstandingPayments);
            Assert.NotNull(dto.Activities);
            Assert.NotNull(dto.SalesTrend);
            Assert.NotNull(dto.PurchaseTrend);
            Assert.NotNull(dto.InventoryStatus);
            Assert.NotNull(dto.RevenueOverview);
        }

        [Fact]
        public async Task DashboardService_GetSummaryAsync_CalculatesSummaryCorrectly()
        {
            var options = new DbContextOptionsBuilder<ERPDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using (var db = new ERPDbContext(options))
            {
                db.SalesOrders.Add(new SalesOrder
                {
                    Id = 1,
                    SalesOrderNumber = "SO-100",
                    CustomerName = "Test Customer",
                    GrandTotal = 500000m,
                    Status = "Confirmed",
                    CreatedDate = DateTime.UtcNow
                });

                db.PurchaseOrders.Add(new PurchaseOrder
                {
                    Id = 1,
                    PurchaseOrderNumber = "PO-100",
                    VendorName = "Test Vendor",
                    TotalAmount = 250000m,
                    Status = PurchaseOrderStatus.Approved,
                    CreatedAt = DateTime.UtcNow
                });

                db.RawMaterials.Add(new RawMaterial
                {
                    Id = 1,
                    MaterialCode = "RM-01",
                    MaterialName = "Steel Bar",
                    AvailableStock = 50,
                    Unit = "kg"
                });

                await db.SaveChangesAsync();
            }

            using (var db = new ERPDbContext(options))
            {
                var service = new DashboardService(db);
                var summary = await service.GetSummaryAsync();

                Assert.NotNull(summary);
                Assert.Equal(8, summary.Kpis.Count);
                Assert.NotEmpty(summary.RecentSales);
                Assert.NotEmpty(summary.RecentPurchase);
                Assert.NotEmpty(summary.LowStock);
                Assert.Equal(500000m, summary.AchievedRevenue);
            }
        }

        [Fact]
        public async Task AdminDashboardController_GetSummary_ReturnsOkResult()
        {
            var options = new DbContextOptionsBuilder<ERPDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var db = new ERPDbContext(options);
            var service = new DashboardService(db);
            var controller = new AdminDashboardController(service);

            var actionResult = await controller.GetSummary(default);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var summary = Assert.IsType<AdminDashboardSummaryDto>(okResult.Value);

            Assert.NotNull(summary);
            Assert.Equal(8, summary.Kpis.Count);
        }
    }
}
