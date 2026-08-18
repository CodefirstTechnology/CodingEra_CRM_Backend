using System;
using System.Threading;
using System.Threading.Tasks;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Procurement
{
    public class PurchaseBillNumberingService
    {
        private readonly ERPDbContext _dbContext;

        public PurchaseBillNumberingService(ERPDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<string> GenerateNumberAsync(CancellationToken cancellationToken = default)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"BILL-{year}";

            var sequence = await _dbContext.PurchaseBillDocumentSequences
                .FirstOrDefaultAsync(x => x.Prefix == prefix, cancellationToken);

            if (sequence is null)
            {
                sequence = new PurchaseBillDocumentSequence
                {
                    Prefix = prefix,
                    LastSequence = 0
                };
                _dbContext.PurchaseBillDocumentSequences.Add(sequence);
            }

            sequence.LastSequence++;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return $"{prefix}-{sequence.LastSequence:D6}";
        }
    }
}
