using System;
using System.Threading;
using System.Threading.Tasks;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    public class DispatchPlanningNumberingService
    {
        private readonly ERPDbContext _db;

        public DispatchPlanningNumberingService(ERPDbContext db)
        {
            _db = db;
        }

        public async Task<string> NextNumberAsync(CancellationToken cancellationToken = default)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"DN-{year}";
            var seq = await _db.DispatchDocumentSequences.FirstOrDefaultAsync(x => x.Prefix == prefix, cancellationToken);
            if (seq is null)
            {
                seq = new DispatchDocumentSequence { Prefix = prefix, LastSequence = 0 };
                _db.DispatchDocumentSequences.Add(seq);
            }

            seq.LastSequence += 1;
            await _db.SaveChangesAsync(cancellationToken);
            return $"{prefix}-{seq.LastSequence:D4}";
        }
    }
}
