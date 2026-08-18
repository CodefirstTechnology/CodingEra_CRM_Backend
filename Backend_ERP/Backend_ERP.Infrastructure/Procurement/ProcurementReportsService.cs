using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
            var posQuery = FilterPurchaseOrders(_dbContext.PurchaseOrders.Include(x => x.Lines).AsNoTracking(), query);
            var grnsQuery = FilterGoodsReceipts(_dbContext.GoodsReceipts.Include(x => x.Items).AsNoTracking(), query);

            var pos = await posQuery.ToListAsync(cancellationToken);
            var grns = await grnsQuery.ToListAsync(cancellationToken);

            var totalPos = pos.Count;
            var pendingApprovals = pos.Count(x => x.Status == PurchaseOrderStatus.Submitted);
            var completedPos = pos.Count(x => x.Status == PurchaseOrderStatus.Completed);
            var openPos = pos.Count(x => x.Status is PurchaseOrderStatus.Draft or PurchaseOrderStatus.Approved or PurchaseOrderStatus.PartiallyReceived);
            var totalGrns = grns.Count;
            var completedGrns = grns.Count(x => x.Status == GoodsReceiptStatus.Completed);
            var totalValue = pos.Sum(x => x.TotalAmount);

            var totalPoQty = pos.SelectMany(x => x.Lines).Sum(l => l.Quantity);
            var totalGrnQty = grns.SelectMany(g => g.Items).Sum(i => i.ReceivedQuantity);
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
            var pos = await FilterPurchaseOrders(_dbContext.PurchaseOrders.Include(x => x.Lines).AsNoTracking(), query).ToListAsync(cancellationToken);
            var grns = await FilterGoodsReceipts(_dbContext.GoodsReceipts.Include(x => x.Items).AsNoTracking(), query).ToListAsync(cancellationToken);

            // Monthly Trend
            var monthlyGroup = pos.GroupBy(x => x.OrderDate.ToString("yyyy-MM"))
                .OrderBy(g => g.Key)
                .Select(g => new ErpChartPointDto
                {
                    Label = DateTime.TryParseExact(g.Key, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
                        ? dt.ToString("MMM yyyy")
                        : g.Key,
                    Value = g.Sum(x => x.TotalAmount),
                    FormattedValue = $"₹{g.Sum(x => x.TotalAmount):N0}"
                }).ToList();

            // Status Distribution
            var statusGroup = pos.GroupBy(x => x.Status.ToString())
                .Select(g => new ErpChartPointDto
                {
                    Label = g.Key,
                    Value = g.Count(),
                    FormattedValue = $"{g.Count()} POs"
                }).ToList();

            // Vendor Distribution
            var vendorGroup = pos.GroupBy(x => x.VendorName)
                .Select(g => new ErpChartPointDto
                {
                    Label = g.Key,
                    Value = g.Sum(x => x.TotalAmount),
                    FormattedValue = $"₹{g.Sum(x => x.TotalAmount):N0}"
                }).OrderByDescending(x => x.Value).Take(5).ToList();

            // Approval Statistics
            var approvalGroup = pos.GroupBy(x => x.Status == PurchaseOrderStatus.Submitted ? "Pending Approval" : "Approved")
                .Select(g => new ErpChartPointDto
                {
                    Label = g.Key,
                    Value = g.Count(),
                    FormattedValue = $"{g.Count()} orders"
                }).ToList();

            var totalPoQty = pos.SelectMany(x => x.Lines).Sum(l => l.Quantity);
            var totalGrnQty = grns.SelectMany(g => g.Items).Sum(i => i.ReceivedQuantity);
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
            var list = await FilterPurchaseOrders(_dbContext.PurchaseOrders.AsNoTracking(), query)
                .OrderByDescending(x => x.Id)
                .ToListAsync(cancellationToken);

            return list.Select(x => new PurchaseOrderReportRowDto
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
            }).ToList();
        }

        public async Task<List<GoodsReceiptReportRowDto>> GetGoodsReceiptReportAsync(ProcurementReportFilterQueryDto query, CancellationToken cancellationToken = default)
        {
            var list = await FilterGoodsReceipts(_dbContext.GoodsReceipts.Include(x => x.Items).AsNoTracking(), query)
                .OrderByDescending(x => x.Id)
                .ToListAsync(cancellationToken);

            return list.Select(x =>
            {
                var totalOrdered = x.Items.Sum(i => i.OrderedQuantity);
                var totalRecv = x.Items.Sum(i => i.ReceivedQuantity);
                var pct = totalOrdered > 0 ? Math.Round((totalRecv / totalOrdered) * 100m, 1) : 100m;

                return new GoodsReceiptReportRowDto
                {
                    Id = x.Id,
                    GrnNumber = x.GRNNumber,
                    PurchaseOrderNumber = x.PurchaseOrderNumber,
                    VendorName = x.VendorName,
                    ReceiptDate = x.ReceiptDate.ToString("yyyy-MM-dd"),
                    Status = x.Status.ToString(),
                    ReceivedPercent = pct,
                    CreatedBy = x.CreatedBy
                };
            }).ToList();
        }

        public async Task<List<VendorPurchaseSummaryRowDto>> GetVendorSummaryAsync(ProcurementReportFilterQueryDto query, CancellationToken cancellationToken = default)
        {
            var pos = await FilterPurchaseOrders(_dbContext.PurchaseOrders.AsNoTracking(), query).ToListAsync(cancellationToken);
            var grns = await FilterGoodsReceipts(_dbContext.GoodsReceipts.AsNoTracking(), query).ToListAsync(cancellationToken);

            var vendorNames = pos.Select(x => x.VendorName).Concat(grns.Select(g => g.VendorName)).Distinct();

            return vendorNames.Select(name =>
            {
                var vPos = pos.Where(p => p.VendorName == name).ToList();
                var vGrns = grns.Where(g => g.VendorName == name).ToList();

                return new VendorPurchaseSummaryRowDto
                {
                    VendorName = name,
                    PurchaseOrderCount = vPos.Count,
                    TotalValue = vPos.Sum(p => p.TotalAmount),
                    CompletedCount = vPos.Count(p => p.Status == PurchaseOrderStatus.Completed),
                    OpenCount = vPos.Count(p => p.Status != PurchaseOrderStatus.Completed && p.Status != PurchaseOrderStatus.Cancelled),
                    GrnCount = vGrns.Count
                };
            }).OrderByDescending(x => x.TotalValue).ToList();
        }

        public async Task<List<StatusSummaryRowDto>> GetStatusSummaryAsync(ProcurementReportFilterQueryDto query, CancellationToken cancellationToken = default)
        {
            var pos = await FilterPurchaseOrders(_dbContext.PurchaseOrders.AsNoTracking(), query).ToListAsync(cancellationToken);
            var totalCount = pos.Count;

            return pos.GroupBy(x => x.Status.ToString())
                .Select(g =>
                {
                    var count = g.Count();
                    var val = g.Sum(x => x.TotalAmount);
                    var pct = totalCount > 0 ? Math.Round(((decimal)count / totalCount) * 100m, 1) : 0m;
                    return new StatusSummaryRowDto
                    {
                        Status = g.Key,
                        Count = count,
                        TotalValue = val,
                        Percent = pct
                    };
                }).ToList();
        }

        public async Task<List<MonthlyTrendRowDto>> GetMonthlyTrendAsync(ProcurementReportFilterQueryDto query, CancellationToken cancellationToken = default)
        {
            var pos = await FilterPurchaseOrders(_dbContext.PurchaseOrders.AsNoTracking(), query).ToListAsync(cancellationToken);
            var grns = await FilterGoodsReceipts(_dbContext.GoodsReceipts.AsNoTracking(), query).ToListAsync(cancellationToken);

            var months = pos.Select(x => x.OrderDate.ToString("yyyy-MM"))
                .Concat(grns.Select(g => g.ReceiptDate.ToString("yyyy-MM")))
                .Distinct()
                .OrderBy(m => m);

            return months.Select(m =>
            {
                var mPos = pos.Where(p => p.OrderDate.ToString("yyyy-MM") == m).ToList();
                var mGrns = grns.Where(g => g.ReceiptDate.ToString("yyyy-MM") == m).ToList();
                var dt = DateTime.TryParseExact(m, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : DateTime.UtcNow;

                return new MonthlyTrendRowDto
                {
                    MonthKey = m,
                    Label = dt.ToString("MMM yyyy"),
                    PurchaseOrderCount = mPos.Count,
                    PurchaseValue = mPos.Sum(p => p.TotalAmount),
                    GrnCount = mGrns.Count
                };
            }).ToList();
        }

        public async Task<List<TopVendorRowDto>> GetTopVendorsAsync(ProcurementReportFilterQueryDto query, int? limit, CancellationToken cancellationToken = default)
        {
            var pos = await FilterPurchaseOrders(_dbContext.PurchaseOrders.AsNoTracking(), query).ToListAsync(cancellationToken);

            var top = pos.GroupBy(x => x.VendorName)
                .Select(g => new TopVendorRowDto
                {
                    VendorName = g.Key,
                    TotalValue = g.Sum(x => x.TotalAmount),
                    PurchaseOrderCount = g.Count()
                })
                .OrderByDescending(x => x.TotalValue);

            var max = limit.HasValue && limit.Value > 0 ? limit.Value : 5;
            return top.Take(max).ToList();
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
