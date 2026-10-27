using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace ERP.Infrastructure.Sales
{
    public class ProformaDashboardService : IProformaDashboardService
    {
        private readonly ERPDbContext _db;
        private readonly IDistributedCache? _cache;
        private readonly ILogger<ProformaDashboardService> _logger;
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

        public ProformaDashboardService(
            ERPDbContext db,
            ILogger<ProformaDashboardService> logger,
            IDistributedCache? cache = null)
        {
            _db = db;
            _logger = logger;
            _cache = cache;
        }

        public async Task<ProformaDashboardTelemetryDto> GetDashboardTelemetryAsync(
            string tenantId = "default",
            bool forceRefresh = false,
            CancellationToken ct = default)
        {
            var cacheKey = $"erp:{tenantId}:proforma:dashboard";

            if (!forceRefresh && _cache != null)
            {
                try
                {
                    var cached = await _cache.GetStringAsync(cacheKey, ct);
                    if (!string.IsNullOrWhiteSpace(cached))
                    {
                        var deserialized = JsonSerializer.Deserialize<ProformaDashboardTelemetryDto>(cached);
                        if (deserialized != null)
                        {
                            return deserialized;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read proforma dashboard telemetry from Redis cache. Falling back to DB.");
                }
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var query = _db.ProformaInvoices
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.TenantId == tenantId);

            var activeList = await query
                .Select(x => new
                {
                    x.Id,
                    x.PiNumber,
                    x.CustomerName,
                    x.QuotationNumber,
                    x.SalesOrderNumber,
                    x.InvoiceDate,
                    x.ValidUntil,
                    x.SalesPerson,
                    x.Currency,
                    x.GrandTotal,
                    x.AdvanceReceivedAmount,
                    x.PaymentStatus,
                    x.BillingType,
                    x.MilestonePercentage,
                    x.RequiresFinanceCreditReview,
                    x.IsProductionReleased,
                    x.IsDispatchReleased,
                    x.Status,
                    x.PlaceOfSupplyStateCode,
                    DaysPending = today.DayNumber - x.InvoiceDate.DayNumber
                })
                .ToListAsync(ct);

            var totalIssued = activeList.Where(x => x.Status != ProformaInvoiceStatuses.Cancelled && x.Status != ProformaInvoiceStatuses.Rejected).ToList();
            var totalIssuedValue = totalIssued.Sum(x => x.GrandTotal);
            var totalIssuedCount = totalIssued.Count;

            var pendingAdvanceList = totalIssued.Where(x => x.PaymentStatus != "FullyPaid").ToList();
            var pendingAdvanceValue = pendingAdvanceList.Sum(x => Math.Max(0m, x.GrandTotal - x.AdvanceReceivedAmount));
            var pendingAdvanceCount = pendingAdvanceList.Count;

            var convertedCount = activeList.Count(x => x.Status == ProformaInvoiceStatuses.Converted || x.Status == ProformaInvoiceStatuses.PartiallyConverted);
            var conversionRate = totalIssuedCount > 0 ? Math.Round(((decimal)convertedCount / totalIssuedCount) * 100m, 1) : 0m;

            var avgAging = pendingAdvanceList.Count > 0
                ? Math.Round((decimal)pendingAdvanceList.Average(x => Math.Max(0, x.DaysPending)), 1)
                : 0m;

            // Aging Buckets
            var aging = new ProformaAgingBucketsDto
            {
                Count0To7 = pendingAdvanceList.Count(x => x.DaysPending <= 7),
                Days0To7 = pendingAdvanceList.Where(x => x.DaysPending <= 7).Sum(x => Math.Max(0m, x.GrandTotal - x.AdvanceReceivedAmount)),

                Count8To15 = pendingAdvanceList.Count(x => x.DaysPending is >= 8 and <= 15),
                Days8To15 = pendingAdvanceList.Where(x => x.DaysPending is >= 8 and <= 15).Sum(x => Math.Max(0m, x.GrandTotal - x.AdvanceReceivedAmount)),

                Count16To30 = pendingAdvanceList.Count(x => x.DaysPending is >= 16 and <= 30),
                Days16To30 = pendingAdvanceList.Where(x => x.DaysPending is >= 16 and <= 30).Sum(x => Math.Max(0m, x.GrandTotal - x.AdvanceReceivedAmount)),

                CountOver30 = pendingAdvanceList.Count(x => x.DaysPending > 30),
                DaysOver30 = pendingAdvanceList.Where(x => x.DaysPending > 30).Sum(x => Math.Max(0m, x.GrandTotal - x.AdvanceReceivedAmount))
            };

            // Status Distribution
            var statusDistribution = activeList
                .GroupBy(x => x.Status)
                .Select(g => new ProformaStatusDistributionDto
                {
                    Status = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(x => x.GrandTotal)
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            // Recent 10
            var recent = activeList
                .OrderByDescending(x => x.InvoiceDate)
                .ThenByDescending(x => x.Id)
                .Take(10)
                .Select(x => new ProformaInvoiceListItemDto
                {
                    Id = x.Id,
                    PiNumber = x.PiNumber,
                    CustomerName = x.CustomerName,
                    QuotationNumber = x.QuotationNumber,
                    SalesOrderNumber = x.SalesOrderNumber,
                    InvoiceDate = x.InvoiceDate.ToString("yyyy-MM-dd"),
                    ValidUntil = x.ValidUntil.ToString("yyyy-MM-dd"),
                    SalesPerson = x.SalesPerson,
                    Currency = x.Currency,
                    GrandTotal = x.GrandTotal,
                    AdvanceReceivedAmount = x.AdvanceReceivedAmount,
                    PaymentStatus = x.PaymentStatus,
                    BillingType = x.BillingType,
                    MilestonePercentage = x.MilestonePercentage,
                    RequiresFinanceCreditReview = x.RequiresFinanceCreditReview,
                    IsProductionReleased = x.IsProductionReleased,
                    IsDispatchReleased = x.IsDispatchReleased,
                    Status = x.Status,
                    PlaceOfSupplyStateCode = x.PlaceOfSupplyStateCode
                })
                .ToList();

            var result = new ProformaDashboardTelemetryDto
            {
                TotalIssuedValue = totalIssuedValue,
                TotalIssuedCount = totalIssuedCount,
                PendingAdvanceValue = pendingAdvanceValue,
                PendingAdvanceCount = pendingAdvanceCount,
                ConversionRatePercentage = conversionRate,
                AverageAgingDays = avgAging,
                Aging = aging,
                StatusDistribution = statusDistribution,
                RecentInvoices = recent
            };

            if (_cache != null)
            {
                try
                {
                    var serialized = JsonSerializer.Serialize(result);
                    await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = CacheTtl
                    }, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cache proforma dashboard telemetry to Redis.");
                }
            }

            return result;
        }

        public async Task InvalidateDashboardCacheAsync(string tenantId = "default", CancellationToken ct = default)
        {
            if (_cache == null) return;
            try
            {
                var cacheKey = $"erp:{tenantId}:proforma:dashboard";
                await _cache.RemoveAsync(cacheKey, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate proforma dashboard cache for tenant {TenantId}.", tenantId);
            }
        }
    }
}
