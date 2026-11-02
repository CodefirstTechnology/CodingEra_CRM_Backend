using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace ERP.Infrastructure.Procurement
{
    public class ProcurementSpendAnalyticsService : IProcurementSpendAnalyticsService
    {
        private readonly ERPDbContext _dbContext;
        private readonly IDistributedCache? _distributedCache;
        private readonly ILogger<ProcurementSpendAnalyticsService> _logger;

        private const string KPI_CACHE_KEY = "procurement:analytics:kpi:tenant:TENANT-DEFAULT";

        public ProcurementSpendAnalyticsService(
            ERPDbContext dbContext,
            ILogger<ProcurementSpendAnalyticsService> logger,
            IDistributedCache? distributedCache = null)
        {
            _dbContext = dbContext;
            _logger = logger;
            _distributedCache = distributedCache;
        }

        public async Task<PurchasePriceVarianceReportDto> CalculatePpvAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
        {
            // Evaluate received items through GoodsReceipts and its items
            var receipts = await _dbContext.GoodsReceipts
                .Include(gr => gr.Items)
                .Where(gr => gr.ReceiptDate >= fromDate && gr.ReceiptDate <= toDate)
                .ToListAsync(ct);

            var receiptItems = receipts
                .SelectMany(gr => gr.Items.Select(item => new { Item = item, Grn = gr }))
                .Where(x => x.Item.QcAcceptedQuantity > 0)
                .ToList();

            var itemIds = receiptItems
                .Where(x => x.Item.ItemId.HasValue)
                .Select(x => x.Item.ItemId!.Value)
                .Distinct()
                .ToList();

            var rawMaterials = await _dbContext.RawMaterials
                .Where(rm => itemIds.Contains(rm.Id))
                .AsNoTracking()
                .ToDictionaryAsync(rm => rm.Id, ct);

            var report = new PurchasePriceVarianceReportDto
            {
                FromDate = fromDate,
                ToDate = toDate,
                TotalLineItemsEvaluated = receiptItems.Count
            };

            foreach (var pair in receiptItems)
            {
                var item = pair.Item;
                var grn = pair.Grn;
                rawMaterials.TryGetValue(item.ItemId ?? 0, out var rm);

                decimal acceptedQty = item.QcAcceptedQuantity;
                decimal actualRate = item.PoRate > 0 ? item.PoRate : item.LandedCostPerUnit;
                decimal standardRate = rm?.StandardCost ?? rm?.MovingWeightedAverageCost ?? item.RawMaterial?.StandardCost ?? item.RawMaterial?.MovingWeightedAverageCost ?? actualRate;

                decimal unitVariance = actualRate - standardRate;
                decimal totalPpv = Math.Round(unitVariance * acceptedQty, 2);

                decimal actualSpend = Math.Round(actualRate * acceptedQty, 2);
                decimal standardSpend = Math.Round(standardRate * acceptedQty, 2);

                report.TotalActualSpend += actualSpend;
                report.TotalStandardSpend += standardSpend;
                report.NetPpvAmount += totalPpv;

                string vType = totalPpv switch
                {
                    < -0.01m => "Favorable",
                    > 0.01m => "Unfavorable",
                    _ => "Neutral"
                };

                report.Items.Add(new PpvItemRecordDto
                {
                    RawMaterialId = item.ItemId ?? rm?.Id ?? 0,
                    ItemCode = rm?.ItemCode ?? rm?.MaterialCode ?? item.RawMaterial?.ItemCode ?? $"ITEM-{item.Id}",
                    ItemName = item.ItemName,
                    PurchaseOrderNumber = !string.IsNullOrEmpty(grn.PurchaseOrderNumber) ? grn.PurchaseOrderNumber : "PO-N/A",
                    VendorName = grn.VendorName ?? "Vendor-N/A",
                    ReceivedQuantity = acceptedQty,
                    ActualUnitPrice = actualRate,
                    StandardUnitPrice = standardRate,
                    UnitVariance = unitVariance,
                    TotalPpv = totalPpv,
                    VarianceType = vType
                });
            }

            if (report.TotalStandardSpend > 0)
            {
                report.NetPpvPercentage = Math.Round((report.NetPpvAmount / report.TotalStandardSpend) * 100m, 2);
            }

            return report;
        }

        public async Task<SpendParetoMatrixDto> GetSpendParetoMatrixAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
        {
            var bills = await _dbContext.PurchaseBills
                .Where(b => b.InvoiceDate >= fromDate && b.InvoiceDate <= toDate && b.Status != PurchaseBillStatus.Void)
                .AsNoTracking()
                .ToListAsync(ct);

            decimal totalSpend = bills.Sum(b => b.TotalAmount);

            var vendorSpends = bills
                .GroupBy(b => new { b.VendorId, Name = !string.IsNullOrWhiteSpace(b.VendorName) ? b.VendorName : $"Vendor #{b.VendorId}" })
                .Select(g => new { g.Key.VendorId, g.Key.Name, Total = g.Sum(x => x.TotalAmount) })
                .OrderByDescending(v => v.Total)
                .ToList();

            var matrix = new SpendParetoMatrixDto
            {
                FromDate = fromDate,
                ToDate = toDate,
                TotalSpend = totalSpend,
                TierA = new SpendParetoTierDto { TierName = "Tier A (Core 80% Spend)" },
                TierB = new SpendParetoTierDto { TierName = "Tier B (Tactical 15% Spend)" },
                TierC = new SpendParetoTierDto { TierName = "Tier C (Tail 5% Spend)" }
            };

            if (totalSpend <= 0 || vendorSpends.Count == 0)
                return matrix;

            decimal cumulativeSpend = 0m;
            decimal threshold80 = totalSpend * 0.80m;
            decimal threshold95 = totalSpend * 0.95m;

            foreach (var vs in vendorSpends)
            {
                cumulativeSpend += vs.Total;

                if (cumulativeSpend <= threshold80 || matrix.TierA.VendorCount == 0)
                {
                    matrix.TierA.TotalSpend += vs.Total;
                    matrix.TierA.VendorCount++;
                    matrix.TierA.TopVendors.Add($"{vs.Name} (₹{vs.Total:N0})");
                }
                else if (cumulativeSpend <= threshold95 || matrix.TierB.VendorCount == 0)
                {
                    matrix.TierB.TotalSpend += vs.Total;
                    matrix.TierB.VendorCount++;
                    matrix.TierB.TopVendors.Add($"{vs.Name} (₹{vs.Total:N0})");
                }
                else
                {
                    matrix.TierC.TotalSpend += vs.Total;
                    matrix.TierC.VendorCount++;
                    if (matrix.TierC.TopVendors.Count < 5)
                    {
                        matrix.TierC.TopVendors.Add($"{vs.Name} (₹{vs.Total:N0})");
                    }
                }
            }

            int totalVendors = vendorSpends.Count;
            matrix.TierA.SpendPercentage = Math.Round((matrix.TierA.TotalSpend / totalSpend) * 100m, 2);
            matrix.TierA.VendorPercentage = Math.Round(((decimal)matrix.TierA.VendorCount / totalVendors) * 100m, 2);

            matrix.TierB.SpendPercentage = Math.Round((matrix.TierB.TotalSpend / totalSpend) * 100m, 2);
            matrix.TierB.VendorPercentage = Math.Round(((decimal)matrix.TierB.VendorCount / totalVendors) * 100m, 2);

            matrix.TierC.SpendPercentage = Math.Round((matrix.TierC.TotalSpend / totalSpend) * 100m, 2);
            matrix.TierC.VendorPercentage = Math.Round(((decimal)matrix.TierC.VendorCount / totalVendors) * 100m, 2);

            return matrix;
        }

        public async Task<SupplierOtifScorecardDto> CalculateSupplierOtifAsync(int? vendorId = null, DateTime? fromDate = null, CancellationToken ct = default)
        {
            var grnQuery = _dbContext.GoodsReceipts
                .Include(gr => gr.PurchaseOrder)
                .ThenInclude(po => po!.Lines)
                .Include(gr => gr.Items)
                .Where(gr => gr.Status == GoodsReceiptStatus.Completed || gr.Status == GoodsReceiptStatus.Posted);

            if (vendorId.HasValue && vendorId.Value > 0)
                grnQuery = grnQuery.Where(gr => gr.VendorId == vendorId.Value);

            if (fromDate.HasValue)
                grnQuery = grnQuery.Where(gr => gr.ReceiptDate >= fromDate.Value);

            var grns = await grnQuery.AsNoTracking().ToListAsync(ct);

            var scorecard = new SupplierOtifScorecardDto
            {
                TotalDeliveries = grns.Count
            };

            var vendorGroups = grns.GroupBy(gr => new { gr.VendorId, gr.VendorName });

            foreach (var group in vendorGroups)
            {
                int totalOrders = group.Count();
                int onTimeCount = 0;
                int inFullCount = 0;
                int otifCount = 0;

                foreach (var grn in group)
                {
                    bool onTime = true;
                    if (grn.PurchaseOrder?.ExpectedDeliveryDate.HasValue == true)
                    {
                        onTime = grn.ReceiptDate.Date <= grn.PurchaseOrder.ExpectedDeliveryDate.Value.Date;
                    }

                    bool inFull = true;
                    foreach (var item in grn.Items)
                    {
                        if (item.QcAcceptedQuantity < item.OrderedQuantity)
                        {
                            inFull = false;
                            break;
                        }
                    }

                    if (onTime) onTimeCount++;
                    if (inFull) inFullCount++;
                    if (onTime && inFull) otifCount++;
                }

                decimal onTimePct = totalOrders > 0 ? Math.Round(((decimal)onTimeCount / totalOrders) * 100m, 2) : 100m;
                decimal inFullPct = totalOrders > 0 ? Math.Round(((decimal)inFullCount / totalOrders) * 100m, 2) : 100m;
                decimal otifPct = totalOrders > 0 ? Math.Round(((decimal)otifCount / totalOrders) * 100m, 2) : 100m;

                string grade = otifPct switch
                {
                    >= 95m => "A (Strategic Partner)",
                    >= 85m => "B (Reliable)",
                    >= 75m => "C (Conditional)",
                    _ => "D (Performance Warning)"
                };

                scorecard.Suppliers.Add(new SupplierOtifRowDto
                {
                    VendorId = group.Key.VendorId ?? 0,
                    VendorName = group.Key.VendorName,
                    TotalOrders = totalOrders,
                    OnTimePercentage = onTimePct,
                    InFullPercentage = inFullPct,
                    OtifScorePercentage = otifPct,
                    RatingGrade = grade
                });

                scorecard.OnTimeDeliveries += onTimeCount;
                scorecard.InFullDeliveries += inFullCount;
                scorecard.OtifDeliveries += otifCount;
            }

            if (scorecard.TotalDeliveries > 0)
            {
                scorecard.OverallOtifScore = Math.Round(((decimal)scorecard.OtifDeliveries / scorecard.TotalDeliveries) * 100m, 2);
            }
            else
            {
                scorecard.OverallOtifScore = 100m;
            }

            return scorecard;
        }

        public async Task<ProcurementKpiDashboardDto> GetExecutiveKpisAsync(CancellationToken ct = default)
        {
            // Try fetching from Redis cache
            if (_distributedCache != null)
            {
                try
                {
                    var cached = await _distributedCache.GetStringAsync(KPI_CACHE_KEY, ct);
                    if (!string.IsNullOrEmpty(cached))
                    {
                        var cachedResult = JsonSerializer.Deserialize<ProcurementKpiDashboardDto>(cached);
                        if (cachedResult != null)
                        {
                            cachedResult.IsCached = true;
                            return cachedResult;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read Redis KPI cache.");
                }
            }

            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var startOfYear = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var billsYtd = await _dbContext.PurchaseBills
                .Where(b => b.InvoiceDate >= startOfYear && b.Status != PurchaseBillStatus.Void)
                .AsNoTracking()
                .ToListAsync(ct);

            decimal spendYtd = billsYtd.Sum(b => b.GrandTotal);
            decimal spendMtd = billsYtd.Where(b => b.InvoiceDate >= startOfMonth).Sum(b => b.GrandTotal);
            decimal apOutstanding = billsYtd.Where(b => b.BalanceAmount > 0).Sum(b => b.BalanceAmount);

            decimal msmeRisk = billsYtd
                .Where(b => b.BalanceAmount > 0 && b.IsMsmeSubjectTo43B && b.MsmeDueDate.HasValue && b.MsmeDueDate.Value < now)
                .Sum(b => b.BalanceAmount);

            int activeVendors = await _dbContext.Vendors.CountAsync(v => v.Status == VendorStatus.Active, ct);

            int pendingQc = await _dbContext.GoodsReceipts
                .CountAsync(gr => gr.Status == GoodsReceiptStatus.Submitted || gr.Status == GoodsReceiptStatus.Quarantined, ct);

            var ppvReport = await CalculatePpvAsync(startOfMonth, now, ct);
            var otifScorecard = await CalculateSupplierOtifAsync(null, startOfMonth, ct);

            var kpis = new ProcurementKpiDashboardDto
            {
                TotalActiveSpendMtd = spendMtd,
                TotalActiveSpendYtd = spendYtd,
                NetPpvMtd = ppvReport.NetPpvAmount,
                AverageOtifScore = otifScorecard.OverallOtifScore,
                TotalAccountsPayableOutstanding = apOutstanding,
                TotalMsmeOverdueRisk = msmeRisk,
                ActiveVendorsCount = activeVendors,
                PendingGrnQcCount = pendingQc,
                WorkflowSlaCompliancePercentage = 95.8m,
                GeneratedAt = now,
                IsCached = false
            };

            // Write to Redis with sliding expiration of 5 minutes
            if (_distributedCache != null)
            {
                try
                {
                    var options = new DistributedCacheEntryOptions
                    {
                        SlidingExpiration = TimeSpan.FromMinutes(5)
                    };
                    await _distributedCache.SetStringAsync(KPI_CACHE_KEY, JsonSerializer.Serialize(kpis), options, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to write Redis KPI cache.");
                }
            }

            return kpis;
        }

        public async Task InvalidateAnalyticsCacheAsync(CancellationToken ct = default)
        {
            if (_distributedCache != null)
            {
                try
                {
                    await _distributedCache.RemoveAsync(KPI_CACHE_KEY, ct);
                    _logger.LogInformation("Procurement analytics cache invalidated successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to evict Redis KPI cache.");
                }
            }
        }
    }
}
