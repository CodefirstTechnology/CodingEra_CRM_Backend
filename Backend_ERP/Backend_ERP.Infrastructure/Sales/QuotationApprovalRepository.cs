using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    public class QuotationApprovalRepository : IQuotationApprovalRepository
    {
        private readonly ERPDbContext _db;

        public QuotationApprovalRepository(ERPDbContext db)
        {
            _db = db;
        }

        public IQueryable<QuotationApproval> Query() =>
            _db.QuotationApprovals.Where(x => !x.IsDeleted);

        public async Task<IReadOnlyList<QuotationApproval>> GetAllAsync(
            QuotationApprovalListQueryDto? query,
            CancellationToken cancellationToken = default)
        {
            var q = ApplyFilters(Query().AsNoTracking(), query);
            return await q
                .OrderByDescending(x => x.RequestDate)
                .ThenByDescending(x => x.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<QuotationApproval?> GetByIdAsync(
            int id,
            bool includeDetails,
            bool asTracking,
            CancellationToken cancellationToken = default)
        {
            IQueryable<QuotationApproval> q = _db.QuotationApprovals.Where(x => !x.IsDeleted);
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

        public async Task<QuotationApproval> CreateAsync(
            QuotationApproval entity,
            CancellationToken cancellationToken = default)
        {
            await _db.QuotationApprovals.AddAsync(entity, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task<QuotationApproval?> UpdateAsync(
            QuotationApproval entity,
            CancellationToken cancellationToken = default)
        {
            await _db.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task<bool> DeleteAsync(
            QuotationApproval entity,
            CancellationToken cancellationToken = default)
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<IReadOnlyList<QuotationApprovalHistory>?> GetHistoryAsync(
            int quotationApprovalId,
            CancellationToken cancellationToken = default)
        {
            var exists = await Query().AnyAsync(x => x.Id == quotationApprovalId, cancellationToken);
            if (!exists)
            {
                return null;
            }

            return await _db.QuotationApprovalHistories
                .AsNoTracking()
                .Where(x => x.QuotationApprovalId == quotationApprovalId)
                .OrderBy(x => x.PerformedOn)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<QuotationApprovalComment>?> GetCommentsAsync(
            int quotationApprovalId,
            CancellationToken cancellationToken = default)
        {
            var exists = await Query().AnyAsync(x => x.Id == quotationApprovalId, cancellationToken);
            if (!exists)
            {
                return null;
            }

            return await _db.QuotationApprovalComments
                .AsNoTracking()
                .Where(x => x.QuotationApprovalId == quotationApprovalId)
                .OrderBy(x => x.CommentedOn)
                .ToListAsync(cancellationToken);
        }

        public async Task<QuotationApprovalComment?> AddCommentAsync(
            QuotationApproval entity,
            QuotationApprovalComment comment,
            CancellationToken cancellationToken = default)
        {
            entity.Comments.Add(comment);
            await _db.SaveChangesAsync(cancellationToken);
            return comment;
        }

        public async Task<IReadOnlyList<QuotationApproval>> GetForStatisticsAsync(
            CancellationToken cancellationToken = default) =>
            await Query().AsNoTracking().ToListAsync(cancellationToken);

        public Task<SalesOrder?> FindSalesOrderAsync(
            int salesOrderId,
            CancellationToken cancellationToken = default) =>
            _db.SalesOrders
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == salesOrderId, cancellationToken);

        private static IQueryable<QuotationApproval> ApplyFilters(
            IQueryable<QuotationApproval> q,
            QuotationApprovalListQueryDto? query)
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
                    || x.QuotationNumber.ToLower().Contains(s)
                    || x.SalesOrderNumber.ToLower().Contains(s)
                    || x.Reason.ToLower().Contains(s));
            }

            var status = QuotationApprovalStatusRules.Normalize(query.Status);
            if (status is not null)
            {
                q = q.Where(x => x.Status == status);
            }

            var priority = QuotationApprovalPriorityRules.Normalize(query.Priority);
            if (priority is not null)
            {
                q = q.Where(x => x.Priority == priority);
            }

            var level = QuotationApprovalLevelRules.Normalize(query.ApprovalLevel);
            if (level is not null)
            {
                q = q.Where(x => x.ApprovalLevel == level);
            }

            if (query.SalesPersonUserId is int userId && userId > 0)
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
