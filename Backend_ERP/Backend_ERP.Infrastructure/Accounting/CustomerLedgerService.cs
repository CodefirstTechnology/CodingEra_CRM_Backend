using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Accounting;
using ERP.Application.Accounting.Dtos;
using ERP.Domain.Accounting;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Accounting
{
    public class CustomerLedgerService : ICustomerLedgerService
    {
        private readonly ERPDbContext _dbContext;
        private readonly CustomerLedgerNumberingService _numberingService;

        public CustomerLedgerService(ERPDbContext dbContext, CustomerLedgerNumberingService numberingService)
        {
            _dbContext = dbContext;
            _numberingService = numberingService;
        }

        public async Task<IReadOnlyList<CustomerLedgerListItemDto>> GetCustomerLedgerAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            var q = _dbContext.CustomerLedgerEntries
                .AsNoTracking()
                .Where(x => !x.IsDeleted);

            if (query?.CustomerId.HasValue == true)
            {
                q = q.Where(x => x.CustomerId == query.CustomerId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query?.Status))
            {
                // Can map to status or entry type filter
            }

            if (!string.IsNullOrWhiteSpace(query?.Search))
            {
                var s = query.Search.Trim().ToLower();
                q = q.Where(x =>
                    x.LedgerNumber.ToLower().Contains(s) ||
                    x.CustomerName.ToLower().Contains(s) ||
                    (x.InvoiceNumber != null && x.InvoiceNumber.ToLower().Contains(s)) ||
                    (x.ReceiptNumber != null && x.ReceiptNumber.ToLower().Contains(s)));
            }

            var entries = await q
                .OrderByDescending(x => x.TransactionDate)
                .ThenByDescending(x => x.Id)
                .ToListAsync(cancellationToken);

            return entries.Select(MapToListItemDto).ToList();
        }

        public async Task<CustomerLedgerEntryDto?> GetCustomerLedgerByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.CustomerLedgerEntries
                .Include(x => x.Notes)
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            return entity is null ? null : MapToEntryDto(entity);
        }

        public async Task<CustomerStatementResultDto?> GetCustomerStatementAsync(int customerId, CancellationToken cancellationToken = default)
        {
            var entries = await _dbContext.CustomerLedgerEntries
                .Include(x => x.Notes)
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .AsNoTracking()
                .Where(x => x.CustomerId == customerId && !x.IsDeleted)
                .OrderBy(x => x.TransactionDate)
                .ThenBy(x => x.Id)
                .ToListAsync(cancellationToken);

            if (!entries.Any())
            {
                return null;
            }

            var first = entries.First();
            var last = entries.Last();

            var totalDebit = entries.Sum(e => e.Debit);
            var totalCredit = entries.Sum(e => e.Credit);
            var closingBalance = last.RunningBalance;
            var maxOutstanding = entries.Max(e => e.Outstanding);
            var overdue = entries.Where(e => e.AgeingDays > 30).Sum(e => e.Outstanding);

            var summary = new CustomerStatementSummaryDto
            {
                CustomerId = customerId,
                CustomerName = first.CustomerName,
                LedgerNumber = first.LedgerNumber,
                OpeningBalance = first.OpeningBalance,
                TotalDebit = totalDebit,
                TotalCredit = totalCredit,
                ClosingBalance = closingBalance,
                Outstanding = maxOutstanding,
                Overdue = overdue
            };

            return new CustomerStatementResultDto
            {
                Summary = summary,
                Entries = entries.Select(MapToEntryDto).ToList()
            };
        }

        public async Task<CustomerLedgerDashboardDto> GetCustomerLedgerDashboardAsync(CancellationToken cancellationToken = default)
        {
            var totalCustomers = await _dbContext.CustomerLedgerEntries
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Select(x => x.CustomerId)
                .Distinct()
                .CountAsync(cancellationToken);

            var outstanding = await _dbContext.OutstandingRecords
                .AsNoTracking()
                .Where(x => x.PartyType == PartyType.Customer)
                .SumAsync(x => x.Outstanding, cancellationToken);

            var today = DateTime.UtcNow.Date;
            var receivedToday = await _dbContext.ReceiptEntries
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.Status == ReceiptEntryStatus.Received && x.ReceiptDate.Date == today)
                .SumAsync(x => x.NetAmount, cancellationToken);

            var overdue = await _dbContext.OutstandingRecords
                .AsNoTracking()
                .Where(x => x.PartyType == PartyType.Customer && x.Status == OutstandingStatus.Overdue)
                .SumAsync(x => x.Outstanding, cancellationToken);

            return new CustomerLedgerDashboardDto
            {
                TotalCustomers = totalCustomers,
                Outstanding = outstanding,
                ReceivedToday = receivedToday,
                Overdue = overdue
            };
        }

        public async Task<CustomerLedgerEntryDto> CreateLedgerEntryAsync(CustomerLedgerEntryDto dto, string createdBy, CancellationToken cancellationToken = default)
        {
            var ledgerNumber = string.IsNullOrWhiteSpace(dto.LedgerNumber)
                ? await _numberingService.GenerateNumberAsync(cancellationToken)
                : dto.LedgerNumber;

            Enum.TryParse<LedgerEntryType>(dto.EntryType, true, out var entryType);

            var entity = new CustomerLedgerEntry
            {
                LedgerNumber = ledgerNumber,
                CustomerId = dto.CustomerId,
                CustomerName = dto.CustomerName,
                OpeningBalance = dto.OpeningBalance,
                Debit = dto.Debit,
                Credit = dto.Credit,
                RunningBalance = dto.RunningBalance,
                Outstanding = dto.Outstanding,
                InvoiceId = dto.InvoiceId,
                InvoiceNumber = dto.InvoiceNumber,
                ReceiptId = dto.ReceiptId,
                ReceiptNumber = dto.ReceiptNumber,
                SalesOrderId = dto.SalesOrderId,
                SalesOrderNumber = dto.SalesOrderNumber,
                ProformaInvoiceId = dto.ProformaInvoiceId,
                ProformaInvoiceNumber = dto.ProformaInvoiceNumber,
                EntryType = entryType,
                TransactionDate = dto.TransactionDate == default ? DateTime.UtcNow : dto.TransactionDate,
                DueDate = dto.DueDate,
                AgeingDays = dto.AgeingDays,
                AgeingBucket = string.IsNullOrWhiteSpace(dto.AgeingBucket) ? "0-30" : dto.AgeingBucket,
                Remarks = dto.Remarks,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = createdBy,
                UpdatedAt = DateTime.UtcNow
            };

            entity.Timeline.Add(new AccountingTimelineEvent
            {
                Action = "Created",
                User = createdBy,
                Date = DateTime.UtcNow,
                ToStatus = entity.EntryType.ToString(),
                Remarks = dto.Remarks
            });

            _dbContext.CustomerLedgerEntries.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return MapToEntryDto(entity);
        }

        private static CustomerLedgerListItemDto MapToListItemDto(CustomerLedgerEntry e) => new()
        {
            Id = e.Id,
            LedgerNumber = e.LedgerNumber,
            CustomerId = e.CustomerId,
            CustomerName = e.CustomerName,
            OpeningBalance = e.OpeningBalance,
            Debit = e.Debit,
            Credit = e.Credit,
            RunningBalance = e.RunningBalance,
            Outstanding = e.Outstanding,
            InvoiceNumber = e.InvoiceNumber,
            ReceiptNumber = e.ReceiptNumber,
            SalesOrderNumber = e.SalesOrderNumber,
            ProformaInvoiceNumber = e.ProformaInvoiceNumber,
            EntryType = e.EntryType.ToString(),
            TransactionDate = e.TransactionDate,
            DueDate = e.DueDate,
            AgeingDays = e.AgeingDays,
            AgeingBucket = e.AgeingBucket,
            Remarks = e.Remarks
        };

        private static CustomerLedgerEntryDto MapToEntryDto(CustomerLedgerEntry e) => new()
        {
            Id = e.Id,
            LedgerNumber = e.LedgerNumber,
            CustomerId = e.CustomerId,
            CustomerName = e.CustomerName,
            OpeningBalance = e.OpeningBalance,
            Debit = e.Debit,
            Credit = e.Credit,
            RunningBalance = e.RunningBalance,
            Outstanding = e.Outstanding,
            InvoiceId = e.InvoiceId,
            InvoiceNumber = e.InvoiceNumber,
            ReceiptId = e.ReceiptId,
            ReceiptNumber = e.ReceiptNumber,
            SalesOrderId = e.SalesOrderId,
            SalesOrderNumber = e.SalesOrderNumber,
            ProformaInvoiceId = e.ProformaInvoiceId,
            ProformaInvoiceNumber = e.ProformaInvoiceNumber,
            EntryType = e.EntryType.ToString(),
            TransactionDate = e.TransactionDate,
            DueDate = e.DueDate,
            AgeingDays = e.AgeingDays,
            AgeingBucket = e.AgeingBucket,
            Remarks = e.Remarks,
            Notes = e.Notes.Select(n => new AccountingNoteDto
            {
                Id = n.Id,
                Text = n.Text,
                CreatedBy = n.CreatedBy,
                CreatedAt = n.CreatedAt
            }).ToList(),
            Attachments = e.Attachments.Select(a => new AccountingAttachmentDto
            {
                Id = a.Id,
                Name = a.Name,
                SizeKb = a.SizeKb,
                UploadedBy = a.UploadedBy,
                UploadedAt = a.UploadedAt
            }).ToList(),
            Timeline = e.Timeline.Select(t => new AccountingTimelineEventDto
            {
                Id = t.Id,
                Action = t.Action,
                User = t.User,
                Date = t.Date,
                FromStatus = t.FromStatus,
                ToStatus = t.ToStatus,
                Remarks = t.Remarks
            }).ToList(),
            CreatedBy = e.CreatedBy,
            CreatedAt = e.CreatedAt,
            UpdatedBy = e.UpdatedBy,
            UpdatedAt = e.UpdatedAt
        };
    }
}
