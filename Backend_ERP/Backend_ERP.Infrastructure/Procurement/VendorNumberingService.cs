using System.Threading;
using System.Threading.Tasks;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Procurement
{
    public class VendorNumberingService
    {
        private readonly ERPDbContext _db;

        public VendorNumberingService(ERPDbContext db)
        {
            _db = db;
        }

        public async Task<string> NextVendorCodeAsync(CancellationToken cancellationToken = default)
        {
            var seq = await _db.VendorDocumentSequences.FirstOrDefaultAsync(x => x.Prefix == "VEN", cancellationToken);
            if (seq is null)
            {
                seq = new VendorDocumentSequence { Prefix = "VEN", LastSequence = 0 };
                _db.VendorDocumentSequences.Add(seq);
            }

            seq.LastSequence += 1;
            await _db.SaveChangesAsync(cancellationToken);
            return $"VEN-{seq.LastSequence:D6}";
        }
    }
}
