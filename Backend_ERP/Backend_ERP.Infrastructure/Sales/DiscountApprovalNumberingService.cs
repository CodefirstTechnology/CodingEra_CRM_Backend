using ERP.Application.Sales;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    public class DiscountApprovalNumberingService : IDiscountApprovalNumberingService
    {
        private const string DefaultPrefix = "DA";
        private readonly ERPDbContext _db;

        public DiscountApprovalNumberingService(ERPDbContext db)
        {
            _db = db;
        }

        public async Task<string> GenerateNextApprovalNumberAsync(
            CancellationToken cancellationToken = default)
        {
            var year = DateTime.UtcNow.Year;
            var seq = await _db.DiscountApprovalDocumentSequences
                .FirstOrDefaultAsync(
                    x => x.FinancialYear == year && x.Prefix == DefaultPrefix,
                    cancellationToken);

            if (seq is null)
            {
                seq = new DiscountApprovalDocumentSequence
                {
                    FinancialYear = year,
                    Prefix = DefaultPrefix,
                    LastNumber = 0,
                    CreatedOn = DateTimeOffset.UtcNow
                };
                _db.DiscountApprovalDocumentSequences.Add(seq);
            }

            seq.LastNumber += 1;
            await _db.SaveChangesAsync(cancellationToken);
            return $"{DefaultPrefix}-{year}-{seq.LastNumber:D5}";
        }
    }
}
