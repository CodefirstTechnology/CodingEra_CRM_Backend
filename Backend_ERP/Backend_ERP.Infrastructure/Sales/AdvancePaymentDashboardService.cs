using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ERP.Infrastructure.Sales
{
    /// <summary>
    /// Advance Payment Dashboard KPI service with Redis 7+ sliding-window caching
    /// (5-minute TTL). Falls back to L1 MemoryCache when Redis is unavailable.
    /// </summary>
    public class AdvancePaymentDashboardService : IAdvancePaymentDashboardService
    {
        private readonly ERPDbContext _db;
        private readonly IMemoryCache _memCache;
        private readonly ILogger<AdvancePaymentDashboardService> _logger;

        // Redis/IDistributedCache is optional – injected via constructor if registered
        private readonly IDistributedCache? _distributedCache;

        private const int CacheTtlMinutes = 5;

        public AdvancePaymentDashboardService(
            ERPDbContext db,
            IMemoryCache memCache,
            ILogger<AdvancePaymentDashboardService> logger,
            IDistributedCache? distributedCache = null)
        {
            _db = db;
            _memCache = memCache;
            _logger = logger;
            _distributedCache = distributedCache;
        }

        public async Task<AdvancePaymentDashboardKpiDto> GetDashboardKpisAsync(
            string tenantId = "default",
            CancellationToken cancellationToken = default)
        {
            var cacheKey = $"adv_dashboard_kpi_{tenantId}";

            // Try Redis first (sliding-window)
            if (_distributedCache != null)
            {
                try
                {
                    var cached = await _distributedCache.GetStringAsync(cacheKey, cancellationToken);
                    if (cached != null)
                    {
                        return JsonSerializer.Deserialize<AdvancePaymentDashboardKpiDto>(cached)
                               ?? await ComputeKpisAsync(tenantId, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Redis unavailable for advance payment dashboard KPI cache. Falling back to L1.");
                }
            }

            // Try L1 MemoryCache
            if (_memCache.TryGetValue(cacheKey, out AdvancePaymentDashboardKpiDto? cached1) && cached1 != null)
            {
                return cached1;
            }

            var dto = await ComputeKpisAsync(tenantId, cancellationToken);

            // Store in L1 with 5-min sliding expiry
            _memCache.Set(cacheKey, dto, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(CacheTtlMinutes)
            });

            // Store in Redis (distributed) with absolute + sliding options
            if (_distributedCache != null)
            {
                try
                {
                    var serialized = JsonSerializer.Serialize(dto);
                    await _distributedCache.SetStringAsync(
                        cacheKey,
                        serialized,
                        new DistributedCacheEntryOptions
                        {
                            SlidingExpiration = TimeSpan.FromMinutes(CacheTtlMinutes),
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheTtlMinutes * 4)
                        },
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to write advance payment dashboard KPI to Redis cache.");
                }
            }

            return dto;
        }

        // ─── Core KPI Computation (EF Core, no raw SQL) ──────────────────────────

        private async Task<AdvancePaymentDashboardKpiDto> ComputeKpisAsync(
            string tenantId,
            CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow;
            var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

            // All non-deleted advance payments for this tenant
            var baseQuery = _db.AdvancePayments
                .AsNoTracking()
                .Where(ap => !ap.IsDeleted && ap.TenantId == tenantId);

            var totalFloat = await baseQuery
                .Where(ap => ap.Status != AdvancePaymentStatuses.Cancelled
                          && ap.Status != AdvancePaymentStatuses.Dishonored
                          && ap.Status != AdvancePaymentStatuses.Rejected)
                .SumAsync(ap => ap.AdvanceAmount, ct);

            var unallocatedFloat = await baseQuery
                .Where(ap => ap.RemainingAmount > 0
                          && ap.Status != AdvancePaymentStatuses.Cancelled
                          && ap.Status != AdvancePaymentStatuses.Dishonored
                          && ap.Status != AdvancePaymentStatuses.Rejected)
                .SumAsync(ap => ap.RemainingAmount, ct);

            var allocatedFloat = await baseQuery
                .Where(ap => ap.Status != AdvancePaymentStatuses.Cancelled
                          && ap.Status != AdvancePaymentStatuses.Dishonored)
                .SumAsync(ap => ap.AppliedAmount, ct);

            var clearedThisMonth = await baseQuery
                .Where(ap => ap.Status == AdvancePaymentStatuses.Cleared
                          && ap.BankReconciledOn >= monthStart)
                .SumAsync(ap => ap.AdvanceAmount, ct);

            var pendingClearanceCount = await baseQuery
                .CountAsync(ap => ap.Status == AdvancePaymentStatuses.Received
                               || ap.Status == AdvancePaymentStatuses.PartiallyApplied, ct);

            var dishonoredCount = await baseQuery
                .CountAsync(ap => ap.Status == AdvancePaymentStatuses.Dishonored, ct);

            // Production-released and Dispatch-released SO counts driven by advance clearances
            var productionReleasedSoCount = await _db.SalesOrders
                .AsNoTracking()
                .CountAsync(so => so.IsProductionReleased, ct);

            var dispatchReleasedSoCount = await _db.SalesOrders
                .AsNoTracking()
                .CountAsync(so => so.IsDispatchReleased, ct);

            // Recent activity: last 10 advance payment events
            var recentActivity = await baseQuery
                .OrderByDescending(ap => ap.UpdatedDate)
                .Take(10)
                .Select(ap => new AdvancePaymentListItemDto
                {
                    Id = ap.Id,
                    PaymentNumber = ap.PaymentNumber,
                    CustomerName = ap.CustomerName,
                    PaymentDate = ap.PaymentDate.ToString("yyyy-MM-dd"),
                    PaymentMode = ap.PaymentMode,
                    Currency = ap.Currency,
                    AdvanceAmount = ap.AdvanceAmount,
                    RemainingAmount = ap.RemainingAmount,
                    Status = ap.Status,
                    ReconciliationStatus = ap.ReconciliationStatus
                })
                .ToListAsync(ct);

            return new AdvancePaymentDashboardKpiDto
            {
                TotalFloat = totalFloat,
                UnallocatedFloat = unallocatedFloat,
                AllocatedFloat = allocatedFloat,
                ClearedThisMonth = clearedThisMonth,
                PendingClearanceCount = pendingClearanceCount,
                ProductionReleasedSoCount = productionReleasedSoCount,
                DispatchReleasedSoCount = dispatchReleasedSoCount,
                DishonoredCount = dishonoredCount,
                RecentActivity = recentActivity
            };
        }
    }
}

