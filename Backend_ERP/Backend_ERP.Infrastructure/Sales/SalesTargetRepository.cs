using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    public class SalesTargetRepository : ISalesTargetRepository
    {
        private readonly ERPDbContext _db;

        public SalesTargetRepository(ERPDbContext db)
        {
            _db = db;
        }

        public IQueryable<SalesTarget> Query() =>
            _db.SalesTargets.Where(x => !x.IsDeleted);

        public async Task<IReadOnlyList<SalesTarget>> GetAllAsync(
            SalesTargetListQueryDto? query,
            CancellationToken cancellationToken = default)
        {
            var q = ApplyFilters(Query().AsNoTracking(), query);
            return await q
                .OrderByDescending(x => x.StartDate)
                .ThenByDescending(x => x.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<SalesTarget?> GetByIdAsync(
            int id,
            bool includeDetails,
            bool asTracking,
            CancellationToken cancellationToken = default)
        {
            IQueryable<SalesTarget> q = _db.SalesTargets.Where(x => !x.IsDeleted);
            if (includeDetails)
            {
                q = q.Include(x => x.Assignments)
                    .Include(x => x.ProgressHistory)
                    .Include(x => x.StatusHistory);
            }

            if (!asTracking)
            {
                q = q.AsNoTracking();
            }

            return await q.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<SalesTarget> CreateAsync(
            SalesTarget entity,
            CancellationToken cancellationToken = default)
        {
            await _db.SalesTargets.AddAsync(entity, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task<SalesTarget?> UpdateAsync(
            SalesTarget entity,
            CancellationToken cancellationToken = default)
        {
            await _db.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task<bool> DeleteAsync(
            SalesTarget entity,
            CancellationToken cancellationToken = default)
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public Task<SalesTarget> DuplicateAsync(
            SalesTarget entity,
            CancellationToken cancellationToken = default) =>
            CreateAsync(entity, cancellationToken);

        public Task<SalesTarget?> ActivateAsync(
            SalesTarget entity,
            CancellationToken cancellationToken = default) =>
            UpdateAsync(entity, cancellationToken);

        public Task<SalesTarget?> DeactivateAsync(
            SalesTarget entity,
            CancellationToken cancellationToken = default) =>
            UpdateAsync(entity, cancellationToken);

        public Task<SalesTarget?> UpdateProgressAsync(
            SalesTarget entity,
            CancellationToken cancellationToken = default) =>
            UpdateAsync(entity, cancellationToken);

        public async Task<IReadOnlyList<SalesTarget>> GetDashboardAsync(
            CancellationToken cancellationToken = default) =>
            await Query().AsNoTracking().ToListAsync(cancellationToken);

        public Task<IReadOnlyList<SalesTarget>> GetReportsAsync(
            SalesTargetListQueryDto? query,
            CancellationToken cancellationToken = default) =>
            GetAllAsync(query, cancellationToken);

        public Task<bool> TargetNumberExistsAsync(
            string targetNumber,
            int? excludeId,
            CancellationToken cancellationToken = default)
        {
            var q = _db.SalesTargets.Where(x => x.TargetNumber == targetNumber);
            if (excludeId is int id)
            {
                q = q.Where(x => x.Id != id);
            }

            return q.AnyAsync(cancellationToken);
        }

        public Task<bool> HasOverlappingActiveTargetAsync(
            int? salesPersonUserId,
            string targetCategory,
            DateOnly startDate,
            DateOnly endDate,
            int? excludeId,
            CancellationToken cancellationToken = default)
        {
            if (salesPersonUserId is null or <= 0)
            {
                return Task.FromResult(false);
            }

            var q = Query().Where(x =>
                x.Status == SalesTargetStatuses.Active
                && x.SalesPersonUserId == salesPersonUserId
                && x.TargetCategory == targetCategory
                && x.StartDate <= endDate
                && x.EndDate >= startDate);

            if (excludeId is int id)
            {
                q = q.Where(x => x.Id != id);
            }

            return q.AnyAsync(cancellationToken);
        }

        public async Task<SalesTarget?> FindPreviousTargetAsync(
            int? salesPersonUserId,
            int? financialYear,
            string? targetCategory,
            CancellationToken cancellationToken = default)
        {
            var q = Query().AsNoTracking().AsQueryable();

            if (salesPersonUserId is int sp and > 0)
            {
                q = q.Where(x => x.SalesPersonUserId == sp);
            }

            if (financialYear is int fy and > 0)
            {
                q = q.Where(x => x.FinancialYear == fy);
            }

            if (!string.IsNullOrWhiteSpace(targetCategory))
            {
                var category = SalesTargetCategoryRules.Normalize(targetCategory) ?? targetCategory.Trim();
                q = q.Where(x => x.TargetCategory == category);
            }

            return await q
                .Include(x => x.Assignments)
                .OrderByDescending(x => x.FinancialYear)
                .ThenByDescending(x => x.EndDate)
                .ThenByDescending(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private static IQueryable<SalesTarget> ApplyFilters(
            IQueryable<SalesTarget> q,
            SalesTargetListQueryDto? query)
        {
            if (query is null)
            {
                return q;
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                var status = SalesTargetStatusRules.Normalize(query.Status) ?? query.Status.Trim();
                q = q.Where(x => x.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(query.TargetType))
            {
                var type = SalesTargetTypeRules.Normalize(query.TargetType) ?? query.TargetType.Trim();
                q = q.Where(x => x.TargetType == type);
            }

            if (!string.IsNullOrWhiteSpace(query.TargetCategory))
            {
                var category = SalesTargetCategoryRules.Normalize(query.TargetCategory)
                    ?? query.TargetCategory.Trim();
                q = q.Where(x => x.TargetCategory == category);
            }

            if (query.SalesPersonUserId is int sp and > 0)
            {
                q = q.Where(x => x.SalesPersonUserId == sp);
            }

            if (query.FinancialYear is int fy and > 0)
            {
                q = q.Where(x => x.FinancialYear == fy);
            }

            var from = SalesTargetMapper.ParseOptionalDate(query.DateFrom);
            if (from is not null)
            {
                q = q.Where(x => x.StartDate >= from);
            }

            var to = SalesTargetMapper.ParseOptionalDate(query.DateTo);
            if (to is not null)
            {
                q = q.Where(x => x.EndDate <= to);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim().ToLower();
                q = q.Where(x =>
                    x.TargetNumber.ToLower().Contains(term)
                    || x.TargetName.ToLower().Contains(term)
                    || x.SalesTeam.ToLower().Contains(term)
                    || x.Branch.ToLower().Contains(term)
                    || x.RegionalManager.ToLower().Contains(term));
            }

            return q;
        }
    }
}
