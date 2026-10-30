using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    public class SalesTargetReportService : ISalesTargetReportService
    {
        private readonly ERPDbContext _context;

        public SalesTargetReportService(ERPDbContext context)
        {
            _context = context;
        }

        public async Task<SalesTargetVarianceReportDto> GetVarianceReportAsync(
            SalesTargetReportFilterDto filter,
            CancellationToken ct = default)
        {
            var query = _context.SalesTargets
                .AsNoTracking()
                .Where(t => !t.IsDeleted && t.FiscalYear == (filter.FiscalYear ?? 2026));

            if (!string.IsNullOrWhiteSpace(filter.Period))
            {
                query = query.Where(t => t.TargetPeriod == filter.Period || t.PeriodType == filter.Period);
            }

            if (!string.IsNullOrWhiteSpace(filter.Branch))
            {
                query = query.Where(t => t.Branch == filter.Branch);
            }

            if (filter.SalesPersonUserId.HasValue)
            {
                query = query.Where(t => t.SalesPersonUserId == filter.SalesPersonUserId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                query = query.Where(t => t.Status == filter.Status);
            }

            var targets = await query.ToListAsync(ct);

            var rows = targets.Select(t =>
            {
                decimal bookingPct = t.RevenueTarget > 0
                    ? Math.Round((t.RealizedRevenue / t.RevenueTarget) * 100m, 2)
                    : 0m;
                decimal cashConversionPct = t.RealizedRevenue > 0
                    ? Math.Round((t.RealizedCash / t.RealizedRevenue) * 100m, 2)
                    : 0m;
                decimal variance = t.RealizedRevenue - t.RevenueTarget;

                string pace = bookingPct >= 105m ? "Ahead"
                            : bookingPct >= 95m ? "OnTrack" : "Deficit";

                return new VarianceRowDto
                {
                    TargetId = t.Id,
                    TargetNumber = t.TargetNumber,
                    TargetName = t.TargetName,
                    Branch = t.Branch,
                    SalesPersonName = !string.IsNullOrEmpty(t.SalesTeam) ? t.SalesTeam : $"User #{t.SalesPersonUserId}",
                    TargetQuota = t.RevenueTarget,
                    ConfirmedBookings = t.RealizedRevenue,
                    DispatchedBilled = t.RealizedBilled,
                    CashCollected = t.RealizedCash,
                    BookingAttainmentPct = bookingPct,
                    CashConversionPct = cashConversionPct,
                    VarianceAmount = variance,
                    PaceStatus = pace
                };
            }).ToList();

            decimal totalTarget = rows.Sum(r => r.TargetQuota);
            decimal totalBooked = rows.Sum(r => r.ConfirmedBookings);
            decimal totalBilled = rows.Sum(r => r.DispatchedBilled);
            decimal totalCash = rows.Sum(r => r.CashCollected);

            decimal overallPct = totalTarget > 0
                ? Math.Round((totalBooked / totalTarget) * 100m, 2)
                : 0m;

            return new SalesTargetVarianceReportDto
            {
                GeneratedOn = DateTime.UtcNow.ToString("O"),
                FiscalYear = filter.FiscalYear ?? 2026,
                Period = filter.Period ?? "FY",
                Rows = rows,
                TotalTarget = totalTarget,
                TotalBooked = totalBooked,
                TotalBilled = totalBilled,
                TotalCash = totalCash,
                OverallAttainmentPct = overallPct
            };
        }

        public async Task<SalesTargetRepLeaderboardDto> GetRepLeaderboardAuditAsync(
            int fiscalYear,
            string? period,
            CancellationToken ct = default)
        {
            var query = _context.SalesTargets
                .AsNoTracking()
                .Where(t => !t.IsDeleted && t.FiscalYear == fiscalYear && t.SalesPersonUserId.HasValue);

            if (!string.IsNullOrWhiteSpace(period))
            {
                query = query.Where(t => t.TargetPeriod == period || t.PeriodType == period);
            }

            var targets = await query.ToListAsync(ct);

            int rank = 1;
            var rankings = targets
                .OrderByDescending(t => t.AchievementPercentage)
                .ThenByDescending(t => t.RealizedRevenue)
                .Select(t =>
                {
                    decimal deficit = Math.Max(0m, t.RevenueTarget - t.RealizedRevenue);
                    string tier = t.AchievementPercentage >= 120m ? "Super2.0x"
                                : t.AchievementPercentage >= 100m ? "Accelerator1.5x"
                                : t.AchievementPercentage >= 80m ? "Base" : "Floor";

                    return new RepLeaderboardRowDto
                    {
                        Rank = rank++,
                        SalesPersonUserId = t.SalesPersonUserId!.Value,
                        SalesPersonName = !string.IsNullOrEmpty(t.SalesTeam) ? t.SalesTeam : $"Rep #{t.SalesPersonUserId}",
                        Branch = t.Branch,
                        TargetQuota = t.RevenueTarget,
                        RealizedRevenue = t.RealizedRevenue,
                        RealizedCash = t.RealizedCash,
                        AttainmentPercentage = t.AchievementPercentage,
                        DeficitAmount = deficit,
                        RequiredDailyRunRate = Math.Round(deficit / 30m, 2),
                        AcceleratorTier = tier
                    };
                })
                .ToList();

            return new SalesTargetRepLeaderboardDto
            {
                GeneratedOn = DateTime.UtcNow.ToString("O"),
                FiscalYear = fiscalYear,
                Rankings = rankings
            };
        }

        public async Task<SalesTargetCategoryMatrixDto> GetCategoryContributionMatrixAsync(
            int fiscalYear,
            string? period,
            CancellationToken ct = default)
        {
            var query = _context.SalesTargets
                .AsNoTracking()
                .Include(t => t.Items)
                .Where(t => !t.IsDeleted && t.FiscalYear == fiscalYear);

            if (!string.IsNullOrWhiteSpace(period))
            {
                query = query.Where(t => t.TargetPeriod == period || t.PeriodType == period);
            }

            var targets = await query.ToListAsync(ct);
            var items = targets.SelectMany(t => t.Items).ToList();

            decimal totalAllRealized = items.Sum(i => i.RealizedValue);

            var grouped = items
                .GroupBy(i => string.IsNullOrEmpty(i.CategoryName) ? "General" : i.CategoryName)
                .Select(g =>
                {
                    decimal targetAmt = g.Sum(x => x.TargetValue);
                    decimal realizedAmt = g.Sum(x => x.RealizedValue);
                    decimal attainment = targetAmt > 0 ? Math.Round((realizedAmt / targetAmt) * 100m, 2) : 0m;
                    decimal contribution = totalAllRealized > 0 ? Math.Round((realizedAmt / totalAllRealized) * 100m, 2) : 0m;

                    return new CategoryMatrixRowDto
                    {
                        CategoryName = g.Key,
                        TargetAmount = targetAmt,
                        RealizedAmount = realizedAmt,
                        AttainmentPercentage = attainment,
                        TargetQuantity = g.Sum(x => x.TargetQuantity),
                        RealizedQuantity = g.Sum(x => x.RealizedQuantity),
                        ContributionPercentage = contribution
                    };
                })
                .OrderByDescending(x => x.RealizedAmount)
                .ToList();

            return new SalesTargetCategoryMatrixDto
            {
                GeneratedOn = DateTime.UtcNow.ToString("O"),
                FiscalYear = fiscalYear,
                Rows = grouped
            };
        }
    }
}

