using System;
using System.Threading;
using System.Threading.Tasks;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    public class VehicleAssignmentNumberingService
    {
        private readonly ERPDbContext _db;

        public VehicleAssignmentNumberingService(ERPDbContext db)
        {
            _db = db;
        }

        public async Task<string> NextNumberAsync(CancellationToken cancellationToken = default)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"VA-{year}";
            var seq = await _db.VehicleAssignmentDocumentSequences.FirstOrDefaultAsync(x => x.Prefix == prefix, cancellationToken);
            if (seq is null)
            {
                seq = new VehicleAssignmentDocumentSequence { Prefix = prefix, LastSequence = 0 };
                _db.VehicleAssignmentDocumentSequences.Add(seq);
            }

            seq.LastSequence += 1;
            await _db.SaveChangesAsync(cancellationToken);
            return $"{prefix}-{seq.LastSequence:D4}";
        }
    }

    public class TransportNumberingService
    {
        private readonly ERPDbContext _db;

        public TransportNumberingService(ERPDbContext db)
        {
            _db = db;
        }

        public async Task<string> NextNumberAsync(CancellationToken cancellationToken = default)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"TR-{year}";
            var seq = await _db.TransportDocumentSequences.FirstOrDefaultAsync(x => x.Prefix == prefix, cancellationToken);
            if (seq is null)
            {
                seq = new TransportDocumentSequence { Prefix = prefix, LastSequence = 0 };
                _db.TransportDocumentSequences.Add(seq);
            }

            seq.LastSequence += 1;
            await _db.SaveChangesAsync(cancellationToken);
            return $"{prefix}-{seq.LastSequence:D4}";
        }
    }
}
