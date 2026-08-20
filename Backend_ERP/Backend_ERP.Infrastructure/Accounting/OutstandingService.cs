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
    public class OutstandingService : IOutstandingService
    {
        private readonly ERPDbContext _dbContext;

        public OutstandingService(ERPDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<OutstandingRowDto>> GetOutstandingAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            var q = _dbContext.OutstandingRecords.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query?.PartyType) && Enum.TryParse<PartyType>(query.PartyType, true, out var pt))
            {
                q = q.Where(x => x.PartyType == pt);
            }

            if (!string.IsNullOrWhiteSpace(query?.Status) && Enum.TryParse<OutstandingStatus>(query.Status, true, out var st))
            {
                q = q.Where(x => x.Status == st);
            }

            if (query?.CustomerId.HasValue == true)
            {
                q = q.Where(x => x.PartyType == PartyType.Customer && x.PartyId == query.CustomerId.Value);
            }

            if (query?.VendorId.HasValue == true)
            {
                q = q.Where(x => x.PartyType == PartyType.Vendor && x.PartyId == query.VendorId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query?.Search))
            {
                var s = query.Search.Trim().ToLower();
                q = q.Where(x =>
                    x.PartyName.ToLower().Contains(s) ||
                    x.DocumentNumber.ToLower().Contains(s));
            }

            var entries = await q
                .OrderByDescending(x => x.DocumentDate)
                .ThenByDescending(x => x.Id)
                .ToListAsync(cancellationToken);

            return entries.Select(MapToDto).ToList();
        }

        public Task<IReadOnlyList<OutstandingRowDto>> GetCustomerOutstandingAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            var modified = query ?? new ListQueryDto();
            modified.PartyType = "Customer";
            return GetOutstandingAsync(modified, cancellationToken);
        }

        public Task<IReadOnlyList<OutstandingRowDto>> GetVendorOutstandingAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            var modified = query ?? new ListQueryDto();
            modified.PartyType = "Vendor";
            return GetOutstandingAsync(modified, cancellationToken);
        }

        public async Task<AgeingReportDto> GetAgeingReportAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            var rows = await GetOutstandingAsync(query, cancellationToken);
            var open = rows.Where(r => r.Outstanding > 0).ToList();

            var partyType = query?.PartyType ?? "All";

            return new AgeingReportDto
            {
                PartyType = partyType,
                Bucket0to30 = open.Sum(r => r.Bucket0to30),
                Bucket31to60 = open.Sum(r => r.Bucket31to60),
                Bucket61to90 = open.Sum(r => r.Bucket61to90),
                Bucket90plus = open.Sum(r => r.Bucket90plus),
                Total = open.Sum(r => r.Outstanding),
                Rows = open
            };
        }

        public async Task<OutstandingDashboardDto> GetOutstandingDashboardAsync(CancellationToken cancellationToken = default)
        {
            var customerOutstanding = await _dbContext.OutstandingRecords
                .AsNoTracking()
                .Where(x => x.PartyType == PartyType.Customer)
                .SumAsync(x => x.Outstanding, cancellationToken);

            var vendorOutstanding = await _dbContext.OutstandingRecords
                .AsNoTracking()
                .Where(x => x.PartyType == PartyType.Vendor)
                .SumAsync(x => x.Outstanding, cancellationToken);

            var overdue = await _dbContext.OutstandingRecords
                .AsNoTracking()
                .Where(x => x.Status == OutstandingStatus.Overdue)
                .SumAsync(x => x.Outstanding, cancellationToken);

            return new OutstandingDashboardDto
            {
                TotalOutstanding = customerOutstanding + vendorOutstanding,
                CustomerOutstanding = customerOutstanding,
                VendorOutstanding = vendorOutstanding,
                Overdue = overdue
            };
        }

        public async Task<OutstandingRowDto> RecordOrUpdateOutstandingAsync(OutstandingRowDto dto, string user, CancellationToken cancellationToken = default)
        {
            Enum.TryParse<PartyType>(dto.PartyType, true, out var partyType);

            var record = await _dbContext.OutstandingRecords
                .FirstOrDefaultAsync(x => x.PartyType == partyType && x.DocumentNumber == dto.DocumentNumber, cancellationToken);

            if (record is null)
            {
                record = new OutstandingRecord
                {
                    PartyType = partyType,
                    PartyId = dto.PartyId,
                    PartyName = dto.PartyName,
                    DocumentId = dto.DocumentId,
                    DocumentNumber = dto.DocumentNumber,
                    DocumentDate = dto.DocumentDate == default ? DateTime.UtcNow : dto.DocumentDate,
                    DueDate = dto.DueDate == default ? DateTime.UtcNow.AddDays(30) : dto.DueDate,
                    OriginalAmount = dto.OriginalAmount,
                    PaidAmount = dto.PaidAmount,
                    Outstanding = dto.Outstanding,
                    CreatedBy = user,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedBy = user,
                    UpdatedAt = DateTime.UtcNow
                };
                _dbContext.OutstandingRecords.Add(record);
            }
            else
            {
                record.PartyName = dto.PartyName;
                record.OriginalAmount = dto.OriginalAmount;
                record.PaidAmount = dto.PaidAmount;
                record.Outstanding = dto.Outstanding;
                record.DueDate = dto.DueDate;
                record.UpdatedBy = user;
                record.UpdatedAt = DateTime.UtcNow;
            }

            RecalculateAgeingAndStatus(record);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return MapToDto(record);
        }

        public async Task<bool> ApplySettlementAsync(string partyTypeStr, string documentNumber, decimal amount, CancellationToken cancellationToken = default)
        {
            if (amount <= 0) return false;
            Enum.TryParse<PartyType>(partyTypeStr, true, out var partyType);

            var record = await _dbContext.OutstandingRecords
                .FirstOrDefaultAsync(x => x.PartyType == partyType && x.DocumentNumber == documentNumber, cancellationToken);

            if (record is null || amount > record.Outstanding + 0.0001m)
            {
                return false;
            }

            record.PaidAmount = Math.Min(record.OriginalAmount, record.PaidAmount + amount);
            record.Outstanding = Math.Max(0, record.OriginalAmount - record.PaidAmount);

            RecalculateAgeingAndStatus(record);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        private static void RecalculateAgeingAndStatus(OutstandingRecord record)
        {
            var now = DateTime.UtcNow.Date;
            var docDate = record.DocumentDate.Date;
            record.AgeingDays = Math.Max(0, (int)(now - docDate).TotalDays);

            record.Bucket0To30 = 0;
            record.Bucket31To60 = 0;
            record.Bucket61To90 = 0;
            record.Bucket90Plus = 0;

            if (record.Outstanding <= 0)
            {
                record.Status = OutstandingStatus.Settled;
                return;
            }

            if (record.AgeingDays <= 30) record.Bucket0To30 = record.Outstanding;
            else if (record.AgeingDays <= 60) record.Bucket31To60 = record.Outstanding;
            else if (record.AgeingDays <= 90) record.Bucket61To90 = record.Outstanding;
            else record.Bucket90Plus = record.Outstanding;

            if (record.AgeingDays > 30)
            {
                record.Status = OutstandingStatus.Overdue;
            }
            else if (record.PaidAmount > 0)
            {
                record.Status = OutstandingStatus.Partial;
            }
            else
            {
                record.Status = OutstandingStatus.Open;
            }
        }

        private static OutstandingRowDto MapToDto(OutstandingRecord r) => new()
        {
            Id = r.Id,
            PartyType = r.PartyType.ToString(),
            PartyId = r.PartyId,
            PartyName = r.PartyName,
            DocumentId = r.DocumentId,
            DocumentNumber = r.DocumentNumber,
            DocumentDate = r.DocumentDate,
            DueDate = r.DueDate,
            OriginalAmount = r.OriginalAmount,
            PaidAmount = r.PaidAmount,
            Outstanding = r.Outstanding,
            Status = r.Status.ToString(),
            AgeingDays = r.AgeingDays,
            Bucket0to30 = r.Bucket0To30,
            Bucket31to60 = r.Bucket31To60,
            Bucket61to90 = r.Bucket61To90,
            Bucket90plus = r.Bucket90Plus
        };
    }
}
