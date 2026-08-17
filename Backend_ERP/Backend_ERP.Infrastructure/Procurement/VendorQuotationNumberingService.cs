using System;
using System.Threading;
using System.Threading.Tasks;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Procurement
{
    public class VendorQuotationNumberingService
    {
        private readonly ERPDbContext _db;

        public VendorQuotationNumberingService(ERPDbContext db)
        {
            _db = db;
        }

        public async Task<string> NextQuotationNumberAsync(CancellationToken cancellationToken = default)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"VQ-{year}";
            var seq = await _db.VendorQuotationDocumentSequences.FirstOrDefaultAsync(x => x.Prefix == prefix, cancellationToken);
            if (seq is null)
            {
                seq = new VendorQuotationDocumentSequence { Prefix = prefix, LastSequence = 0 };
                _db.VendorQuotationDocumentSequences.Add(seq);
            }

            seq.LastSequence += 1;
            await _db.SaveChangesAsync(cancellationToken);
            return $"{prefix}-{seq.LastSequence:D6}";
        }
    }
}
