using System;
using System.Threading;
using System.Threading.Tasks;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Procurement
{
    public class PurchaseOrderNumberingService
    {
        private readonly ERPDbContext _db;

        public PurchaseOrderNumberingService(ERPDbContext db)
        {
            _db = db;
        }

        public async Task<string> NextPurchaseOrderNumberAsync(CancellationToken cancellationToken = default)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"PO-{year}";
            var seq = await _db.PurchaseOrderDocumentSequences.FirstOrDefaultAsync(x => x.Prefix == prefix, cancellationToken);
            if (seq is null)
            {
                seq = new PurchaseOrderDocumentSequence { Prefix = prefix, LastSequence = 0 };
                _db.PurchaseOrderDocumentSequences.Add(seq);
            }

            seq.LastSequence += 1;
            await _db.SaveChangesAsync(cancellationToken);
            return $"{prefix}-{seq.LastSequence:D6}";
        }
    }
}
