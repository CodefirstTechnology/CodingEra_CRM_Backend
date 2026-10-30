using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Sales.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Infrastructure.Sales
{
    public class SalesTargetDashboardService : ISalesTargetDashboardService
    {
        private readonly ERPDbContext _context;
        private readonly ITargetRedisCacheManager _cacheManager;
        private readonly ILogger<SalesTargetDashboardService> _logger;

        public SalesTargetDashboardService(
            ERPDbContext context,
            ITargetRedisCacheManager cacheManager,
            ILogger<SalesTargetDashboardService> logger)
        {
            _context = context;
            _cacheManager = cacheManager;
            _logger = logger;
        }

        public async Task<SalesTargetDashboardDto> GetDashboardKPIsAsync(
            int fiscalYear,
            string? period,
            string? branch,
            int? repUserId,
            CancellationToken ct = default)
        {
            string cacheKey = $"erp:default:targets:dash:fy_{fiscalYear}:per_{period ?? "all"}:br_{branch ?? "all"}:rep_{repUserId?.ToString() ?? "all"}";

            var cached = await _cacheManager.GetAsync<SalesTargetDashboardDto>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            var query = _context.SalesTargets
                .AsNoTracking()
                .Include(t => t.Items)
                .Where(t => !t.IsDeleted && t.FiscalYear == fiscalYear);

            if (!string.IsNullOrWhiteSpace(period))
            {
                query = query.Where(t => t.TargetPeriod == period || t.PeriodType == period);
            }

            if (!string.IsNullOrWhiteSpace(branch))
            {
                query = query.Where(t => t.Branch == branch);
            }

            if (repUserId.HasValue)
            {
                query = query.Where(t => t.SalesPersonUserId == repUserId.Value);
            }

            var targets = await query.ToListAsync(ct);

            var totalTargets = targets.Count;
            var activeTargets = targets.Count(t => t.Status == SalesTargetStatuses.Active);
            var completedTargets = targets.Count(t => t.Status == SalesTargetStatuses.Completed);
            var expiredTargets = targets.Count(t => t.Status == SalesTargetStatuses.Expired);
            var cancelledTargets = targets.Count(t => t.Status == SalesTargetStatuses.Cancelled);
            var draftTargets = targets.Count(t => t.Status == SalesTargetStatuses.Draft);

            decimal companyRevenueQuota = targets.Sum(t => t.RevenueTarget);
            decimal companyRealizedRev = targets.Sum(t => t.RealizedRevenue);
            decimal companyRealizedCash = targets.Sum(t => t.RealizedCash);
            decimal companyRealizedBilled = targets.Sum(t => t.RealizedBilled);

            decimal attainmentPct = companyRevenueQuota > 0
                ? Math.Round((companyRealizedRev / companyRevenueQuota) * 100m, 2)
                : 0m;

            // Run-rate & Pace calculations
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            int elapsedDays = Math.Max(1, today.DayOfYear);
            int totalDaysInYear = DateTime.IsLeapYear(fiscalYear) ? 366 : 365;
            int remainingDays = Math.Max(1, totalDaysInYear - elapsedDays);

            decimal currentDailyRunRate = companyRealizedRev / elapsedDays;
            decimal requiredDailyRunRate = Math.Max(0m, companyRevenueQuota - companyRealizedRev) / remainingDays;
            decimal paceIndex = (companyRevenueQuota > 0 && elapsedDays > 0)
                ? Math.Round((companyRealizedRev * totalDaysInYear) / (companyRevenueQuota * elapsedDays), 2)
                : 1.0m;

            int onTrackCount = targets.Count(t => t.AchievementPercentage >= 80m);
            int deficitCount = targets.Count(t => t.AchievementPercentage < 80m && t.Status == SalesTargetStatuses.Active);

            // Top Performers ranking
            var topPerformers = targets
                .Where(t => t.SalesPersonUserId.HasValue && t.RevenueTarget > 0)
                .OrderByDescending(t => t.AchievementPercentage)
                .Take(5)
                .Select(t => new TopPerformerCardDto
                {
                    SalesPersonUserId = t.SalesPersonUserId!.Value,
                    SalesPersonName = !string.IsNullOrEmpty(t.SalesTeam) ? t.SalesTeam : $"Rep #{t.SalesPersonUserId}",
                    Branch = t.Branch,
                    TargetQuota = t.RevenueTarget,
                    AchievedRevenue = t.RealizedRevenue,
                    AttainmentPercentage = t.AchievementPercentage,
                    BadgeLevel = t.AchievementPercentage >= 120m ? "Platinum"
                               : t.AchievementPercentage >= 100m ? "Gold"
                               : t.AchievementPercentage >= 80m ? "Silver" : "Bronze"
                })
                .ToList();

            // Product Category Heatmap
            var categoryHeatmap = targets
                .SelectMany(t => t.Items.Select(i => new { Branch = t.Branch, Item = i }))
                .GroupBy(x => new { x.Branch, x.Item.CategoryName })
                .Select(g => new CategoryAchievementHeatmapDto
                {
                    Branch = string.IsNullOrEmpty(g.Key.Branch) ? "General" : g.Key.Branch,
                    CategoryName = string.IsNullOrEmpty(g.Key.CategoryName) ? "Unassigned" : g.Key.CategoryName,
                    TargetAmount = g.Sum(x => x.Item.TargetValue),
                    RealizedAmount = g.Sum(x => x.Item.RealizedValue),
                    AttainmentPercentage = g.Sum(x => x.Item.TargetValue) > 0
                        ? Math.Round((g.Sum(x => x.Item.RealizedValue) / g.Sum(x => x.Item.TargetValue)) * 100m, 2)
                        : 0m
                })
                .OrderBy(x => x.Branch)
                .ThenBy(x => x.CategoryName)
                .ToList();

            var recentTargets = targets
                .OrderByDescending(t => t.CreatedDate)
                .Take(10)
                .Select(t => new SalesTargetListItemDto
                {
                    Id = t.Id,
                    TargetNumber = t.TargetNumber,
                    TargetName = t.TargetName,
                    TargetPeriod = t.TargetPeriod,
                    PeriodType = t.PeriodType,
                    TargetCategory = t.TargetCategory,
                    AssignmentType = t.AssignmentType,
                    SalesPersonUserId = t.SalesPersonUserId,
                    SalesTeam = t.SalesTeam,
                    Branch = t.Branch,
                    FinancialYear = t.FiscalYear,
                    StartDate = t.StartDate.ToString("yyyy-MM-dd"),
                    EndDate = t.EndDate.ToString("yyyy-MM-dd"),
                    RevenueTarget = t.RevenueTarget,
                    VolumeTarget = t.VolumeTarget,
                    AchievedVolume = t.AchievedVolume,
                    MarginHurdleTarget = t.MarginHurdleTarget,
                    RealizedRevenue = t.RealizedRevenue,
                    RealizedCash = t.RealizedCash,
                    RealizedBilled = t.RealizedBilled,
                    AchievedValue = t.AchievedValue,
                    RemainingValue = t.RemainingValue,
                    OverAchievementValue = t.OverAchievementValue,
                    AchievementPercentage = t.AchievementPercentage,
                    IsLocked = t.IsLocked,
                    RevisionNumber = t.RevisionNumber,
                    Currency = t.Currency,
                    Status = t.Status,
                    ParentTargetId = t.ParentTargetId
                })
                .ToList();

            var dto = new SalesTargetDashboardDto
            {
                TotalCount = totalTargets,
                DraftCount = draftTargets,
                ActiveCount = activeTargets,
                CompletedCount = completedTargets,
                ExpiredCount = expiredTargets,
                CancelledCount = cancelledTargets,
                TotalTargetValue = companyRevenueQuota,
                TotalAchievedValue = companyRealizedRev,
                AverageAchievementPercentage = targets.Any() ? Math.Round(targets.Average(t => t.AchievementPercentage), 2) : 0m,
                CompanyRevenueQuota = companyRevenueQuota,
                CompanyRealizedRevenue = companyRealizedRev,
                CompanyRealizedCash = companyRealizedCash,
                CompanyRealizedBilled = companyRealizedBilled,
                CompanyAttainmentPercentage = attainmentPct,
                RequiredDailyRunRate = Math.Round(requiredDailyRunRate, 2),
                CurrentDailyRunRate = Math.Round(currentDailyRunRate, 2),
                PaceIndex = paceIndex,
                OnTrackCount = onTrackCount,
                DeficitCount = deficitCount,
                TopPerformers = topPerformers,
                CategoryHeatmap = categoryHeatmap,
                Recent = recentTargets
            };

            await _cacheManager.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(5));
            return dto;
        }

        public async Task InvalidateDashboardCacheAsync(string tenantId, CancellationToken ct = default)
        {
            await _cacheManager.InvalidateTenantAsync(tenantId);
        }
    }
}

