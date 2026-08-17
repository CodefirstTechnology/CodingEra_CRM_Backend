using System;
using System.Threading;
using System.Threading.Tasks;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Procurement
{
    public class VendorComparisonNumberingService
    {
        private readonly ERPDbContext _db;

        public VendorComparisonNumberingService(ERPDbContext db)
        {
            _db = db;
        }

        public async Task<string> NextComparisonNumberAsync(CancellationToken cancellationToken = default)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"VC-{year}";
            var seq = await _db.VendorComparisonDocumentSequences.FirstOrDefaultAsync(x => x.Prefix == prefix, cancellationToken);
            if (seq is null)
            {
                seq = new VendorComparisonDocumentSequence { Prefix = prefix, LastSequence = 0 };
                _db.VendorComparisonDocumentSequences.Add(seq);
            }

            seq.LastSequence += 1;
            await _db.SaveChangesAsync(cancellationToken);
            return $"{prefix}-{seq.LastSequence:D6}";
        }
    }
}
