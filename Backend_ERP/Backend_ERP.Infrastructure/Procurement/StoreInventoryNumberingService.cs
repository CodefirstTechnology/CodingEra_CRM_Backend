using System;
using System.Threading;
using System.Threading.Tasks;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Procurement
{
    public class StoreInventoryNumberingService
    {
        private readonly ERPDbContext _dbContext;

        public StoreInventoryNumberingService(ERPDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<string> GenerateNumberAsync(string prefix, CancellationToken cancellationToken = default)
        {
            var year = DateTime.UtcNow.Year;
            var fullPrefix = $"{prefix.ToUpperInvariant()}-{year}";

            var sequence = await _dbContext.StoreInventoryDocumentSequences
                .FirstOrDefaultAsync(x => x.Prefix == fullPrefix, cancellationToken);

            if (sequence is null)
            {
                sequence = new StoreInventoryDocumentSequence
                {
                    Prefix = fullPrefix,
                    LastSequence = 0
                };
                _dbContext.StoreInventoryDocumentSequences.Add(sequence);
            }

            sequence.LastSequence++;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return $"{fullPrefix}-{sequence.LastSequence:D6}";
        }
    }
}
