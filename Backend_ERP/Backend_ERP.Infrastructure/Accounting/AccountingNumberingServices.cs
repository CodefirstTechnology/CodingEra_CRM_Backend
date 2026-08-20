using System;
using System.Threading;
using System.Threading.Tasks;
using ERP.Domain.Accounting;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Accounting
{
    public class CustomerLedgerNumberingService
    {
        private readonly ERPDbContext _dbContext;
        public CustomerLedgerNumberingService(ERPDbContext dbContext) => _dbContext = dbContext;

        public async Task<string> GenerateNumberAsync(CancellationToken cancellationToken = default)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"CL-{year}";
            var sequence = await _dbContext.CustomerLedgerDocumentSequences
                .FirstOrDefaultAsync(x => x.Prefix == prefix, cancellationToken);

            if (sequence is null)
            {
                sequence = new CustomerLedgerDocumentSequence { Prefix = prefix, LastSequence = 0 };
                _dbContext.CustomerLedgerDocumentSequences.Add(sequence);
            }

            sequence.LastSequence++;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return $"{prefix}-{sequence.LastSequence:D3}";
        }
    }

    public class GstNumberingService
    {
        private readonly ERPDbContext _dbContext;
        public GstNumberingService(ERPDbContext dbContext) => _dbContext = dbContext;

        public async Task<string> GenerateNumberAsync(CancellationToken cancellationToken = default)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"GST-{year}";
            var sequence = await _dbContext.GstDocumentSequences
                .FirstOrDefaultAsync(x => x.Prefix == prefix, cancellationToken);

            if (sequence is null)
            {
                sequence = new GstDocumentSequence { Prefix = prefix, LastSequence = 0 };
                _dbContext.GstDocumentSequences.Add(sequence);
            }

            sequence.LastSequence++;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return $"{prefix}-{sequence.LastSequence:D3}";
        }
    }

    public class PaymentNumberingService
    {
        private readonly ERPDbContext _dbContext;
        public PaymentNumberingService(ERPDbContext dbContext) => _dbContext = dbContext;

        public async Task<string> GenerateNumberAsync(CancellationToken cancellationToken = default)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"PAY-{year}";
            var sequence = await _dbContext.PaymentDocumentSequences
                .FirstOrDefaultAsync(x => x.Prefix == prefix, cancellationToken);

            if (sequence is null)
            {
                sequence = new PaymentDocumentSequence { Prefix = prefix, LastSequence = 0 };
                _dbContext.PaymentDocumentSequences.Add(sequence);
            }

            sequence.LastSequence++;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return $"{prefix}-{sequence.LastSequence:D3}";
        }
    }

    public class ReceiptNumberingService
    {
        private readonly ERPDbContext _dbContext;
        public ReceiptNumberingService(ERPDbContext dbContext) => _dbContext = dbContext;

        public async Task<string> GenerateNumberAsync(CancellationToken cancellationToken = default)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"REC-{year}";
            var sequence = await _dbContext.ReceiptDocumentSequences
                .FirstOrDefaultAsync(x => x.Prefix == prefix, cancellationToken);

            if (sequence is null)
            {
                sequence = new ReceiptDocumentSequence { Prefix = prefix, LastSequence = 0 };
                _dbContext.ReceiptDocumentSequences.Add(sequence);
            }

            sequence.LastSequence++;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return $"{prefix}-{sequence.LastSequence:D3}";
        }
    }

    public class BankReconNumberingService
    {
        private readonly ERPDbContext _dbContext;
        public BankReconNumberingService(ERPDbContext dbContext) => _dbContext = dbContext;

        public async Task<string> GenerateNumberAsync(CancellationToken cancellationToken = default)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"BRS-{year}";
            var sequence = await _dbContext.BankReconDocumentSequences
                .FirstOrDefaultAsync(x => x.Prefix == prefix, cancellationToken);

            if (sequence is null)
            {
                sequence = new BankReconDocumentSequence { Prefix = prefix, LastSequence = 0 };
                _dbContext.BankReconDocumentSequences.Add(sequence);
            }

            sequence.LastSequence++;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return $"{prefix}-{sequence.LastSequence:D3}";
        }
    }
}
