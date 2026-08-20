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
    public class ReceiptService : IReceiptService
    {
        private readonly ERPDbContext _dbContext;
        private readonly ReceiptNumberingService _numberingService;
        private readonly IOutstandingService _outstandingService;
        private readonly ICustomerLedgerService _customerLedgerService;

        public ReceiptService(
            ERPDbContext dbContext,
            ReceiptNumberingService numberingService,
            IOutstandingService outstandingService,
            ICustomerLedgerService customerLedgerService)
        {
            _dbContext = dbContext;
            _numberingService = numberingService;
            _outstandingService = outstandingService;
            _customerLedgerService = customerLedgerService;
        }

        public async Task<IReadOnlyList<ReceiptListItemDto>> GetReceiptsAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            var q = _dbContext.ReceiptEntries
                .AsNoTracking()
                .Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(query?.Status) && Enum.TryParse<ReceiptEntryStatus>(query.Status, true, out var st))
            {
                q = q.Where(x => x.Status == st);
            }

            if (query?.CustomerId.HasValue == true)
            {
                q = q.Where(x => x.CustomerId == query.CustomerId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query?.Search))
            {
                var s = query.Search.Trim().ToLower();
                q = q.Where(x =>
                    x.ReceiptNumber.ToLower().Contains(s) ||
                    x.CustomerName.ToLower().Contains(s) ||
                    x.InvoiceNumber.ToLower().Contains(s) ||
                    (x.ReferenceNumber != null && x.ReferenceNumber.ToLower().Contains(s)));
            }

            var entries = await q
                .OrderByDescending(x => x.ReceiptDate)
                .ThenByDescending(x => x.Id)
                .ToListAsync(cancellationToken);

            return entries.Select(MapToListItemDto).ToList();
        }

        public async Task<ReceiptEntryDto?> GetReceiptByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.ReceiptEntries
                .Include(x => x.Notes)
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            return entity is null ? null : MapToDto(entity);
        }

        public async Task<ReceiptEntryDto> CreateReceiptAsync(ReceiptCreateRequestDto dto, string user, CancellationToken cancellationToken = default)
        {
            ValidateReceiptFields(dto.Amount, dto.Tds, dto.ReceiptMode, dto.ReferenceNumber);

            Enum.TryParse<PaymentMode>(dto.ReceiptMode.Replace(" ", ""), true, out var mode);
            var netAmount = dto.Amount - dto.Tds;
            var receiptNumber = await _numberingService.GenerateNumberAsync(cancellationToken);

            DateTime receiptDate = DateTime.TryParse(dto.ReceiptDate, out var parsedDate) ? parsedDate : DateTime.UtcNow;

            var entity = new ReceiptEntry
            {
                ReceiptNumber = receiptNumber,
                CustomerId = dto.CustomerId,
                CustomerName = dto.CustomerName,
                InvoiceId = dto.InvoiceId,
                InvoiceNumber = dto.InvoiceNumber,
                SalesOrderId = dto.SalesOrderId,
                SalesOrderNumber = dto.SalesOrderNumber,
                ReceiptDate = receiptDate,
                ReceiptMode = mode,
                BankName = dto.BankName,
                BankAccount = dto.BankAccount,
                ReferenceNumber = dto.ReferenceNumber,
                Amount = dto.Amount,
                Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "INR" : dto.Currency,
                Tds = dto.Tds,
                NetAmount = netAmount,
                Status = ReceiptEntryStatus.Draft,
                Remarks = dto.Remarks,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = user,
                UpdatedAt = DateTime.UtcNow
            };

            if (!string.IsNullOrWhiteSpace(dto.Notes))
            {
                entity.Notes.Add(new AccountingNote
                {
                    Text = dto.Notes,
                    CreatedBy = user,
                    CreatedAt = DateTime.UtcNow
                });
            }

            entity.Timeline.Add(new AccountingTimelineEvent
            {
                Action = "Created",
                User = user,
                Date = DateTime.UtcNow,
                ToStatus = "Draft",
                Remarks = dto.Remarks
            });

            _dbContext.ReceiptEntries.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return MapToDto(entity);
        }

        public async Task<ReceiptEntryDto> UpdateReceiptAsync(int id, ReceiptUpdateRequestDto dto, string user, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.ReceiptEntries
                .Include(x => x.Notes)
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (entity is null) throw new KeyNotFoundException($"Receipt #{id} not found");
            if (entity.Status != ReceiptEntryStatus.Draft)
            {
                throw new InvalidOperationException("Only draft receipts can be edited");
            }

            if (dto.CustomerId.HasValue) entity.CustomerId = dto.CustomerId.Value;
            if (!string.IsNullOrWhiteSpace(dto.CustomerName)) entity.CustomerName = dto.CustomerName;
            if (dto.InvoiceId.HasValue) entity.InvoiceId = dto.InvoiceId.Value;
            if (!string.IsNullOrWhiteSpace(dto.InvoiceNumber)) entity.InvoiceNumber = dto.InvoiceNumber;
            if (dto.SalesOrderId.HasValue) entity.SalesOrderId = dto.SalesOrderId.Value;
            if (!string.IsNullOrWhiteSpace(dto.SalesOrderNumber)) entity.SalesOrderNumber = dto.SalesOrderNumber;
            if (!string.IsNullOrWhiteSpace(dto.BankName)) entity.BankName = dto.BankName;
            if (!string.IsNullOrWhiteSpace(dto.BankAccount)) entity.BankAccount = dto.BankAccount;
            if (dto.ReferenceNumber != null) entity.ReferenceNumber = dto.ReferenceNumber;
            if (!string.IsNullOrWhiteSpace(dto.Remarks)) entity.Remarks = dto.Remarks;

            if (!string.IsNullOrWhiteSpace(dto.ReceiptDate) && DateTime.TryParse(dto.ReceiptDate, out var parsedDate))
            {
                entity.ReceiptDate = parsedDate;
            }

            if (!string.IsNullOrWhiteSpace(dto.ReceiptMode) && Enum.TryParse<PaymentMode>(dto.ReceiptMode.Replace(" ", ""), true, out var mode))
            {
                entity.ReceiptMode = mode;
            }

            if (dto.Amount.HasValue) entity.Amount = dto.Amount.Value;
            if (dto.Tds.HasValue) entity.Tds = dto.Tds.Value;
            entity.NetAmount = entity.Amount - entity.Tds;

            ValidateReceiptFields(entity.Amount, entity.Tds, entity.ReceiptMode.ToString(), entity.ReferenceNumber);

            entity.UpdatedBy = user;
            entity.UpdatedAt = DateTime.UtcNow;

            entity.Timeline.Add(new AccountingTimelineEvent
            {
                Action = "Updated",
                User = user,
                Date = DateTime.UtcNow,
                Remarks = dto.Remarks
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapToDto(entity);
        }

        public async Task<bool> DeleteReceiptAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.ReceiptEntries
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (entity is null) return false;
            if (entity.Status != ReceiptEntryStatus.Draft)
            {
                throw new InvalidOperationException("Only draft receipts can be deleted");
            }

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<ReceiptEntryDto> ApproveReceiptAsync(int id, StatusActionRequestDto? payload, string user, CancellationToken cancellationToken = default)
        {
            return await TransitionReceiptAsync(id, ReceiptEntryStatus.Draft, ReceiptEntryStatus.Approved, "Approved", payload, user, cancellationToken);
        }

        public async Task<ReceiptEntryDto> PostReceiptAsync(int id, StatusActionRequestDto? payload, string user, CancellationToken cancellationToken = default)
        {
            return await TransitionReceiptAsync(id, ReceiptEntryStatus.Approved, ReceiptEntryStatus.Posted, "Posted", payload, user, cancellationToken);
        }

        public async Task<ReceiptEntryDto> RecordReceivedAsync(int id, StatusActionRequestDto? payload, string user, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.ReceiptEntries
                .Include(x => x.Notes)
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (entity is null) throw new KeyNotFoundException($"Receipt #{id} not found");
            if (entity.Status != ReceiptEntryStatus.Posted)
            {
                throw new InvalidOperationException("Receipt must be in Posted status to record as Received");
            }

            // 1. Settle against Customer Outstanding
            await _outstandingService.ApplySettlementAsync("Customer", entity.InvoiceNumber, entity.NetAmount, cancellationToken);

            // 2. Post Credit Entry into Customer Ledger
            var priorEntries = await _dbContext.CustomerLedgerEntries
                .AsNoTracking()
                .Where(x => x.CustomerId == entity.CustomerId && !x.IsDeleted)
                .OrderBy(x => x.Id)
                .ToListAsync(cancellationToken);

            var last = priorEntries.LastOrDefault();
            var opening = last?.RunningBalance ?? 0m;
            var credit = entity.NetAmount;
            var running = opening - credit;

            await _customerLedgerService.CreateLedgerEntryAsync(new CustomerLedgerEntryDto
            {
                CustomerId = entity.CustomerId,
                CustomerName = entity.CustomerName,
                OpeningBalance = opening,
                Debit = 0m,
                Credit = credit,
                RunningBalance = running,
                Outstanding = Math.Max(0m, running),
                InvoiceId = entity.InvoiceId,
                InvoiceNumber = entity.InvoiceNumber,
                ReceiptId = entity.Id,
                ReceiptNumber = entity.ReceiptNumber,
                SalesOrderId = entity.SalesOrderId,
                SalesOrderNumber = entity.SalesOrderNumber,
                EntryType = "Receipt",
                TransactionDate = entity.ReceiptDate,
                Remarks = $"Receipt {entity.ReceiptNumber} against {entity.InvoiceNumber}"
            }, user, cancellationToken);

            entity.Status = ReceiptEntryStatus.Received;
            entity.UpdatedBy = user;
            entity.UpdatedAt = DateTime.UtcNow;

            entity.Timeline.Add(new AccountingTimelineEvent
            {
                Action = "Recorded Received",
                User = user,
                Date = DateTime.UtcNow,
                FromStatus = "Posted",
                ToStatus = "Received",
                Remarks = payload?.Remarks
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapToDto(entity);
        }

        public async Task<ReceiptEntryDto> CancelReceiptAsync(int id, StatusActionRequestDto? payload, string user, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.ReceiptEntries
                .Include(x => x.Notes)
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (entity is null) throw new KeyNotFoundException($"Receipt #{id} not found");
            if (entity.Status == ReceiptEntryStatus.Received || entity.Status == ReceiptEntryStatus.Cancelled)
            {
                throw new InvalidOperationException("Cannot cancel a Received or Cancelled receipt");
            }

            if (string.IsNullOrWhiteSpace(payload?.Remarks))
            {
                throw new InvalidOperationException("Remarks are mandatory for cancellation");
            }

            var from = entity.Status.ToString();
            entity.Status = ReceiptEntryStatus.Cancelled;
            entity.UpdatedBy = user;
            entity.UpdatedAt = DateTime.UtcNow;

            entity.Timeline.Add(new AccountingTimelineEvent
            {
                Action = "Cancelled",
                User = user,
                Date = DateTime.UtcNow,
                FromStatus = from,
                ToStatus = "Cancelled",
                Remarks = payload.Remarks
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapToDto(entity);
        }

        public async Task<ReceiptEntryDto> DuplicateReceiptAsync(int id, string user, CancellationToken cancellationToken = default)
        {
            var src = await _dbContext.ReceiptEntries
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (src is null) throw new KeyNotFoundException($"Receipt #{id} not found");

            var receiptNumber = await _numberingService.GenerateNumberAsync(cancellationToken);

            var cloned = new ReceiptEntry
            {
                ReceiptNumber = receiptNumber,
                CustomerId = src.CustomerId,
                CustomerName = src.CustomerName,
                InvoiceId = src.InvoiceId,
                InvoiceNumber = src.InvoiceNumber,
                SalesOrderId = src.SalesOrderId,
                SalesOrderNumber = src.SalesOrderNumber,
                ReceiptDate = DateTime.UtcNow,
                ReceiptMode = src.ReceiptMode,
                BankName = src.BankName,
                BankAccount = src.BankAccount,
                ReferenceNumber = src.ReferenceNumber,
                Amount = src.Amount,
                Currency = src.Currency,
                Tds = src.Tds,
                NetAmount = src.NetAmount,
                Status = ReceiptEntryStatus.Draft,
                Remarks = $"Copy of {src.ReceiptNumber}",
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = user,
                UpdatedAt = DateTime.UtcNow
            };

            cloned.Timeline.Add(new AccountingTimelineEvent
            {
                Action = "Duplicated",
                User = user,
                Date = DateTime.UtcNow,
                ToStatus = "Draft",
                Remarks = $"Duplicated from {src.ReceiptNumber}"
            });

            _dbContext.ReceiptEntries.Add(cloned);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return MapToDto(cloned);
        }

        public async Task<ReceiptDashboardDto> GetReceiptDashboardAsync(CancellationToken cancellationToken = default)
        {
            var today = DateTime.UtcNow.Date;

            var todaysReceipts = await _dbContext.ReceiptEntries
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.ReceiptDate.Date == today)
                .CountAsync(cancellationToken);

            var pending = await _dbContext.ReceiptEntries
                .AsNoTracking()
                .Where(x => !x.IsDeleted && (x.Status == ReceiptEntryStatus.Draft || x.Status == ReceiptEntryStatus.Approved))
                .CountAsync(cancellationToken);

            var received = await _dbContext.ReceiptEntries
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.Status == ReceiptEntryStatus.Received)
                .CountAsync(cancellationToken);

            var outstanding = await _dbContext.OutstandingRecords
                .AsNoTracking()
                .Where(x => x.PartyType == PartyType.Customer)
                .SumAsync(x => x.Outstanding, cancellationToken);

            return new ReceiptDashboardDto
            {
                TodaysReceipts = todaysReceipts,
                Pending = pending,
                Received = received,
                Outstanding = outstanding
            };
        }

        private async Task<ReceiptEntryDto> TransitionReceiptAsync(
            int id,
            ReceiptEntryStatus expectedStatus,
            ReceiptEntryStatus targetStatus,
            string action,
            StatusActionRequestDto? payload,
            string user,
            CancellationToken cancellationToken)
        {
            var entity = await _dbContext.ReceiptEntries
                .Include(x => x.Notes)
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (entity is null) throw new KeyNotFoundException($"Receipt #{id} not found");
            if (entity.Status != expectedStatus)
            {
                throw new InvalidOperationException($"Receipt must be in {expectedStatus} status to perform {action}");
            }

            entity.Status = targetStatus;
            entity.UpdatedBy = user;
            entity.UpdatedAt = DateTime.UtcNow;

            entity.Timeline.Add(new AccountingTimelineEvent
            {
                Action = action,
                User = user,
                Date = DateTime.UtcNow,
                FromStatus = expectedStatus.ToString(),
                ToStatus = targetStatus.ToString(),
                Remarks = payload?.Remarks
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapToDto(entity);
        }

        private static void ValidateReceiptFields(decimal amount, decimal tds, string mode, string? referenceNumber)
        {
            if (amount <= 0) throw new InvalidOperationException("Amount must be greater than zero");
            if (tds < 0) throw new InvalidOperationException("TDS cannot be negative");
            if (tds > amount) throw new InvalidOperationException("TDS cannot exceed amount");

            var modeClean = mode.Replace(" ", "");
            if ((modeClean.Equals("NEFT", StringComparison.OrdinalIgnoreCase) ||
                 modeClean.Equals("RTGS", StringComparison.OrdinalIgnoreCase) ||
                 modeClean.Equals("UPI", StringComparison.OrdinalIgnoreCase) ||
                 modeClean.Equals("BankTransfer", StringComparison.OrdinalIgnoreCase)) &&
                string.IsNullOrWhiteSpace(referenceNumber))
            {
                throw new InvalidOperationException("Reference number is required for electronic receipt mode");
            }
        }

        private static ReceiptListItemDto MapToListItemDto(ReceiptEntry r) => new()
        {
            Id = r.Id,
            ReceiptNumber = r.ReceiptNumber,
            CustomerId = r.CustomerId,
            CustomerName = r.CustomerName,
            InvoiceNumber = r.InvoiceNumber,
            SalesOrderNumber = r.SalesOrderNumber,
            ReceiptDate = r.ReceiptDate,
            ReceiptMode = r.ReceiptMode.ToString(),
            BankName = r.BankName,
            Amount = r.Amount,
            Currency = r.Currency,
            Tds = r.Tds,
            NetAmount = r.NetAmount,
            Status = r.Status.ToString()
        };

        private static ReceiptEntryDto MapToDto(ReceiptEntry r) => new()
        {
            Id = r.Id,
            ReceiptNumber = r.ReceiptNumber,
            CustomerId = r.CustomerId,
            CustomerName = r.CustomerName,
            InvoiceId = r.InvoiceId,
            InvoiceNumber = r.InvoiceNumber,
            SalesOrderId = r.SalesOrderId,
            SalesOrderNumber = r.SalesOrderNumber,
            ReceiptDate = r.ReceiptDate,
            ReceiptMode = r.ReceiptMode.ToString(),
            BankName = r.BankName,
            BankAccount = r.BankAccount,
            ReferenceNumber = r.ReferenceNumber,
            Amount = r.Amount,
            Currency = r.Currency,
            Tds = r.Tds,
            NetAmount = r.NetAmount,
            Status = r.Status.ToString(),
            Remarks = r.Remarks,
            Notes = r.Notes.Select(n => new AccountingNoteDto
            {
                Id = n.Id,
                Text = n.Text,
                CreatedBy = n.CreatedBy,
                CreatedAt = n.CreatedAt
            }).ToList(),
            Attachments = r.Attachments.Select(a => new AccountingAttachmentDto
            {
                Id = a.Id,
                Name = a.Name,
                SizeKb = a.SizeKb,
                UploadedBy = a.UploadedBy,
                UploadedAt = a.UploadedAt
            }).ToList(),
            Timeline = r.Timeline.Select(t => new AccountingTimelineEventDto
            {
                Id = t.Id,
                Action = t.Action,
                User = t.User,
                Date = t.Date,
                FromStatus = t.FromStatus,
                ToStatus = t.ToStatus,
                Remarks = t.Remarks
            }).ToList(),
            CreatedBy = r.CreatedBy,
            CreatedAt = r.CreatedAt,
            UpdatedBy = r.UpdatedBy,
            UpdatedAt = r.UpdatedAt
        };
    }
}
