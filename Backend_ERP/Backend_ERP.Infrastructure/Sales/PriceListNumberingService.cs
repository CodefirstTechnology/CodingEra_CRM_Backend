using ERP.Application.Sales;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    public class PriceListNumberingService : IPriceListNumberingService
    {
        private const string DefaultPrefix = "PL";
        private readonly ERPDbContext _db;

        public PriceListNumberingService(ERPDbContext db)
        {
            _db = db;
        }

        public async Task<string> GenerateNextPriceListNumberAsync(
            CancellationToken cancellationToken = default)
        {
            var year = DateTime.UtcNow.Year;
            var seq = await _db.PriceListDocumentSequences
                .FirstOrDefaultAsync(
                    x => x.FinancialYear == year && x.Prefix == DefaultPrefix,
                    cancellationToken);

            if (seq is null)
            {
                seq = new PriceListDocumentSequence
                {
                    FinancialYear = year,
                    Prefix = DefaultPrefix,
                    LastNumber = 0,
                    CreatedOn = DateTimeOffset.UtcNow
                };
                _db.PriceListDocumentSequences.Add(seq);
            }

            seq.LastNumber += 1;
            await _db.SaveChangesAsync(cancellationToken);
            return $"{DefaultPrefix}-{year}-{seq.LastNumber:D5}";
        }
    }
}
