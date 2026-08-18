using System;
using System.Threading;
using System.Threading.Tasks;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Procurement
{
    public class GoodsReceiptNumberingService
    {
        private readonly ERPDbContext _db;

        public GoodsReceiptNumberingService(ERPDbContext db)
        {
            _db = db;
        }

        public async Task<string> NextGRNNumberAsync(CancellationToken cancellationToken = default)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"GRN-{year}";
            var seq = await _db.GoodsReceiptDocumentSequences.FirstOrDefaultAsync(x => x.Prefix == prefix, cancellationToken);
            if (seq is null)
            {
                seq = new GoodsReceiptDocumentSequence { Prefix = prefix, LastSequence = 0 };
                _db.GoodsReceiptDocumentSequences.Add(seq);
            }

            seq.LastSequence += 1;
            await _db.SaveChangesAsync(cancellationToken);
            return $"{prefix}-{seq.LastSequence:D6}";
        }
    }
}
