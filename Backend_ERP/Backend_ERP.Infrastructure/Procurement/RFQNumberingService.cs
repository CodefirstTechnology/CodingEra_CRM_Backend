using System;
using System.Threading;
using System.Threading.Tasks;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Procurement
{
    public class RFQNumberingService
    {
        private readonly ERPDbContext _db;

        public RFQNumberingService(ERPDbContext db)
        {
            _db = db;
        }

        public async Task<string> NextRFQNumberAsync(CancellationToken cancellationToken = default)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"RFQ-{year}";
            var seq = await _db.RFQDocumentSequences.FirstOrDefaultAsync(x => x.Prefix == prefix, cancellationToken);
            if (seq is null)
            {
                seq = new RFQDocumentSequence { Prefix = prefix, LastSequence = 0 };
                _db.RFQDocumentSequences.Add(seq);
            }

            seq.LastSequence += 1;
            await _db.SaveChangesAsync(cancellationToken);
            return $"{prefix}-{seq.LastSequence:D6}";
        }
    }
}
