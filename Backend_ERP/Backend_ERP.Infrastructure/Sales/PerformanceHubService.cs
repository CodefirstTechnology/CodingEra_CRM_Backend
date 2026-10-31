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
using Microsoft.Extensions.Logging;

namespace ERP.Infrastructure.Sales
{
    public class PerformanceHubService : IPerformanceHubService
    {
        private readonly ERPDbContext _context;
        private readonly IPerformanceScoringEngine _scoringEngine;
        private readonly IPerformanceRecalculationService _recalcService;
        private readonly IPerformanceRedisCacheManager? _redisCache;
        private readonly ILogger<PerformanceHubService> _logger;

        public PerformanceHubService(
            ERPDbContext context,
            IPerformanceScoringEngine? scoringEngine = null,
            IPerformanceRecalculationService? recalcService = null,
            IPerformanceRedisCacheManager? redisCache = null,
            ILogger<PerformanceHubService>? logger = null)
        {
            _context = context;
            _scoringEngine = scoringEngine ?? new PerformanceScoringEngine();
            _recalcService = recalcService ?? new PerformanceRecalculationService(context, _scoringEngine, redisCache);
            _redisCache = redisCache;
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PerformanceHubService>.Instance;
        }

        public async Task<RepOverviewCockpitDto> GetRepOverviewAsync(
            int salesPersonUserId,
            int financialYear,
            CancellationToken ct = default)
        {
            var perf = await _context.SalespersonPerformances
                .Include(p => p.Badges)
                .Include(p => p.CommissionEntries)
                .FirstOrDefaultAsync(p => p.SalesPersonUserId == salesPersonUserId && p.FinancialYear == financialYear, ct);

            if (perf == null)
            {
                await _recalcService.RecalculateRepPerformanceAsync(salesPersonUserId, financialYear, ct);
                perf = await _context.SalespersonPerformances
                    .Include(p => p.Badges)
                    .Include(p => p.CommissionEntries)
                    .FirstOrDefaultAsync(p => p.SalesPersonUserId == salesPersonUserId && p.FinancialYear == financialYear, ct);
            }

            if (perf == null)
            {
                return new RepOverviewCockpitDto { SalesPersonUserId = salesPersonUserId, FinancialYear = financialYear };
            }

            // Expected Attainment based on elapsed fiscal year time
            var fyStart = new DateTime(financialYear - 1, 4, 1, 0, 0, 0, DateTimeKind.Utc);
            var fyEnd = new DateTime(financialYear, 3, 31, 23, 59, 59, DateTimeKind.Utc);
            var now = DateTime.UtcNow;
            decimal expectedAttainment = 100m;
            if (now >= fyStart && now <= fyEnd)
            {
                double total = (fyEnd - fyStart).TotalDays;
                double elapsed = (now - fyStart).TotalDays;
                expectedAttainment = Math.Round((decimal)(elapsed / total) * 100m, 1);
            }

            // Cash collection %
            decimal cashCollectionPct = perf.TotalBookedRevenue > 0
                ? Math.Round((perf.TotalCashCollected / perf.TotalBookedRevenue) * 100m, 1)
                : 0m;

            // Radar Skill Matrix
            var radar = _scoringEngine.CalculateRadarSkills(
                perf.AttainmentPercentage,
                cashCollectionPct,
                perf.GrossMarginPercentage,
                perf.AvgDealVelocityDays,
                perf.ConversionRate);

            // Milestones
            var milestones = new List<AttainmentMilestoneDto>();
            decimal[] milestoneTargets = { 25m, 50m, 75m, 100m, 125m, 150m };
            foreach (var targetPct in milestoneTargets)
            {
                decimal reqVal = Math.Round(perf.TotalTargetValue * (targetPct / 100m), 2);
                bool achieved = perf.TotalAchievedValue >= reqVal && reqVal > 0;
                milestones.Add(new AttainmentMilestoneDto
                {
                    TargetPercentage = targetPct,
                    Label = $"{targetPct:F0}% Milestone",
                    IsAchieved = achieved,
                    AchievedValue = Math.Min(perf.TotalAchievedValue, reqVal),
                    RequiredValue = reqVal,
                    AchievedDate = achieved ? perf.UpdatedDate : null
                });
            }

            // Badges
            var badgeDtos = perf.Badges
                .Select(b => new SalesRepBadgeDto
                {
                    Id = b.Id,
                    BadgeCode = b.BadgeCode,
                    BadgeName = b.BadgeTitle,
                    Description = b.BadgeCategory,
                    IconName = b.Icon,
                    BadgeCategory = b.BadgeCategory,
                    CriteriaSnapshotJson = b.EvaluationCriteriaSnapshot,
                    AwardedDate = b.AwardedDate,
                    FinancialYear = financialYear
                })
                .ToList();

            // Recent Commissions
            var recentLedgers = await _context.SalesCommissionLedgers
                .Where(c => c.ProfileId == perf.Id || c.SalesPersonUserId == salesPersonUserId)
                .OrderByDescending(c => c.AccruedDate)
                .Take(15)
                .ToListAsync(ct);

            var recentCommissions = recentLedgers
                .Select(c => new SalesCommissionLedgerDto
                {
                    Id = c.Id,
                    ProfileId = c.ProfileId,
                    SalesPersonUserId = c.SalesPersonUserId,
                    SalesPersonName = perf.SalesPersonName,
                    CommercialDocumentType = c.SalesOrderId.HasValue ? "SalesOrder" : (c.AdvancePaymentId.HasValue ? "AdvancePayment" : "Disbursement"),
                    CommercialDocumentId = c.SalesOrderId ?? c.AdvancePaymentId ?? 0,
                    CommercialDocumentNumber = c.PaymentVoucherReference ?? $"DOC-{c.SalesOrderId}",
                    TransactionDate = c.AccruedDate,
                    DocumentAmount = c.TransactionAmount,
                    GrossMarginAmount = 0m,
                    GrossMarginPercentage = 0m,
                    BaseRateApplied = c.CommissionRate,
                    AcceleratorMultiplier = c.AcceleratorMultiplier,
                    AccruedCommission = c.AccruedCommission,
                    CashGatePassed = c.PayoutStatus != "GatedPendingCash",
                    IsClawback = c.IsClawback,
                    ClawbackReason = c.ClawbackReason,
                    Status = c.PayoutStatus,
                    TransactionType = c.TransactionType,
                    Notes = c.ClawbackReason,
                    ApprovedByUserId = int.TryParse(c.ApprovedByUserId, out var parsedAppr) ? parsedAppr : null,
                    ApprovedDate = c.PayoutDate
                })
                .ToList();

            decimal pendingCashGated = await _context.SalesCommissionLedgers
                .Where(c => c.SalesPersonUserId == salesPersonUserId && c.PayoutStatus == "GatedPendingCash")
                .SumAsync(c => (decimal?)c.AccruedCommission, ct) ?? 0m;

            return new RepOverviewCockpitDto
            {
                SalesPersonUserId = perf.SalesPersonUserId,
                SalesPersonName = perf.SalesPersonName,
                SalesTeam = perf.SalesTeam,
                Branch = perf.Branch,
                RegionalManager = perf.RegionalManager,
                TerritoryId = perf.TerritoryId,
                FinancialYear = perf.FinancialYear,

                TotalTargetValue = perf.TotalTargetValue,
                TotalAchievedValue = perf.TotalAchievedValue,
                TotalBookedRevenue = perf.TotalBookedRevenue,
                TotalCashCollected = perf.TotalCashCollected,
                TotalDispatchedRevenue = perf.TotalDispatchedRevenue,
                AttainmentPercentage = perf.AttainmentPercentage,
                PacingIndex = perf.PacingIndex,
                ExpectedAttainmentPercentage = expectedAttainment,

                GrossMarginPercentage = perf.GrossMarginPercentage,
                TotalGrossMarginAmount = perf.TotalGrossMarginAmount,
                DiscountDisciplineScore = perf.DiscountDisciplineScore,
                AvgDealVelocityDays = perf.AvgDealVelocityDays,
                ConversionRate = perf.ConversionRate,
                ConfirmedOrderCount = perf.ConfirmedOrderCount,
                TotalQuotationCount = perf.TotalQuotationCount,

                CurrentRank = perf.CurrentRank,
                PreviousRank = perf.PreviousRank,
                TierBadge = perf.TierBadge,
                WinningStreakMonths = perf.WinningStreakMonths,
                WeightedScore = perf.WeightedScore,
                Badges = badgeDtos,
                SkillRadar = radar,
                Milestones = milestones,

                TotalAccruedCommission = perf.TotalAccruedCommission,
                TotalClawbackCommission = perf.TotalClawbackCommission,
                TotalPaidCommission = perf.TotalPaidCommission,
                NetPayableCommission = perf.NetPayableCommission,
                PendingCashGatedCommission = pendingCashGated,
                RecentCommissions = recentCommissions
            };
        }

        public async Task<LeaderboardPodiumDto> GetLeaderboardAsync(
            LeaderboardFilterDto filter,
            CancellationToken ct = default)
        {
            var query = _context.SalespersonPerformances
                .Include(p => p.Badges)
                .Where(p => p.FinancialYear == filter.FinancialYear && p.Status == "Active");

            if (!string.IsNullOrWhiteSpace(filter.SalesTeam))
                query = query.Where(p => p.SalesTeam == filter.SalesTeam);

            if (!string.IsNullOrWhiteSpace(filter.Branch))
                query = query.Where(p => p.Branch == filter.Branch);

            if (filter.TerritoryId.HasValue)
                query = query.Where(p => p.TerritoryId == filter.TerritoryId.Value);

            // Dynamic Sorting based on selected metric
            query = (filter.Metric?.ToLowerInvariant()) switch
            {
                "achievedvalue" => query.OrderByDescending(p => p.TotalAchievedValue),
                "attainmentpercentage" => query.OrderByDescending(p => p.AttainmentPercentage),
                "grossmarginpercentage" => query.OrderByDescending(p => p.GrossMarginPercentage),
                "cashcollected" => query.OrderByDescending(p => p.TotalCashCollected),
                _ => query.OrderByDescending(p => p.WeightedScore).ThenByDescending(p => p.TotalAchievedValue)
            };

            var allPerformances = await query.ToListAsync(ct);
            int totalCount = allPerformances.Count;

            int rank = 1;
            var mappedItems = new List<LeaderboardItemDto>();
            foreach (var p in allPerformances)
            {
                var badges = p.Badges.Select(b => new SalesRepBadgeDto
                {
                    Id = b.Id,
                    BadgeCode = b.BadgeCode,
                    BadgeName = b.BadgeTitle,
                    Description = b.BadgeCategory,
                    IconName = b.Icon,
                    BadgeCategory = b.BadgeCategory,
                    CriteriaSnapshotJson = b.EvaluationCriteriaSnapshot,
                    AwardedDate = b.AwardedDate,
                    FinancialYear = filter.FinancialYear
                }).ToList();

                mappedItems.Add(new LeaderboardItemDto
                {
                    Rank = rank,
                    PreviousRank = p.PreviousRank > 0 ? p.PreviousRank : rank,
                    SalesPersonUserId = p.SalesPersonUserId,
                    SalesPersonName = p.SalesPersonName,
                    SalesTeam = p.SalesTeam,
                    Branch = p.Branch,
                    TerritoryId = p.TerritoryId,
                    WeightedScore = p.WeightedScore,
                    TotalTargetValue = p.TotalTargetValue,
                    TotalAchievedValue = p.TotalAchievedValue,
                    AttainmentPercentage = p.AttainmentPercentage,
                    GrossMarginPercentage = p.GrossMarginPercentage,
                    TotalCashCollected = p.TotalCashCollected,
                    ConfirmedOrderCount = p.ConfirmedOrderCount,
                    AvgDealVelocityDays = p.AvgDealVelocityDays,
                    TierBadge = p.TierBadge,
                    WinningStreakMonths = p.WinningStreakMonths,
                    Badges = badges
                });
                rank++;
            }

            var top3 = mappedItems.Take(3).ToList();
            var pagedRankings = mappedItems
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            return new LeaderboardPodiumDto
            {
                PodiumTop3 = top3,
                Rankings = pagedRankings,
                TotalCount = totalCount,
                GeneratedAt = DateTimeOffset.UtcNow,
                DataSource = "Database"
            };
        }

        public async Task<List<SalesCommissionLedgerDto>> GetRepCommissionLedgerAsync(
            int salesPersonUserId,
            int financialYear,
            CancellationToken ct = default)
        {
            var rawLedgers = await _context.SalesCommissionLedgers
                .Where(c => c.SalesPersonUserId == salesPersonUserId)
                .OrderByDescending(c => c.AccruedDate)
                .ToListAsync(ct);

            return rawLedgers
                .Select(c => new SalesCommissionLedgerDto
                {
                    Id = c.Id,
                    ProfileId = c.ProfileId,
                    SalesPersonUserId = c.SalesPersonUserId,
                    CommercialDocumentType = c.SalesOrderId.HasValue ? "SalesOrder" : (c.AdvancePaymentId.HasValue ? "AdvancePayment" : "Disbursement"),
                    CommercialDocumentId = c.SalesOrderId ?? c.AdvancePaymentId ?? 0,
                    CommercialDocumentNumber = c.PaymentVoucherReference ?? $"DOC-{c.SalesOrderId}",
                    TransactionDate = c.AccruedDate,
                    DocumentAmount = c.TransactionAmount,
                    GrossMarginAmount = 0m,
                    GrossMarginPercentage = 0m,
                    BaseRateApplied = c.CommissionRate,
                    AcceleratorMultiplier = c.AcceleratorMultiplier,
                    AccruedCommission = c.AccruedCommission,
                    CashGatePassed = c.PayoutStatus != "GatedPendingCash",
                    IsClawback = c.IsClawback,
                    ClawbackReason = c.ClawbackReason,
                    Status = c.PayoutStatus,
                    TransactionType = c.TransactionType,
                    Notes = c.ClawbackReason,
                    ApprovedByUserId = int.TryParse(c.ApprovedByUserId, out var parsedAppr) ? parsedAppr : null,
                    ApprovedDate = c.PayoutDate
                })
                .ToList();
        }

        public async Task<bool> ApproveCommissionEntryAsync(
            CommissionApprovalRequestDto request,
            int approverUserId,
            CancellationToken ct = default)
        {
            var entry = await _context.SalesCommissionLedgers
                .FirstOrDefaultAsync(l => l.Id == request.LedgerId, ct);

            if (entry == null) return false;

            entry.PayoutStatus = request.Approved ? "ApprovedForPayment" : "Rejected";
            entry.ApprovedByUserId = approverUserId.ToString();
            entry.PayoutDate = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DisburseCommissionPayoutAsync(
            CommissionDisbursementRequestDto request,
            int disburserUserId,
            CancellationToken ct = default)
        {
            var perf = await _context.SalespersonPerformances
                .FirstOrDefaultAsync(p => p.SalesPersonUserId == request.SalesPersonUserId && p.FinancialYear == request.FinancialYear, ct);

            if (perf == null) return false;

            var disbursement = new SalesCommissionLedger
            {
                TenantId = perf.TenantId,
                ProfileId = perf.Id,
                SalesPersonUserId = request.SalesPersonUserId,
                TransactionType = "PayoutDisbursement",
                TransactionAmount = 0m,
                CommissionRate = 0m,
                AcceleratorMultiplier = 1.0m,
                AccruedCommission = Math.Abs(request.Amount),
                IsClawback = false,
                PayoutStatus = "Disbursed",
                PaymentVoucherReference = request.DisbursementReference,
                ApprovedByUserId = disburserUserId.ToString(),
                PayoutDate = DateTimeOffset.UtcNow,
                AccruedDate = DateTimeOffset.UtcNow
            };

            _context.SalesCommissionLedgers.Add(disbursement);
            await _context.SaveChangesAsync(ct); // Trigger fn_trg_sync_rep_commission_totals will sync TotalPaidCommission!

            return true;
        }

        public async Task<List<IncentiveSchemeDto>> GetIncentiveSchemesAsync(
            int financialYear,
            CancellationToken ct = default)
        {
            var schemes = await _context.SalesIncentiveSchemes
                .Where(s => s.FiscalYear == financialYear)
                .OrderByDescending(s => s.Id)
                .ToListAsync(ct);

            return schemes.Select(s =>
            {
                List<IncentiveAcceleratorTier> tiers = new();
                if (!string.IsNullOrWhiteSpace(s.AcceleratorTiersJson))
                {
                    try { tiers = JsonSerializer.Deserialize<List<IncentiveAcceleratorTier>>(s.AcceleratorTiersJson) ?? new(); }
                    catch { }
                }

                return new IncentiveSchemeDto
                {
                    Id = s.Id,
                    SchemeCode = s.SchemeCode,
                    SchemeName = s.SchemeName,
                    FiscalYear = s.FiscalYear,
                    BaseCommissionRate = s.BaseCommissionRate,
                    GrossMarginHurdlePercentage = s.MarginHurdlePercentage,
                    RequireAdvanceReceiptForAccrual = s.RequiresCashClearance,
                    MinCashClearancePercentage = s.MinCashClearancePercentage,
                    TierAccelerators = tiers,
                    IsActive = s.Status == "Active",
                    Notes = s.PeriodType
                };
            }).ToList();
        }

        public async Task<IncentiveSchemeDto> CreateIncentiveSchemeAsync(
            CreateIncentiveSchemeDto request,
            int creatorUserId,
            CancellationToken ct = default)
        {
            var scheme = new SalesIncentiveScheme
            {
                SchemeCode = request.SchemeCode,
                SchemeName = request.SchemeName,
                FiscalYear = request.FiscalYear,
                BaseCommissionRate = request.BaseCommissionRate,
                MarginHurdlePercentage = request.GrossMarginHurdlePercentage,
                RequiresCashClearance = request.RequireAdvanceReceiptForAccrual,
                MinCashClearancePercentage = request.MinCashClearancePercentage,
                AcceleratorTiersJson = JsonSerializer.Serialize(request.TierAccelerators ?? new List<IncentiveAcceleratorTier>()),
                Status = request.IsActive ? "Active" : "Draft",
                CreatedDate = DateTimeOffset.UtcNow
            };

            _context.SalesIncentiveSchemes.Add(scheme);
            await _context.SaveChangesAsync(ct);

            return new IncentiveSchemeDto
            {
                Id = scheme.Id,
                SchemeCode = scheme.SchemeCode,
                SchemeName = scheme.SchemeName,
                FiscalYear = scheme.FiscalYear,
                BaseCommissionRate = scheme.BaseCommissionRate,
                GrossMarginHurdlePercentage = scheme.MarginHurdlePercentage,
                RequireAdvanceReceiptForAccrual = scheme.RequiresCashClearance,
                MinCashClearancePercentage = scheme.MinCashClearancePercentage,
                TierAccelerators = request.TierAccelerators ?? new(),
                IsActive = scheme.Status == "Active"
            };
        }

        public async Task<bool> ToggleSchemeActiveAsync(
            int schemeId,
            bool isActive,
            CancellationToken ct = default)
        {
            var scheme = await _context.SalesIncentiveSchemes.FindAsync(new object[] { schemeId }, ct);
            if (scheme == null) return false;

            scheme.Status = isActive ? "Active" : "Inactive";
            await _context.SaveChangesAsync(ct);
            return true;
        }
    }
}
