using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Dashboard;
using ERP.Application.Dashboard.Dtos;
using ERP.Domain.Procurement;
using ERP.Domain.Production;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Dashboard
{
    public class DashboardService : IDashboardService
    {
        private readonly ERPDbContext _db;

        public DashboardService(ERPDbContext db)
        {
            _db = db;
        }

        public async Task<AdminDashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var weekAgo = now.AddDays(-7);

            // 1. Core Counts
            var totalSalesOrders = await _db.SalesOrders.AsNoTracking().CountAsync(cancellationToken);
            var recentSalesOrdersWeek = await _db.SalesOrders.AsNoTracking().CountAsync(x => x.CreatedDate >= weekAgo, cancellationToken);

            var totalPurchaseOrders = await _db.PurchaseOrders.AsNoTracking().CountAsync(x => !x.IsDeleted, cancellationToken);
            var openPurchaseOrders = await _db.PurchaseOrders.AsNoTracking().CountAsync(x => !x.IsDeleted && x.Status != PurchaseOrderStatus.Completed, cancellationToken);

            var totalRawMaterials = await _db.RawMaterials.AsNoTracking().CountAsync(x => !x.IsDeleted, cancellationToken);
            var totalFinishedGoods = await _db.FinishedGoods.AsNoTracking().CountAsync(x => !x.IsDeleted, cancellationToken);
            var lowStockMaterials = await _db.RawMaterials.AsNoTracking().CountAsync(x => !x.IsDeleted && x.AvailableStock < 100, cancellationToken);
            var lowStockGoods = await _db.FinishedGoods.AsNoTracking().CountAsync(x => !x.IsDeleted && x.AvailableQuantity < 50, cancellationToken);

            var totalWorkOrders = await _db.WorkOrders.AsNoTracking().CountAsync(x => !x.IsDeleted, cancellationToken);
            var inProgressWorkOrders = await _db.WorkOrders.AsNoTracking().CountAsync(x => !x.IsDeleted && x.Status == WorkOrderStatus.InProgress, cancellationToken);

            var pendingIncomingQc = await _db.IncomingInspections.AsNoTracking().CountAsync(x => !x.IsDeleted && (x.Status == IncomingInspectionStatus.Draft || x.Status == IncomingInspectionStatus.Submitted), cancellationToken);
            var pendingFinalQc = await _db.FinalInspections.AsNoTracking().CountAsync(x => !x.IsDeleted && (x.Status == FinalInspectionStatus.Draft || x.Status == FinalInspectionStatus.Submitted), cancellationToken);

            var todayDispatches = await _db.DispatchPlans.AsNoTracking().CountAsync(x => x.DispatchDate >= now.Date, cancellationToken);

            var totalOutstandingAmount = await _db.CustomerLedgerEntries.AsNoTracking().Where(x => !x.IsDeleted).SumAsync(x => x.Outstanding, cancellationToken);
            var totalOutstandingCount = await _db.CustomerLedgerEntries.AsNoTracking().CountAsync(x => !x.IsDeleted && x.Outstanding > 0, cancellationToken);

            var monthlyRevenueAchieved = await _db.SalesOrders.AsNoTracking().Where(x => x.CreatedDate >= monthStart).SumAsync(x => x.GrandTotal, cancellationToken);
            var monthlyRevenueTarget = await _db.SalesTargets.AsNoTracking().Where(x => !x.IsDeleted).SumAsync(x => x.TargetValue, cancellationToken);

            if (monthlyRevenueTarget == 0) monthlyRevenueTarget = 2250000m; // Default target baseline if unconfigured

            int targetPct = monthlyRevenueTarget > 0 
                ? (int)Math.Min(100, Math.Round((monthlyRevenueAchieved / monthlyRevenueTarget) * 100m)) 
                : 0;

            // 2. KPI Cards
            var kpis = new List<DashboardKpiDto>
            {
                new DashboardKpiDto { Label = "Sales Orders", Value = totalSalesOrders.ToString("N0"), Hint = recentSalesOrdersWeek > 0 ? $"+{recentSalesOrdersWeek} this week" : "Active", Route = "/sales/orders" },
                new DashboardKpiDto { Label = "Purchase Orders", Value = totalPurchaseOrders.ToString("N0"), Hint = $"{openPurchaseOrders} open", Route = "/sales/purchase-order" },
                new DashboardKpiDto { Label = "Inventory Items", Value = (totalRawMaterials + totalFinishedGoods).ToString("N0"), Hint = $"{lowStockMaterials + lowStockGoods} SKUs low", Route = "/store-inventory/raw-materials" },
                new DashboardKpiDto { Label = "Production Orders", Value = totalWorkOrders.ToString("N0"), Hint = $"{inProgressWorkOrders} in progress", Route = "/production/work-orders" },
                new DashboardKpiDto { Label = "Pending Quality Checks", Value = (pendingIncomingQc + pendingFinalQc).ToString("N0"), Hint = $"{pendingIncomingQc} incoming", Route = "/quality-control/incoming-inspection" },
                new DashboardKpiDto { Label = "Today's Dispatches", Value = todayDispatches.ToString("N0"), Hint = $"{todayDispatches} scheduled", Route = "/dispatch-logistics/dispatch-planning" },
                new DashboardKpiDto { Label = "Outstanding Payments", Value = FormatCurrency(totalOutstandingAmount), Hint = $"{totalOutstandingCount} invoices", Route = "/accounting/outstanding" },
                new DashboardKpiDto { Label = "Monthly Revenue", Value = FormatCurrency(monthlyRevenueAchieved), Hint = $"{targetPct}% of target", Route = "/accounting/financial-reports" }
            };

            // 3. Quick Actions
            var quickActions = new List<DashboardQuickActionDto>
            {
                new DashboardQuickActionDto { Label = "Create Sales Order", Route = "/sales/orders/create" },
                new DashboardQuickActionDto { Label = "Create Purchase Order", Route = "/purchase" },
                new DashboardQuickActionDto { Label = "Add Product", Route = "/store-inventory" },
                new DashboardQuickActionDto { Label = "Create Work Order", Route = "/production" },
                new DashboardQuickActionDto { Label = "Dispatch Material", Route = "/dispatch-logistics" },
                new DashboardQuickActionDto { Label = "Receipt Entry", Route = "/accounting/receipt-entry/create" },
                new DashboardQuickActionDto { Label = "Payment Entry", Route = "/accounting/payment-entry/create" }
            };

            // 4. Recent Lists
            var rawRecentSales = await _db.SalesOrders.AsNoTracking()
                .OrderByDescending(x => x.Id)
                .Take(4)
                .Select(x => new { x.SalesOrderNumber, x.CustomerName, x.GrandTotal, x.Status })
                .ToListAsync(cancellationToken);

            var recentSales = rawRecentSales.Select(x => new DashboardListRowDto
            {
                Primary = x.SalesOrderNumber,
                Secondary = x.CustomerName,
                Meta = $"{FormatCurrency(x.GrandTotal)} · {x.Status}",
                Tone = x.Status == "Confirmed" ? "ok" : (x.Status == "Draft" ? "muted" : "warn")
            }).ToList();

            var rawRecentPurchase = await _db.PurchaseOrders.AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.Id)
                .Take(4)
                .Select(x => new { x.PurchaseOrderNumber, x.VendorName, x.TotalAmount, x.Status })
                .ToListAsync(cancellationToken);

            var recentPurchase = rawRecentPurchase.Select(x => new DashboardListRowDto
            {
                Primary = x.PurchaseOrderNumber,
                Secondary = x.VendorName,
                Meta = $"{FormatCurrency(x.TotalAmount)} · {x.Status}",
                Tone = x.Status == PurchaseOrderStatus.Approved || x.Status == PurchaseOrderStatus.Completed ? "ok" : "warn"
            }).ToList();

            var rawLowStock = await _db.RawMaterials.AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.AvailableStock)
                .Take(4)
                .Select(x => new { x.MaterialCode, x.MaterialName, x.AvailableStock, x.Unit })
                .ToListAsync(cancellationToken);

            var lowStock = rawLowStock.Select(x => new DashboardListRowDto
            {
                Primary = x.MaterialCode,
                Secondary = x.MaterialName,
                Meta = $"{x.AvailableStock:N0} {x.Unit} · Min 100",
                Tone = "warn"
            }).ToList();

            var rawPendingProduction = await _db.WorkOrders.AsNoTracking()
                .Where(x => !x.IsDeleted && x.Status != WorkOrderStatus.Completed)
                .OrderByDescending(x => x.Id)
                .Take(3)
                .Select(x => new { x.WorkOrderNumber, x.ProductName, x.Status, x.DueDate })
                .ToListAsync(cancellationToken);

            if (!rawPendingProduction.Any())
            {
                rawPendingProduction = await _db.WorkOrders.AsNoTracking()
                    .Where(x => !x.IsDeleted)
                    .OrderByDescending(x => x.Id)
                    .Take(3)
                    .Select(x => new { x.WorkOrderNumber, x.ProductName, x.Status, x.DueDate })
                    .ToListAsync(cancellationToken);
            }

            var pendingProduction = rawPendingProduction.Select(x => new DashboardListRowDto
            {
                Primary = x.WorkOrderNumber,
                Secondary = x.ProductName,
                Meta = $"Status: {x.Status}",
                Tone = x.Status == WorkOrderStatus.Completed ? "ok" : "warn"
            }).ToList();

            var rawUpcomingDispatches = await _db.DispatchPlans.AsNoTracking()
                .OrderByDescending(x => x.Id)
                .Take(3)
                .Select(x => new { x.DispatchNumber, x.CustomerName, x.Status, x.DispatchDate })
                .ToListAsync(cancellationToken);

            var upcomingDispatches = rawUpcomingDispatches.Select(x => new DashboardListRowDto
            {
                Primary = x.DispatchNumber,
                Secondary = x.CustomerName,
                Meta = $"Status: {x.Status}",
                Tone = x.Status == DispatchPlanStatus.ReadyForDispatch || x.Status == DispatchPlanStatus.Closed ? "ok" : "muted"
            }).ToList();

            var rawOutstanding = await _db.CustomerLedgerEntries.AsNoTracking()
                .Where(x => !x.IsDeleted && x.Outstanding > 0)
                .OrderByDescending(x => x.Outstanding)
                .Take(3)
                .Select(x => new { x.LedgerNumber, x.CustomerName, x.Outstanding })
                .ToListAsync(cancellationToken);

            var outstandingPayments = rawOutstanding.Select(x => new DashboardListRowDto
            {
                Primary = x.LedgerNumber,
                Secondary = x.CustomerName,
                Meta = $"{FormatCurrency(x.Outstanding)} · Outstanding",
                Tone = "warn"
            }).ToList();

            // 5. Activities
            var activities = new List<DashboardActivityDto>();

            var latestSo = await _db.SalesOrders.AsNoTracking().OrderByDescending(x => x.Id).FirstOrDefaultAsync(cancellationToken);
            if (latestSo != null)
            {
                activities.Add(new DashboardActivityDto { Title = "Sales order confirmed", Detail = $"{latestSo.SalesOrderNumber} for {latestSo.CustomerName}", Time = "Recent" });
            }

            var latestQc = await _db.IncomingInspections.AsNoTracking().Where(x => !x.IsDeleted).OrderByDescending(x => x.Id).FirstOrDefaultAsync(cancellationToken);
            if (latestQc != null)
            {
                activities.Add(new DashboardActivityDto { Title = "Quality check processed", Detail = $"{latestQc.InspectionNumber} ({latestQc.Status})", Time = "Recent" });
            }

            var latestPo = await _db.PurchaseOrders.AsNoTracking().Where(x => !x.IsDeleted).OrderByDescending(x => x.Id).FirstOrDefaultAsync(cancellationToken);
            if (latestPo != null)
            {
                activities.Add(new DashboardActivityDto { Title = "Purchase order approved", Detail = $"{latestPo.PurchaseOrderNumber} for {latestPo.VendorName}", Time = "Recent" });
            }

            var latestDp = await _db.DispatchPlans.AsNoTracking().OrderByDescending(x => x.Id).FirstOrDefaultAsync(cancellationToken);
            if (latestDp != null)
            {
                activities.Add(new DashboardActivityDto { Title = "Dispatch scheduled", Detail = $"{latestDp.DispatchNumber} for {latestDp.CustomerName}", Time = "Recent" });
            }

            var latestWo = await _db.WorkOrders.AsNoTracking().Where(x => !x.IsDeleted).OrderByDescending(x => x.Id).FirstOrDefaultAsync(cancellationToken);
            if (latestWo != null)
            {
                activities.Add(new DashboardActivityDto { Title = "Work order created", Detail = $"{latestWo.WorkOrderNumber} for {latestWo.ProductName}", Time = "Recent" });
            }

            // 6. Trends & Charts
            var salesTrend = new List<DashboardChartBarDto>();
            var purchaseTrend = new List<DashboardChartBarDto>();

            for (int i = 5; i >= 0; i--)
            {
                var dt = now.AddMonths(-i);
                var monthName = dt.ToString("MMM", CultureInfo.InvariantCulture);
                var mStart = new DateTime(dt.Year, dt.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var mEnd = mStart.AddMonths(1);

                var sVal = await _db.SalesOrders.AsNoTracking()
                    .Where(x => x.CreatedDate >= mStart && x.CreatedDate < mEnd)
                    .SumAsync(x => x.GrandTotal, cancellationToken);

                var pVal = await _db.PurchaseOrders.AsNoTracking()
                    .Where(x => !x.IsDeleted && x.CreatedAt >= mStart && x.CreatedAt < mEnd)
                    .SumAsync(x => x.TotalAmount, cancellationToken);

                salesTrend.Add(new DashboardChartBarDto { Label = monthName, Value = Math.Round(sVal / 1000m, 1) });
                purchaseTrend.Add(new DashboardChartBarDto { Label = monthName, Value = Math.Round(pVal / 1000m, 1) });
            }

            var inventoryStatus = new List<DashboardChartBarDto>
            {
                new DashboardChartBarDto { Label = "Raw", Value = totalRawMaterials },
                new DashboardChartBarDto { Label = "WIP", Value = totalWorkOrders },
                new DashboardChartBarDto { Label = "FG", Value = totalFinishedGoods },
                new DashboardChartBarDto { Label = "Spare", Value = await _db.InventoryBatches.AsNoTracking().CountAsync(x => !x.IsDeleted, cancellationToken) }
            };

            var revenueOverview = new List<DashboardChartBarDto>();
            for (int w = 1; w <= 4; w++)
            {
                var wStart = monthStart.AddDays((w - 1) * 7);
                var wEnd = w == 4 ? monthStart.AddMonths(1) : monthStart.AddDays(w * 7);

                var wRev = await _db.SalesOrders.AsNoTracking()
                    .Where(x => x.CreatedDate >= wStart && x.CreatedDate < wEnd)
                    .SumAsync(x => x.GrandTotal, cancellationToken);

                revenueOverview.Add(new DashboardChartBarDto { Label = $"W{w}", Value = Math.Round(wRev / 1000m, 1) });
            }

            return new AdminDashboardSummaryDto
            {
                Kpis = kpis,
                QuickActions = quickActions,
                RecentSales = recentSales,
                RecentPurchase = recentPurchase,
                LowStock = lowStock,
                PendingProduction = pendingProduction,
                UpcomingDispatches = upcomingDispatches,
                OutstandingPayments = outstandingPayments,
                Activities = activities,
                SalesTrend = salesTrend,
                PurchaseTrend = purchaseTrend,
                InventoryStatus = inventoryStatus,
                RevenueOverview = revenueOverview,
                AchievedRevenue = monthlyRevenueAchieved,
                TargetRevenue = monthlyRevenueTarget,
                TargetPct = targetPct
            };
        }

        private static string FormatCurrency(decimal amount)
        {
            if (amount >= 100000m)
            {
                return $"₹{(amount / 100000m):F1}L";
            }
            return $"₹{amount:N0}";
        }
    }
}
