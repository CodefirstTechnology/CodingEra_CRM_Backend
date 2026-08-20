using System;
using System.Threading;
using System.Threading.Tasks;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    public class PodNumberingService
    {
        private readonly ERPDbContext _db;

        public PodNumberingService(ERPDbContext db)
        {
            _db = db;
        }

        public async Task<string> GenerateNextNumberAsync(CancellationToken cancellationToken = default)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"POD-{year}";

            var sequence = await _db.PodDocumentSequences
                .FirstOrDefaultAsync(s => s.Prefix == prefix, cancellationToken);

            if (sequence == null)
            {
                sequence = new PodDocumentSequence
                {
                    Prefix = prefix,
                    LastSequence = 0
                };
                _db.PodDocumentSequences.Add(sequence);
            }

            sequence.LastSequence++;
            await _db.SaveChangesAsync(cancellationToken);

            return $"{prefix}-{sequence.LastSequence:D4}";
        }
    }
}
