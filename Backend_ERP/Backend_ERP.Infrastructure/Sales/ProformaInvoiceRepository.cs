using ERP.Application.Sales;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    public class ProformaInvoiceRepository : IProformaInvoiceRepository
    {
        private readonly ERPDbContext _db;

        public ProformaInvoiceRepository(ERPDbContext db)
        {
            _db = db;
        }

        public Task<bool> AnyAsync(CancellationToken cancellationToken = default) =>
            _db.ProformaInvoices.AnyAsync(x => !x.IsDeleted, cancellationToken);

        public IQueryable<ProformaInvoice> Query() =>
            _db.ProformaInvoices.Where(x => !x.IsDeleted);

        public async Task<ProformaInvoice?> GetByIdAsync(
            int id,
            bool includeDetails,
            bool asTracking,
            CancellationToken cancellationToken = default)
        {
            IQueryable<ProformaInvoice> q = _db.ProformaInvoices.Where(x => !x.IsDeleted);
            if (includeDetails)
            {
                q = q.Include(x => x.Items)
                    .Include(x => x.StatusHistory)
                    .Include(x => x.ApprovalHistory);
            }

            if (!asTracking)
            {
                q = q.AsNoTracking();
            }

            return await q.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task AddAsync(ProformaInvoice entity, CancellationToken cancellationToken = default) =>
            await _db.ProformaInvoices.AddAsync(entity, cancellationToken);

        public void Remove(ProformaInvoice entity) => _db.ProformaInvoices.Remove(entity);

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            _db.SaveChangesAsync(cancellationToken);

        public async Task<string> ReserveNextPiNumberAsync(CancellationToken cancellationToken = default)
        {
            var year = DateTime.UtcNow.Year;
            var seq = await _db.ProformaInvoiceDocumentSequences
                .FirstOrDefaultAsync(x => x.Year == year, cancellationToken);

            if (seq is null)
            {
                seq = new ProformaInvoiceDocumentSequence { Year = year, LastSequence = 0 };
                _db.ProformaInvoiceDocumentSequences.Add(seq);
            }

            seq.LastSequence += 1;
            await _db.SaveChangesAsync(cancellationToken);
            return $"PI-{year}-{seq.LastSequence:D5}";
        }

        public Task<bool> PiNumberExistsAsync(
            string piNumber,
            int? excludeId,
            CancellationToken cancellationToken = default)
        {
            var q = _db.ProformaInvoices.Where(x => x.PiNumber == piNumber);
            if (excludeId is int id)
            {
                q = q.Where(x => x.Id != id);
            }

            return q.AnyAsync(cancellationToken);
        }

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
    }
}
