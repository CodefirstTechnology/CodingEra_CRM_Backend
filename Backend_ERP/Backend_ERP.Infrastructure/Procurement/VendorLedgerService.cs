using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Procurement
{
    public class VendorLedgerService : IVendorLedgerService
    {
        private readonly ERPDbContext _dbContext;

        public VendorLedgerService(ERPDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<VendorLedgerStatementResultDto> GetVendorLedgerStatementAsync(int vendorId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
        {
            var vendor = await _dbContext.Vendors
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == vendorId, cancellationToken);

            var vendorName = vendor?.Name ?? $"Vendor #{vendorId}";

            var fromUtc = fromDate.ToUniversalTime();
            var toUtc = toDate.ToUniversalTime();

            // Calculate Opening Balance prior to fromUtc
            var priorEntries = await _dbContext.VendorLedgerEntries
                .Where(e => e.VendorId == vendorId && e.EntryDate < fromUtc)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var openingBalance = priorEntries.Sum(e => e.CreditAmount - e.DebitAmount);

            // Fetch entries in date range
            var entries = await _dbContext.VendorLedgerEntries
                .Where(e => e.VendorId == vendorId && e.EntryDate >= fromUtc && e.EntryDate <= toUtc)
                .OrderBy(e => e.EntryDate)
                .ThenBy(e => e.Id)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            decimal runningBal = openingBalance;
            decimal totalDebit = 0m;
            decimal totalCredit = 0m;

            var statementItems = new List<VendorLedgerStatementItemDto>();

            foreach (var entry in entries)
            {
                runningBal += (entry.CreditAmount - entry.DebitAmount);
                totalDebit += entry.DebitAmount;
                totalCredit += entry.CreditAmount;

                statementItems.Add(new VendorLedgerStatementItemDto
                {
                    Id = entry.Id,
                    EntryDate = entry.EntryDate,
                    VoucherNumber = entry.VoucherNumber,
                    EntryType = entry.EntryType,
                    ReferenceNumber = entry.ReferenceNumber,
                    DebitAmount = entry.DebitAmount,
                    CreditAmount = entry.CreditAmount,
                    RunningBalance = runningBal,
                    Narration = entry.Narration
                });
            }

            return new VendorLedgerStatementResultDto
            {
                VendorId = vendorId,
                VendorName = vendorName,
                OpeningBalance = openingBalance,
                TotalDebit = totalDebit,
                TotalCredit = totalCredit,
                NetBalance = openingBalance + totalCredit - totalDebit,
                Entries = statementItems
            };
        }

        public async Task<List<VendorAgingSummaryDto>> GetVendorAgingSummaryAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.VendorAgingSummaries
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
    }
}
