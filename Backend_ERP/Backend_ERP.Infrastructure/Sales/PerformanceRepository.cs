using ERP.Application.Sales;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    public class PerformanceRepository : IPerformanceRepository
    {
        private readonly ERPDbContext _db;

        public PerformanceRepository(ERPDbContext db)
        {
            _db = db;
        }

        public IQueryable<SalesOrder> SalesOrders() => _db.SalesOrders;
        public IQueryable<ProformaInvoice> ProformaInvoices() => _db.ProformaInvoices.Where(x => !x.IsDeleted);
        public IQueryable<AdvancePayment> AdvancePayments() => _db.AdvancePayments.Where(x => !x.IsDeleted);
        public IQueryable<SalesTarget> SalesTargets() => _db.SalesTargets.Where(x => !x.IsDeleted);
        public IQueryable<PerformanceSnapshot> Snapshots() => _db.PerformanceSnapshots;
        public IQueryable<PerformanceHistory> History() => _db.PerformanceHistory;
        public IQueryable<PerformanceExportHistory> ExportHistory() => _db.PerformanceExportHistory;

        public async Task AddExportAsync(
            PerformanceExportHistory export,
            CancellationToken cancellationToken = default)
        {
            await _db.PerformanceExportHistory.AddAsync(export, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<IEnumerable<ERP.Application.Sales.Dtos.PerformanceLeaderboardDto>> GetLeaderboardAsync(
            int financialYear,
            CancellationToken ct = default)
        {
            var list = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                _db.SalespersonPerformances
                    .AsNoTracking()
                    .Where(p => p.FinancialYear == financialYear && p.Status == "Active")
                    .OrderByDescending(p => p.WeightedScore)
                    .ThenByDescending(p => p.TotalAchievedValue),
                ct);

            int rank = 1;
            return list.Select(p => new ERP.Application.Sales.Dtos.PerformanceLeaderboardDto
            {
                Id = p.Id,
                SalesPersonUserId = p.SalesPersonUserId,
                SalesPerson = p.SalesPersonName,
                SalesPersonName = p.SalesPersonName,
                SalesTeam = p.SalesTeam,
                Branch = p.Branch,
                Rank = rank++,
                WeightedScore = p.WeightedScore,
                TargetValue = p.TotalTargetValue,
                AchievedValue = p.TotalAchievedValue,
                AttainmentPercentage = p.AttainmentPercentage,
                AchievementPercentage = p.AttainmentPercentage,
                ConfirmedOrderCount = p.ConfirmedOrderCount,
                ConversionRate = p.ConversionRate,
                CalculatedCommission = p.CalculatedCommission,
                Status = p.Status,
                IsTopPerformer = rank <= 4,
                ActiveBadgesJson = p.ActiveBadgesJson
            });
        }
    }
}
