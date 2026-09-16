using ERP.Application.Sales;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    public class AdvancePaymentNumberingService : IAdvancePaymentNumberingService
    {
        private const string DefaultPrefix = "ADV";
        private readonly ERPDbContext _db;

        public AdvancePaymentNumberingService(ERPDbContext db)
        {
            _db = db;
        }

        public async Task<string> GenerateNextPaymentNumberAsync(
            CancellationToken cancellationToken = default)
        {
            var year = DateTime.UtcNow.Year;
            var seq = await _db.AdvancePaymentDocumentSequences
                .FirstOrDefaultAsync(
                    x => x.FinancialYear == year && x.Prefix == DefaultPrefix,
                    cancellationToken);

            if (seq is null)
            {
                seq = new AdvancePaymentDocumentSequence
                {
                    FinancialYear = year,
                    Prefix = DefaultPrefix,
                    LastNumber = 0,
                    CreatedOn = DateTimeOffset.UtcNow
                };
                _db.AdvancePaymentDocumentSequences.Add(seq);
            }

            seq.LastNumber += 1;
            await _db.SaveChangesAsync(cancellationToken);
            return $"{DefaultPrefix}-{year}-{seq.LastNumber:D5}";
        }

        public async Task<string> GenerateNextReceiptVoucherNumberAsync(
            CancellationToken cancellationToken = default)
        {
            const string prefix = "RV";
            var year = DateTime.UtcNow.Year;
            var seq = await _db.AdvancePaymentDocumentSequences
                .FirstOrDefaultAsync(
                    x => x.FinancialYear == year && x.Prefix == prefix,
                    cancellationToken);

            if (seq is null)
            {
                seq = new AdvancePaymentDocumentSequence
                {
                    FinancialYear = year,
                    Prefix = prefix,
                    LastNumber = 0,
                    CreatedOn = DateTimeOffset.UtcNow
                };
                _db.AdvancePaymentDocumentSequences.Add(seq);
            }

            seq.LastNumber += 1;
            await _db.SaveChangesAsync(cancellationToken);
            return $"{prefix}-{year}-{seq.LastNumber:D5}";
        }

        public async Task<string> GenerateNextRefundVoucherNumberAsync(
            CancellationToken cancellationToken = default)
        {
            const string prefix = "RFV";
            var year = DateTime.UtcNow.Year;
            var seq = await _db.AdvancePaymentDocumentSequences
                .FirstOrDefaultAsync(
                    x => x.FinancialYear == year && x.Prefix == prefix,
                    cancellationToken);

            if (seq is null)
            {
                seq = new AdvancePaymentDocumentSequence
                {
                    FinancialYear = year,
                    Prefix = prefix,
                    LastNumber = 0,
                    CreatedOn = DateTimeOffset.UtcNow
                };
                _db.AdvancePaymentDocumentSequences.Add(seq);
            }

            seq.LastNumber += 1;
            await _db.SaveChangesAsync(cancellationToken);
            return $"{prefix}-{year}-{seq.LastNumber:D5}";
        }
    }
}
