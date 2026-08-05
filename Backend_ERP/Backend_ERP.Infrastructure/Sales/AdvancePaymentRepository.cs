using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    public class AdvancePaymentRepository : IAdvancePaymentRepository
    {
        private readonly ERPDbContext _db;

        public AdvancePaymentRepository(ERPDbContext db)
        {
            _db = db;
        }

        public IQueryable<AdvancePayment> Query() =>
            _db.AdvancePayments.Where(x => !x.IsDeleted);

        public async Task<IReadOnlyList<AdvancePayment>> GetAllAsync(
            AdvancePaymentListQueryDto? query,
            CancellationToken cancellationToken = default)
        {
            var q = ApplyFilters(Query().AsNoTracking(), query);
            return await q
                .OrderByDescending(x => x.PaymentDate)
                .ThenByDescending(x => x.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<AdvancePayment?> GetByIdAsync(
            int id,
            bool includeDetails,
            bool asTracking,
            CancellationToken cancellationToken = default)
        {
            IQueryable<AdvancePayment> q = _db.AdvancePayments.Where(x => !x.IsDeleted);
            if (includeDetails)
            {
                q = q.Include(x => x.Applications)
                    .Include(x => x.Timeline);
            }

            if (!asTracking)
            {
                q = q.AsNoTracking();
            }

            return await q.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<AdvancePayment> CreateAsync(
            AdvancePayment entity,
            CancellationToken cancellationToken = default)
        {
            await _db.AdvancePayments.AddAsync(entity, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task<AdvancePayment?> UpdateAsync(
            AdvancePayment entity,
            CancellationToken cancellationToken = default)
        {
            await _db.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task<bool> DeleteAsync(
            AdvancePayment entity,
            CancellationToken cancellationToken = default)
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<AdvancePayment?> ApplyAsync(
            AdvancePayment entity,
            AdvancePaymentApplication application,
            CancellationToken cancellationToken = default)
        {
            entity.Applications.Add(application);
            await _db.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task<IReadOnlyList<AdvancePaymentTimeline>?> GetTimelineAsync(
            int advancePaymentId,
            CancellationToken cancellationToken = default)
        {
            var exists = await Query().AnyAsync(x => x.Id == advancePaymentId, cancellationToken);
            if (!exists)
            {
                return null;
            }

            return await _db.AdvancePaymentTimelines
                .AsNoTracking()
                .Where(x => x.AdvancePaymentId == advancePaymentId)
                .OrderBy(x => x.PerformedOn)
                .ToListAsync(cancellationToken);
        }

        public Task<IReadOnlyList<AdvancePayment>> GetLedgerAsync(
            AdvancePaymentListQueryDto? query,
            CancellationToken cancellationToken = default) =>
            GetAllAsync(query, cancellationToken);

        public async Task<IReadOnlyList<AdvancePayment>> GetDashboardAsync(
            CancellationToken cancellationToken = default) =>
            await Query().AsNoTracking().ToListAsync(cancellationToken);

        public Task<bool> PaymentNumberExistsAsync(
            string paymentNumber,
            int? excludeId,
            CancellationToken cancellationToken = default)
        {
            var q = _db.AdvancePayments.Where(x => x.PaymentNumber == paymentNumber);
            if (excludeId is int id)
            {
                q = q.Where(x => x.Id != id);
            }

            return q.AnyAsync(cancellationToken);
        }

        public Task<bool> ApplicationExistsAsync(
            int advancePaymentId,
            int salesOrderId,
            CancellationToken cancellationToken = default) =>
            _db.AdvancePaymentApplications.AnyAsync(
                x => x.AdvancePaymentId == advancePaymentId && x.SalesOrderId == salesOrderId,
                cancellationToken);

        public Task<SalesOrder?> FindSalesOrderAsync(
            int salesOrderId,
            CancellationToken cancellationToken = default) =>
            _db.SalesOrders.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == salesOrderId, cancellationToken);

        public async Task<IReadOnlyList<SalesOrder>> ListSalesOrdersForLookupAsync(
            CancellationToken cancellationToken = default)
        {
            return await _db.SalesOrders.AsNoTracking()
                .OrderByDescending(x => x.Id)
                .Take(100)
                .ToListAsync(cancellationToken);
        }

        private static IQueryable<AdvancePayment> ApplyFilters(
            IQueryable<AdvancePayment> q,
            AdvancePaymentListQueryDto? query)
        {
            if (query is null)
            {
                return q;
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                var status = AdvancePaymentStatusRules.Normalize(query.Status) ?? query.Status.Trim();
                q = q.Where(x => x.Status == status);
            }

            var from = AdvancePaymentMapper.ParseOptionalDate(query.DateFrom);
            if (from is not null)
            {
                q = q.Where(x => x.PaymentDate >= from);
            }

            var to = AdvancePaymentMapper.ParseOptionalDate(query.DateTo);
            if (to is not null)
            {
                q = q.Where(x => x.PaymentDate <= to);
            }

            if (!string.IsNullOrWhiteSpace(query.Customer))
            {
                var c = query.Customer.Trim().ToLower();
                q = q.Where(x => x.CustomerName.ToLower().Contains(c));
            }

            if (!string.IsNullOrWhiteSpace(query.PaymentMode))
            {
                var mode = AdvancePaymentModeRules.Normalize(query.PaymentMode) ?? query.PaymentMode.Trim();
                q = q.Where(x => x.PaymentMode == mode);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim().ToLower();
                q = q.Where(x =>
                    x.PaymentNumber.ToLower().Contains(term)
                    || x.CustomerName.ToLower().Contains(term)
                    || x.ReferenceNumber.ToLower().Contains(term)
                    || x.SalesOrderNumber.ToLower().Contains(term)
                    || x.QuotationNumber.ToLower().Contains(term));
            }

            return q;
        }
    }
}
