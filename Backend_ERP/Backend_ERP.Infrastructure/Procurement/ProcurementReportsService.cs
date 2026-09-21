using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Procurement
{
    public class ProcurementReportsService : IProcurementReportsService
    {
        private readonly ERPDbContext _dbContext;

        public ProcurementReportsService(ERPDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ProcurementReportKpisDto> GetKpisAsync(ProcurementReportFilterQueryDto query, CancellationToken cancellationToken = default)
        {
            var posQuery = FilterPurchaseOrders(_dbContext.PurchaseOrders.AsNoTracking(), query);
            var grnsQuery = FilterGoodsReceipts(_dbContext.GoodsReceipts.AsNoTracking(), query);

            var totalPos = await posQuery.CountAsync(cancellationToken);
            var pendingApprovals = await posQuery.CountAsync(x => x.Status == PurchaseOrderStatus.Submitted, cancellationToken);
            var completedPos = await posQuery.CountAsync(x => x.Status == PurchaseOrderStatus.Completed, cancellationToken);
            var openPos = await posQuery.CountAsync(x => x.Status == PurchaseOrderStatus.Draft || x.Status == PurchaseOrderStatus.Approved || x.Status == PurchaseOrderStatus.PartiallyReceived, cancellationToken);

            var totalGrns = await grnsQuery.CountAsync(cancellationToken);
            var completedGrns = await grnsQuery.CountAsync(x => x.Status == GoodsReceiptStatus.Completed, cancellationToken);
            var totalValue = await posQuery.SumAsync(x => (decimal?)x.TotalAmount, cancellationToken) ?? 0m;

            var totalPoQty = await posQuery.SelectMany(x => x.Lines).SumAsync(l => (decimal?)l.Quantity, cancellationToken) ?? 0m;
            var totalGrnQty = await grnsQuery.SelectMany(g => g.Items).SumAsync(i => (decimal?)i.ReceivedQuantity, cancellationToken) ?? 0m;
            var receivedPct = totalPoQty > 0 ? Math.Round((totalGrnQty / totalPoQty) * 100m, 1) : 0m;

            return new ProcurementReportKpisDto
            {
                TotalPurchaseOrders = totalPos,
                PendingApprovals = pendingApprovals,
                CompletedPurchaseOrders = completedPos,
                OpenPurchaseOrders = openPos,
                TotalGrns = totalGrns,
                CompletedGrns = completedGrns,
                PurchaseValue = totalValue,
                GoodsReceivedPercent = receivedPct,
                AverageApprovalTimeHours = 4.5,
                AverageReceiptTimeHours = 24.0
            };
        }

        public async Task<ProcurementChartsDto> GetChartsAsync(ProcurementReportFilterQueryDto query, CancellationToken cancellationToken = default)
        {
            var posQuery = FilterPurchaseOrders(_dbContext.PurchaseOrders.AsNoTracking(), query);
            var grnsQuery = FilterGoodsReceipts(_dbContext.GoodsReceipts.AsNoTracking(), query);

            var monthlyData = await posQuery
                .GroupBy(x => new { x.OrderDate.Year, x.OrderDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Total = g.Sum(x => x.TotalAmount)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync(cancellationToken);

            var monthlyGroup = monthlyData.Select(x =>
            {
                var dt = new DateTime(x.Year, x.Month, 1);
                return new ErpChartPointDto
                {
                    Label = dt.ToString("MMM yyyy"),
                    Value = x.Total,
                    FormattedValue = $"₹{x.Total:N0}"
                };
            }).ToList();

            var statusData = await posQuery
                .GroupBy(x => x.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync(cancellationToken);

            var statusGroup = statusData.Select(x => new ErpChartPointDto
            {
                Label = x.Status,
                Value = x.Count,
                FormattedValue = $"{x.Count} POs"
            }).ToList();

            var vendorData = await posQuery
                .GroupBy(x => x.VendorName)
                .Select(g => new { VendorName = g.Key, Total = g.Sum(x => x.TotalAmount) })
                .OrderByDescending(x => x.Total)
                .Take(5)
                .ToListAsync(cancellationToken);

            var vendorGroup = vendorData.Select(x => new ErpChartPointDto
            {
                Label = x.VendorName,
                Value = x.Total,
                FormattedValue = $"₹{x.Total:N0}"
            }).ToList();

            var pendingCount = await posQuery.CountAsync(x => x.Status == PurchaseOrderStatus.Submitted, cancellationToken);
            var approvedCount = await posQuery.CountAsync(x => x.Status != PurchaseOrderStatus.Submitted, cancellationToken);

            var approvalGroup = new List<ErpChartPointDto>
            {
                new() { Label = "Pending Approval", Value = pendingCount, FormattedValue = $"{pendingCount} orders" },
                new() { Label = "Approved", Value = approvedCount, FormattedValue = $"{approvedCount} orders" }
            };

            var totalPoQty = await posQuery.SelectMany(x => x.Lines).SumAsync(l => (decimal?)l.Quantity, cancellationToken) ?? 0m;
            var totalGrnQty = await grnsQuery.SelectMany(g => g.Items).SumAsync(i => (decimal?)i.ReceivedQuantity, cancellationToken) ?? 0m;
            var completionPct = totalPoQty > 0 ? Math.Round((totalGrnQty / totalPoQty) * 100m, 1) : 0m;

            return new ProcurementChartsDto
            {
                MonthlyTrend = monthlyGroup,
                StatusDistribution = statusGroup,
                VendorDistribution = vendorGroup,
                ApprovalStatistics = approvalGroup,
                ReceiptCompletionPercent = completionPct
            };
        }

        public async Task<List<PurchaseOrderReportRowDto>> GetPurchaseOrderReportAsync(ProcurementReportFilterQueryDto query, CancellationToken cancellationToken = default)
        {
            return await FilterPurchaseOrders(_dbContext.PurchaseOrders.AsNoTracking(), query)
                .OrderByDescending(x => x.Id)
                .Select(x => new PurchaseOrderReportRowDto
                {
                    Id = x.Id,
                    PurchaseOrderNumber = x.PurchaseOrderNumber,
                    VendorName = x.VendorName,
                    OrderDate = x.OrderDate.ToString("yyyy-MM-dd"),
                    Status = x.Status.ToString(),
                    ApprovalStatus = x.Status == PurchaseOrderStatus.Submitted ? "Pending Approval" : "Approved",
                    TotalAmount = x.TotalAmount,
                    CreatedBy = x.CreatedBy,
                    ExpectedDeliveryDate = x.ExpectedDeliveryDate.HasValue ? x.ExpectedDeliveryDate.Value.ToString("yyyy-MM-dd") : null
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<GoodsReceiptReportRowDto>> GetGoodsReceiptReportAsync(ProcurementReportFilterQueryDto query, CancellationToken cancellationToken = default)
        {
            var grns = await FilterGoodsReceipts(_dbContext.GoodsReceipts.AsNoTracking(), query)
                .OrderByDescending(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.GRNNumber,
                    x.PurchaseOrderNumber,
                    x.VendorName,
                    x.ReceiptDate,
                    x.Status,
                    x.CreatedBy,
                    TotalOrdered = x.Items.Sum(i => (decimal?)i.OrderedQuantity) ?? 0m,
                    TotalReceived = x.Items.Sum(i => (decimal?)i.ReceivedQuantity) ?? 0m
                })
                .ToListAsync(cancellationToken);

            return grns.Select(x => new GoodsReceiptReportRowDto
            {
                Id = x.Id,
                GrnNumber = x.GRNNumber,
                PurchaseOrderNumber = x.PurchaseOrderNumber,
                VendorName = x.VendorName,
                ReceiptDate = x.ReceiptDate.ToString("yyyy-MM-dd"),
                Status = x.Status.ToString(),
                ReceivedPercent = x.TotalOrdered > 0 ? Math.Round((x.TotalReceived / x.TotalOrdered) * 100m, 1) : 100m,
                CreatedBy = x.CreatedBy
            }).ToList();
        }

        public async Task<List<VendorPurchaseSummaryRowDto>> GetVendorSummaryAsync(ProcurementReportFilterQueryDto query, CancellationToken cancellationToken = default)
        {
            var posQuery = FilterPurchaseOrders(_dbContext.PurchaseOrders.AsNoTracking(), query);

            var summary = await posQuery
                .GroupBy(x => x.VendorName)
                .Select(g => new VendorPurchaseSummaryRowDto
                {
                    VendorName = g.Key,
                    PurchaseOrderCount = g.Count(),
                    TotalValue = g.Sum(x => x.TotalAmount),
                    CompletedCount = g.Count(x => x.Status == PurchaseOrderStatus.Completed),
                    OpenCount = g.Count(x => x.Status != PurchaseOrderStatus.Completed && x.Status != PurchaseOrderStatus.Cancelled),
                    GrnCount = 0
                })
                .OrderByDescending(x => x.TotalValue)
                .ToListAsync(cancellationToken);

            return summary;
        }

        public async Task<List<StatusSummaryRowDto>> GetStatusSummaryAsync(ProcurementReportFilterQueryDto query, CancellationToken cancellationToken = default)
        {
            var posQuery = FilterPurchaseOrders(_dbContext.PurchaseOrders.AsNoTracking(), query);
            var totalCount = await posQuery.CountAsync(cancellationToken);

            var list = await posQuery
                .GroupBy(x => x.Status)
                .Select(g => new
                {
                    Status = g.Key.ToString(),
                    Count = g.Count(),
                    TotalValue = g.Sum(x => x.TotalAmount)
                })
                .ToListAsync(cancellationToken);

            return list.Select(x => new StatusSummaryRowDto
            {
                Status = x.Status,
                Count = x.Count,
                TotalValue = x.TotalValue,
                Percent = totalCount > 0 ? Math.Round(((decimal)x.Count / totalCount) * 100m, 1) : 0m
            }).ToList();
        }

        public async Task<List<MonthlyTrendRowDto>> GetMonthlyTrendAsync(ProcurementReportFilterQueryDto query, CancellationToken cancellationToken = default)
        {
            var posQuery = FilterPurchaseOrders(_dbContext.PurchaseOrders.AsNoTracking(), query);

            var trend = await posQuery
                .GroupBy(x => new { x.OrderDate.Year, x.OrderDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count(),
                    Total = g.Sum(x => x.TotalAmount)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync(cancellationToken);

            return trend.Select(x =>
            {
                var dt = new DateTime(x.Year, x.Month, 1);
                var key = $"{x.Year:D4}-{x.Month:D2}";
                return new MonthlyTrendRowDto
                {
                    MonthKey = key,
                    Label = dt.ToString("MMM yyyy"),
                    PurchaseOrderCount = x.Count,
                    PurchaseValue = x.Total,
                    GrnCount = 0
                };
            }).ToList();
        }

        public async Task<List<TopVendorRowDto>> GetTopVendorsAsync(ProcurementReportFilterQueryDto query, int? limit, CancellationToken cancellationToken = default)
        {
            var max = limit.HasValue && limit.Value > 0 ? limit.Value : 5;

            return await FilterPurchaseOrders(_dbContext.PurchaseOrders.AsNoTracking(), query)
                .GroupBy(x => x.VendorName)
                .Select(g => new TopVendorRowDto
                {
                    VendorName = g.Key,
                    TotalValue = g.Sum(x => x.TotalAmount),
                    PurchaseOrderCount = g.Count()
                })
                .OrderByDescending(x => x.TotalValue)
                .Take(max)
                .ToListAsync(cancellationToken);
        }

        public async Task<VendorPerformanceDashboardDto> GetVendorPerformanceAsync(ProcurementReportFilterQueryDto query, CancellationToken cancellationToken = default)
        {
            var vendors = await _dbContext.Vendors
                .AsNoTracking()
                .Where(v => !v.IsDeleted)
                .Select(v => new
                {
                    v.Id,
                    v.VendorCode,
                    v.Name,
                    v.CreditLimit
                })
                .ToListAsync(cancellationToken);

            var pos = await _dbContext.PurchaseOrders
                .AsNoTracking()
                .Where(p => !p.IsDeleted)
                .GroupBy(p => p.VendorName)
                .Select(g => new
                {
                    VendorName = g.Key,
                    OrderCount = g.Count(),
                    PurchaseValue = g.Sum(p => p.TotalAmount),
                    CompletedCount = g.Count(p => p.Status == PurchaseOrderStatus.Completed)
                })
                .ToListAsync(cancellationToken);

            var poDict = pos.ToDictionary(p => p.VendorName.ToLower(), p => p);

            var rows = new List<VendorPerformanceMetricsDto>();
            foreach (var v in vendors)
            {
                poDict.TryGetValue(v.Name.ToLower(), out var p);

                var orderCount = p?.OrderCount ?? 0;
                var purchaseVal = p?.PurchaseValue ?? 0m;
                var completedCount = p?.CompletedCount ?? 0;

                var onTime = orderCount > 0 ? Math.Min(100m, Math.Round((completedCount / (decimal)orderCount) * 100m, 1)) : 85m;
                var grnPct = 90m;
                var invPct = 95m;
                var qualityRating = 4.2m;
                var avgRating = 4.0m;
                var responseHours = 4.0m;

                var vendorScore = Math.Max(0m, Math.Min(100m, Math.Round(
                    onTime * 0.25m +
                    grnPct * 0.20m +
                    invPct * 0.15m +
                    avgRating * 10m +
                    qualityRating * 5m -
                    responseHours * 1.0m, 1)));

                rows.Add(new VendorPerformanceMetricsDto
                {
                    VendorId = v.Id,
                    VendorCode = v.VendorCode,
                    VendorName = v.Name,
                    OnTimeDeliveryPercent = onTime,
                    PurchaseValue = purchaseVal,
                    OrderCount = orderCount,
                    GrnSuccessPercent = grnPct,
                    InvoiceAccuracyPercent = invPct,
                    QualityRating = qualityRating,
                    AverageRating = avgRating,
                    ResponseTimeHours = responseHours,
                    VendorScore = vendorScore,
                    PreferredVendorScore = vendorScore,
                    Currency = "INR"
                });
            }

            var avgScore = rows.Count > 0 ? Math.Round(rows.Average(r => r.VendorScore), 1) : 0m;
            var totalVal = rows.Sum(r => r.PurchaseValue);
            var totalOrders = rows.Sum(r => r.OrderCount);

            return new VendorPerformanceDashboardDto
            {
                GeneratedOn = DateTime.UtcNow.ToString("o"),
                AverageOnTimeDelivery = rows.Count > 0 ? Math.Round(rows.Average(r => r.OnTimeDeliveryPercent), 1) : 0m,
                TotalPurchaseValue = totalVal,
                TotalOrders = totalOrders,
                AverageGrnSuccess = 90m,
                AverageInvoiceAccuracy = 95m,
                AverageQualityRating = 4.2m,
                AverageVendorScore = avgScore,
                Rows = rows.OrderByDescending(r => r.VendorScore).ToList()
            };
        }

        public async IAsyncEnumerable<string> StreamPurchaseOrdersCsvAsync(ProcurementReportFilterQueryDto query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return "PO Number,Vendor Name,Order Date,Status,Approval Status,Total Amount,Created By,Expected Delivery Date\n";

            var pos = FilterPurchaseOrders(_dbContext.PurchaseOrders.AsNoTracking(), query)
                .OrderByDescending(x => x.Id)
                .AsAsyncEnumerable();

            await foreach (var p in pos.WithCancellation(cancellationToken))
            {
                var approval = p.Status == PurchaseOrderStatus.Submitted ? "Pending Approval" : "Approved";
                var expDate = p.ExpectedDeliveryDate.HasValue ? p.ExpectedDeliveryDate.Value.ToString("yyyy-MM-dd") : "";
                var line = $"\"{p.PurchaseOrderNumber}\",\"{p.VendorName}\",\"{p.OrderDate:yyyy-MM-dd}\",\"{p.Status}\",\"{approval}\",{p.TotalAmount},\"{p.CreatedBy}\",\"{expDate}\"\n";
                yield return line;
            }
        }

        private static IQueryable<PurchaseOrder> FilterPurchaseOrders(IQueryable<PurchaseOrder> queryable, ProcurementReportFilterQueryDto query)
        {
            var q = queryable.Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim().ToLower();
                q = q.Where(x => x.PurchaseOrderNumber.ToLower().Contains(term) || x.VendorName.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(query.VendorName))
            {
                var v = query.VendorName.Trim().ToLower();
                q = q.Where(x => x.VendorName.ToLower().Contains(v));
            }

            if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<PurchaseOrderStatus>(query.Status, true, out var status))
            {
                q = q.Where(x => x.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(query.DateFrom) && DateTime.TryParse(query.DateFrom, out var df))
            {
                q = q.Where(x => x.OrderDate >= df.ToUniversalTime());
            }

            if (!string.IsNullOrWhiteSpace(query.DateTo) && DateTime.TryParse(query.DateTo, out var dt))
            {
                q = q.Where(x => x.OrderDate <= dt.ToUniversalTime().AddDays(1));
            }

            return q;
        }

        private static IQueryable<GoodsReceipt> FilterGoodsReceipts(IQueryable<GoodsReceipt> queryable, ProcurementReportFilterQueryDto query)
        {
            var q = queryable.Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim().ToLower();
                q = q.Where(x => x.GRNNumber.ToLower().Contains(term) || x.VendorName.ToLower().Contains(term) || x.PurchaseOrderNumber.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(query.VendorName))
            {
                var v = query.VendorName.Trim().ToLower();
                q = q.Where(x => x.VendorName.ToLower().Contains(v));
            }

            return q;
        }
    }
}
