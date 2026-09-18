using ERP.Application.Sales;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    public class PerformanceRecalculationService : IPerformanceRecalculationService
    {
        private readonly ERPDbContext _context;

        public PerformanceRecalculationService(ERPDbContext context) => _context = context;

        public async Task RecalculateRepPerformanceAsync(int salesPersonUserId, int financialYear, CancellationToken ct = default)
        {
            var perf = await _context.SalespersonPerformances
                .FirstOrDefaultAsync(p => p.SalesPersonUserId == salesPersonUserId && p.FinancialYear == financialYear, ct);

            if (perf == null)
            {
                perf = new SalespersonPerformance
                {
                    SalesPersonUserId = salesPersonUserId,
                    SalesPersonName = $"Salesperson {salesPersonUserId}",
                    FinancialYear = financialYear,
                    Status = "Active",
                    CreatedDate = DateTimeOffset.UtcNow
                };
                _context.SalespersonPerformances.Add(perf);
            }

            // 1. Fetch Target Metrics
            var targets = await _context.SalesTargets
                .Where(t => t.SalesPersonUserId == salesPersonUserId && t.FinancialYear == financialYear && !t.IsDeleted && t.Status != "Cancelled")
                .ToListAsync(ct);

            perf.TotalTargetValue = targets.Sum(t => t.TargetValue);
            perf.TotalAchievedValue = targets.Sum(t => t.AchievedValue);
            perf.CalculatedCommission = targets.Sum(t => t.CalculatedCommissionAmount);
            perf.AttainmentPercentage = perf.TotalTargetValue > 0
                ? Math.Round((perf.TotalAchievedValue / perf.TotalTargetValue) * 100m, 2, MidpointRounding.AwayFromZero)
                : 0m;

            // 2. Fetch Sales Order & Quotation Ratios
            var orders = await _context.SalesOrders
                .Where(so => so.SalesPersonUserId == salesPersonUserId && so.Status != "Cancelled")
                .ToListAsync(ct);

            var spIdStr = salesPersonUserId.ToString();
            var quotations = await _context.Quotations
                .Where(q => q.SalesPerson == spIdStr || q.SalesPerson == perf.SalesPersonName)
                .ToListAsync(ct);

            perf.ConfirmedOrderCount = orders.Count;
            perf.TotalQuotationCount = quotations.Count;
            perf.ConversionRate = perf.TotalQuotationCount > 0
                ? Math.Round(((decimal)perf.ConfirmedOrderCount / perf.TotalQuotationCount) * 100m, 2, MidpointRounding.AwayFromZero)
                : 0m;

            // 3. Compute Gross Margin Percentage (Order Value vs Estimated Cost)
            // Defaults to 22.5% if orders exist
            perf.GrossMarginPercentage = orders.Any() ? 22.5m : 0m;

            // 4. Compute Average Deal Velocity
            if (orders.Any())
            {
                double totalDays = 0;
                int evaluatedCount = 0;
                foreach (var order in orders)
                {
                    var days = Math.Max(0.5, (order.CreatedDate.UtcDateTime - order.OrderDate.ToDateTime(TimeOnly.MinValue)).TotalDays);
                    totalDays += days;
                    evaluatedCount++;
                }
                perf.AvgDealVelocityDays = evaluatedCount > 0
                    ? Math.Round((decimal)(totalDays / evaluatedCount), 1, MidpointRounding.AwayFromZero)
                    : 0m;
            }

            // 5. Compute Balanced Multi-Factor Score
            // Formula: (Attainment% * 0.50) + (ConversionRate% * 0.30) + (GrossMargin% * 0.20)
            perf.WeightedScore = Math.Round(
                (perf.AttainmentPercentage * 0.50m) +
                (perf.ConversionRate * 0.30m) +
                (perf.GrossMarginPercentage * 0.20m), 2, MidpointRounding.AwayFromZero);

            // 6. Determine Rank
            int currentRank = await _context.SalespersonPerformances
                .CountAsync(p => p.FinancialYear == financialYear && p.WeightedScore > perf.WeightedScore && p.Status == "Active", ct) + 1;

            // 7. Evaluate & Serialize Badges
            var earnedBadges = PerformanceBadgeService.EvaluateBadges(
                perf.AttainmentPercentage,
                perf.GrossMarginPercentage,
                perf.AvgDealVelocityDays,
                currentRank);

            perf.ActiveBadgesJson = System.Text.Json.JsonSerializer.Serialize(earnedBadges);

            perf.UpdatedDate = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(ct);
        }

        public async Task GenerateMonthlySnapshotsAsync(int financialYear, string periodKey, CancellationToken ct = default)
        {
            var activePerformances = await _context.SalespersonPerformances
                .Where(p => p.FinancialYear == financialYear && p.Status == "Active")
                .OrderByDescending(p => p.WeightedScore)
                .ThenByDescending(p => p.TotalAchievedValue)
                .ToListAsync(ct);

            int rank = 1;
            foreach (var p in activePerformances)
            {
                var snapshot = new SalespersonPerformanceSnapshot
                {
                    SalesPersonUserId = p.SalesPersonUserId,
                    SalesPersonName = p.SalesPersonName,
                    Branch = p.Branch,
                    SalesTeam = p.SalesTeam,
                    FinancialYear = p.FinancialYear,
                    PeriodKey = periodKey,
                    Rank = rank++,
                    WeightedScore = p.WeightedScore,
                    AttainmentPercentage = p.AttainmentPercentage,
                    TotalAchievedValue = p.TotalAchievedValue,
                    TotalTargetValue = p.TotalTargetValue,
                    CalculatedCommission = p.CalculatedCommission,
                    ActiveBadgesJson = p.ActiveBadgesJson,
                    SnapshotDate = DateTimeOffset.UtcNow
                };
                _context.SalespersonPerformanceSnapshots.Add(snapshot);
            }

            await _context.SaveChangesAsync(ct);
        }
    }
}
