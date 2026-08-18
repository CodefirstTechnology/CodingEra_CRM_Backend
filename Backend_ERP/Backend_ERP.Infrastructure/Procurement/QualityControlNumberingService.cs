using System;
using System.Threading;
using System.Threading.Tasks;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Procurement
{
    public class QualityControlNumberingService
    {
        private readonly ERPDbContext _db;

        public QualityControlNumberingService(ERPDbContext db)
        {
            _db = db;
        }

        public async Task<string> NextNumberAsync(string typePrefix, CancellationToken cancellationToken = default)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"{typePrefix}-{year}";
            var seq = await _db.QualityControlDocumentSequences.FirstOrDefaultAsync(x => x.Prefix == prefix, cancellationToken);
            if (seq is null)
            {
                seq = new QualityControlDocumentSequence { Prefix = prefix, LastSequence = 0 };
                _db.QualityControlDocumentSequences.Add(seq);
            }

            seq.LastSequence += 1;
            await _db.SaveChangesAsync(cancellationToken);
            return $"{prefix}-{seq.LastSequence:D6}";
        }
    }
}
