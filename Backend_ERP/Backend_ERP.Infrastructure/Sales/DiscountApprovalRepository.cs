using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    public class DiscountApprovalRepository : IDiscountApprovalRepository
    {
        private readonly ERPDbContext _db;

        public DiscountApprovalRepository(ERPDbContext db)
        {
            _db = db;
        }

        public IQueryable<DiscountApproval> Query() =>
            _db.DiscountApprovals.Where(x => !x.IsDeleted);

        public async Task<IReadOnlyList<DiscountApproval>> GetAllAsync(
            DiscountApprovalListQueryDto? query,
            CancellationToken cancellationToken = default)
        {
            var q = ApplyFilters(Query().AsNoTracking(), query);
            return await q
                .OrderByDescending(x => x.RequestDate)
                .ThenByDescending(x => x.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<DiscountApproval?> GetByIdAsync(
            int id,
            bool includeDetails,
            bool asTracking,
            CancellationToken cancellationToken = default)
        {
            IQueryable<DiscountApproval> q = _db.DiscountApprovals.Where(x => !x.IsDeleted);
            if (includeDetails)
            {
                q = q.Include(x => x.History)
                    .Include(x => x.Comments);
            }

            if (!asTracking)
            {
                q = q.AsNoTracking();
            }

            return await q.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<DiscountApproval> CreateAsync(
            DiscountApproval entity,
            CancellationToken cancellationToken = default)
        {
            await _db.DiscountApprovals.AddAsync(entity, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task<DiscountApproval?> UpdateAsync(
            DiscountApproval entity,
            CancellationToken cancellationToken = default)
        {
            await _db.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task<bool> DeleteAsync(
            DiscountApproval entity,
            CancellationToken cancellationToken = default)
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<IReadOnlyList<DiscountApprovalHistory>?> GetHistoryAsync(
            int discountApprovalId,
            CancellationToken cancellationToken = default)
        {
            var exists = await Query().AnyAsync(x => x.Id == discountApprovalId, cancellationToken);
            if (!exists)
            {
                return null;
            }

            return await _db.DiscountApprovalHistories
                .AsNoTracking()
                .Where(x => x.DiscountApprovalId == discountApprovalId)
                .OrderBy(x => x.PerformedOn)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<DiscountApprovalComment>?> GetCommentsAsync(
            int discountApprovalId,
            CancellationToken cancellationToken = default)
        {
            var exists = await Query().AnyAsync(x => x.Id == discountApprovalId, cancellationToken);
            if (!exists)
            {
                return null;
            }

            return await _db.DiscountApprovalComments
                .AsNoTracking()
                .Where(x => x.DiscountApprovalId == discountApprovalId)
                .OrderBy(x => x.CommentedOn)
                .ToListAsync(cancellationToken);
        }

        public async Task<DiscountApprovalComment?> AddCommentAsync(
            DiscountApproval entity,
            DiscountApprovalComment comment,
            CancellationToken cancellationToken = default)
        {
            entity.Comments.Add(comment);
            await _db.SaveChangesAsync(cancellationToken);
            return comment;
        }

        public async Task<IReadOnlyList<DiscountApproval>> GetForStatisticsAsync(
            CancellationToken cancellationToken = default) =>
            await Query().AsNoTracking().ToListAsync(cancellationToken);

        public Task<SalesOrder?> FindSalesOrderAsync(
            int salesOrderId,
            CancellationToken cancellationToken = default) =>
            _db.SalesOrders
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == salesOrderId, cancellationToken);

        public Task<PriceList?> FindPriceListAsync(
            int priceListId,
            CancellationToken cancellationToken = default) =>
            _db.PriceLists
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == priceListId && !x.IsDeleted, cancellationToken);

        public async Task<IReadOnlyList<SalesOrder>> ListSalesOrdersForLookupAsync(
            CancellationToken cancellationToken = default) =>
            await _db.SalesOrders
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .Take(100)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<PriceList>> ListPriceListsForLookupAsync(
            CancellationToken cancellationToken = default) =>
            await _db.PriceLists
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.Id)
                .Take(100)
                .ToListAsync(cancellationToken);

        private static IQueryable<DiscountApproval> ApplyFilters(
            IQueryable<DiscountApproval> q,
            DiscountApprovalListQueryDto? query)
        {
            if (query is null)
            {
                return q;
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.Trim().ToLowerInvariant();
                q = q.Where(x =>
                    x.ApprovalNumber.ToLower().Contains(s)
                    || x.CustomerName.ToLower().Contains(s)
                    || x.SalesOrderNumber.ToLower().Contains(s)
                    || x.QuotationNumber.ToLower().Contains(s)
                    || x.Reason.ToLower().Contains(s));
            }

            var status = DiscountApprovalStatusRules.Normalize(query.Status);
            if (status is not null)
            {
                q = q.Where(x => x.Status == status);
            }

            var priority = DiscountApprovalPriorityRules.Normalize(query.Priority);
            if (priority is not null)
            {
                q = q.Where(x => x.Priority == priority);
            }

            var level = DiscountApprovalLevelRules.Normalize(query.ApprovalLevel);
            if (level is not null)
            {
                q = q.Where(x => x.ApprovalLevel == level);
            }

            var source = DiscountApprovalSourceTypeRules.Normalize(query.SourceType);
            if (source is not null)
            {
                q = q.Where(x => x.SourceType == source);
            }

            var category = PriceListCustomerCategoryRules.Normalize(query.CustomerCategory);
            if (category is not null)
            {
                q = q.Where(x => x.CustomerCategory == category);
            }

            if (query.SalesPersonUserId is int userId and > 0)
            {
                q = q.Where(x => x.SalesPersonUserId == userId);
            }

            if (DateOnly.TryParse(query.DateFrom, out var from))
            {
                q = q.Where(x => x.RequestDate >= from);
            }

            if (DateOnly.TryParse(query.DateTo, out var to))
            {
                q = q.Where(x => x.RequestDate <= to);
            }

            return q;
        }
    }
}
