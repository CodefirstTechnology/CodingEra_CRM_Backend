using System;
using System.Threading;
using System.Threading.Tasks;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    public class LrNumberingService
    {
        private readonly ERPDbContext _db;

        public LrNumberingService(ERPDbContext db)
        {
            _db = db;
        }

        public async Task<string> NextNumberAsync(CancellationToken cancellationToken = default)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"LR-{year}";
            var seq = await _db.LrDocumentSequences.FirstOrDefaultAsync(x => x.Prefix == prefix, cancellationToken);
            if (seq is null)
            {
                seq = new LrDocumentSequence { Prefix = prefix, LastSequence = 0 };
                _db.LrDocumentSequences.Add(seq);
            }

            seq.LastSequence += 1;
            await _db.SaveChangesAsync(cancellationToken);
            return $"{prefix}-{seq.LastSequence:D4}";
        }
    }
}
