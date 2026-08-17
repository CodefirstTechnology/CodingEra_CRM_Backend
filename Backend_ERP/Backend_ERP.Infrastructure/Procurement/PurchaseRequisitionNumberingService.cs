using System;
using System.Threading;
using System.Threading.Tasks;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Procurement
{
    public class PurchaseRequisitionNumberingService
    {
        private readonly ERPDbContext _db;

        public PurchaseRequisitionNumberingService(ERPDbContext db)
        {
            _db = db;
        }

        public async Task<string> NextPRNumberAsync(CancellationToken cancellationToken = default)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"PR-{year}";
            var seq = await _db.PurchaseRequisitionDocumentSequences.FirstOrDefaultAsync(x => x.Prefix == prefix, cancellationToken);
            if (seq is null)
            {
                seq = new PurchaseRequisitionDocumentSequence { Prefix = prefix, LastSequence = 0 };
                _db.PurchaseRequisitionDocumentSequences.Add(seq);
            }

            seq.LastSequence += 1;
            await _db.SaveChangesAsync(cancellationToken);
            return $"{prefix}-{seq.LastSequence:D6}";
        }
    }
}
