using ERP.Application.Sales;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    public class SalesTargetNumberingService : ISalesTargetNumberingService
    {
        private const string DefaultPrefix = "TGT";
        private readonly ERPDbContext _db;

        public SalesTargetNumberingService(ERPDbContext db)
        {
            _db = db;
        }

        public async Task<string> GenerateNextTargetNumberAsync(
            CancellationToken cancellationToken = default)
        {
            var year = DateTime.UtcNow.Year;
            var seq = await _db.SalesTargetDocumentSequences
                .FirstOrDefaultAsync(
                    x => x.FinancialYear == year && x.Prefix == DefaultPrefix,
                    cancellationToken);

            if (seq is null)
            {
                seq = new SalesTargetDocumentSequence
                {
                    FinancialYear = year,
                    Prefix = DefaultPrefix,
                    LastNumber = 0,
                    CreatedOn = DateTimeOffset.UtcNow
                };
                _db.SalesTargetDocumentSequences.Add(seq);
            }

            seq.LastNumber += 1;
            await _db.SaveChangesAsync(cancellationToken);
            return $"{DefaultPrefix}-{year}-{seq.LastNumber:D5}";
        }
    }
}
