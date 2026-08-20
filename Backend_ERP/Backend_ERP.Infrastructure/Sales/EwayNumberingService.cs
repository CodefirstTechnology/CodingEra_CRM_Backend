using System;
using System.Threading;
using System.Threading.Tasks;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    public class EwayNumberingService
    {
        private readonly ERPDbContext _db;

        public EwayNumberingService(ERPDbContext db)
        {
            _db = db;
        }

        public async Task<string> NextNumberAsync(CancellationToken cancellationToken = default)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"EWB-{year}";
            var seq = await _db.EwayDocumentSequences.FirstOrDefaultAsync(x => x.Prefix == prefix, cancellationToken);
            if (seq is null)
            {
                seq = new EwayDocumentSequence { Prefix = prefix, LastSequence = 0 };
                _db.EwayDocumentSequences.Add(seq);
            }

            seq.LastSequence += 1;
            await _db.SaveChangesAsync(cancellationToken);
            return $"{prefix}-{seq.LastSequence:D4}";
        }
    }
}
