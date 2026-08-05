using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    public class PriceListRepository : IPriceListRepository
    {
        private readonly ERPDbContext _db;

        public PriceListRepository(ERPDbContext db)
        {
            _db = db;
        }

        public IQueryable<PriceList> Query() =>
            _db.PriceLists.Where(x => !x.IsDeleted);

        public async Task<IReadOnlyList<PriceList>> GetAllAsync(
            PriceListListQueryDto? query,
            CancellationToken cancellationToken = default)
        {
            var q = ApplyFilters(Query().AsNoTracking().Include(x => x.Items), query);
            return await q
                .OrderByDescending(x => x.EffectiveFrom)
                .ThenByDescending(x => x.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<PriceList?> GetByIdAsync(
            int id,
            bool includeDetails,
            bool asTracking,
            CancellationToken cancellationToken = default)
        {
            IQueryable<PriceList> q = _db.PriceLists.Where(x => !x.IsDeleted);
            if (includeDetails)
            {
                q = q.Include(x => x.Items).Include(x => x.History);
            }

            if (!asTracking)
            {
                q = q.AsNoTracking();
            }

            return await q.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<PriceList> CreateAsync(
            PriceList entity,
            CancellationToken cancellationToken = default)
        {
            await _db.PriceLists.AddAsync(entity, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task<PriceList?> UpdateAsync(
            PriceList entity,
            CancellationToken cancellationToken = default)
        {
            await _db.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task<bool> DeleteAsync(
            PriceList entity,
            CancellationToken cancellationToken = default)
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<IReadOnlyList<PriceList>> GetActiveAsync(
            DateOnly? asOfDate,
            CancellationToken cancellationToken = default)
        {
            var date = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            return await Query().AsNoTracking()
                .Include(x => x.Items)
                .Where(x => x.Status == PriceListStatuses.Active
                    && x.EffectiveFrom <= date
                    && (x.EffectiveTo == null || x.EffectiveTo >= date))
                .OrderByDescending(x => x.EffectiveFrom)
                .ToListAsync(cancellationToken);
        }

        public Task<bool> PriceListNumberExistsAsync(
            string priceListNumber,
            int? excludeId,
            CancellationToken cancellationToken = default)
        {
            var q = _db.PriceLists.Where(x => x.PriceListNumber == priceListNumber);
            if (excludeId is int id)
            {
                q = q.Where(x => x.Id != id);
            }

            return q.AnyAsync(cancellationToken);
        }

        public Task<bool> HasOverlappingActiveAsync(
            string priceListName,
            string customerCategory,
            string currency,
            DateOnly effectiveFrom,
            DateOnly? effectiveTo,
            int? excludeId,
            CancellationToken cancellationToken = default)
        {
            var end = effectiveTo ?? DateOnly.MaxValue;
            var q = Query().Where(x =>
                x.Status == PriceListStatuses.Active
                && x.PriceListName == priceListName
                && x.CustomerCategory == customerCategory
                && x.Currency == currency
                && x.EffectiveFrom <= end
                && (x.EffectiveTo == null || x.EffectiveTo >= effectiveFrom));

            if (excludeId is int id)
            {
                q = q.Where(x => x.Id != id);
            }

            return q.AnyAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<PriceListHistory>> GetHistoryAsync(
            int priceListId,
            CancellationToken cancellationToken = default)
        {
            var exists = await Query().AnyAsync(x => x.Id == priceListId, cancellationToken);
            if (!exists)
            {
                return Array.Empty<PriceListHistory>();
            }

            return await _db.PriceListHistories.AsNoTracking()
                .Where(x => x.PriceListId == priceListId)
                .OrderBy(x => x.ChangedOn)
                .ToListAsync(cancellationToken);
        }

        private static IQueryable<PriceList> ApplyFilters(
            IQueryable<PriceList> q,
            PriceListListQueryDto? query)
        {
            if (query is null)
            {
                return q;
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                var status = PriceListStatusRules.Normalize(query.Status) ?? query.Status.Trim();
                q = q.Where(x => x.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(query.CustomerCategory))
            {
                var category = PriceListCustomerCategoryRules.Normalize(query.CustomerCategory)
                    ?? query.CustomerCategory.Trim();
                q = q.Where(x => x.CustomerCategory == category);
            }

            if (!string.IsNullOrWhiteSpace(query.Currency))
            {
                var currency = query.Currency.Trim().ToUpperInvariant();
                q = q.Where(x => x.Currency == currency);
            }

            var from = PriceListMapper.ParseOptionalDate(query.DateFrom);
            if (from is not null)
            {
                q = q.Where(x => x.EffectiveFrom >= from);
            }

            var to = PriceListMapper.ParseOptionalDate(query.DateTo);
            if (to is not null)
            {
                q = q.Where(x => x.EffectiveFrom <= to);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim().ToLower();
                q = q.Where(x =>
                    x.PriceListNumber.ToLower().Contains(term)
                    || x.PriceListName.ToLower().Contains(term)
                    || x.Description.ToLower().Contains(term)
                    || x.CustomerCategory.ToLower().Contains(term));
            }

            return q;
        }
    }
}
