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
    public class PaymentService : IPaymentService
    {
        private readonly ERPDbContext _dbContext;
        private readonly PaymentNumberingService _numberingService;
        private readonly IOutstandingService _outstandingService;

        public PaymentService(
            ERPDbContext dbContext,
            PaymentNumberingService numberingService,
            IOutstandingService outstandingService)
        {
            _dbContext = dbContext;
            _numberingService = numberingService;
            _outstandingService = outstandingService;
        }

        public async Task<IReadOnlyList<PaymentListItemDto>> GetPaymentsAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            var q = _dbContext.PaymentEntries
                .AsNoTracking()
                .Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(query?.Status) && Enum.TryParse<PaymentEntryStatus>(query.Status, true, out var st))
            {
                q = q.Where(x => x.Status == st);
            }

            if (query?.VendorId.HasValue == true)
            {
                q = q.Where(x => x.VendorId == query.VendorId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query?.Search))
            {
                var s = query.Search.Trim().ToLower();
                q = q.Where(x =>
                    x.PaymentNumber.ToLower().Contains(s) ||
                    x.VendorName.ToLower().Contains(s) ||
                    x.PurchaseBillNumber.ToLower().Contains(s) ||
                    (x.ReferenceNumber != null && x.ReferenceNumber.ToLower().Contains(s)));
            }

            var entries = await q
                .OrderByDescending(x => x.PaymentDate)
                .ThenByDescending(x => x.Id)
                .ToListAsync(cancellationToken);

            return entries.Select(MapToListItemDto).ToList();
        }

        public async Task<PaymentEntryDto?> GetPaymentByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.PaymentEntries
                .Include(x => x.Notes)
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            return entity is null ? null : MapToDto(entity);
        }

        public async Task<PaymentEntryDto> CreatePaymentAsync(PaymentCreateRequestDto dto, string user, CancellationToken cancellationToken = default)
        {
            ValidatePaymentFields(dto.Amount, dto.Tds, dto.PaymentMode, dto.ChequeNumber, dto.ReferenceNumber);

            Enum.TryParse<PaymentMode>(dto.PaymentMode.Replace(" ", ""), true, out var mode);
            var netAmount = dto.Amount - dto.Tds;
            var paymentNumber = await _numberingService.GenerateNumberAsync(cancellationToken);

            DateTime paymentDate = DateTime.TryParse(dto.PaymentDate, out var parsedDate) ? parsedDate : DateTime.UtcNow;

            var entity = new PaymentEntry
            {
                PaymentNumber = paymentNumber,
                VendorId = dto.VendorId,
                VendorName = dto.VendorName,
                PurchaseBillId = dto.PurchaseBillId,
                PurchaseBillNumber = dto.PurchaseBillNumber,
                PaymentDate = paymentDate,
                PaymentMode = mode,
                BankName = dto.BankName,
                BankAccount = dto.BankAccount,
                ChequeNumber = dto.ChequeNumber,
                ReferenceNumber = dto.ReferenceNumber,
                Amount = dto.Amount,
                Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "INR" : dto.Currency,
                Tds = dto.Tds,
                NetAmount = netAmount,
                Status = PaymentEntryStatus.Draft,
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

            _dbContext.PaymentEntries.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return MapToDto(entity);
        }

        public async Task<PaymentEntryDto> UpdatePaymentAsync(int id, PaymentUpdateRequestDto dto, string user, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.PaymentEntries
                .Include(x => x.Notes)
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (entity is null)
            {
                throw new KeyNotFoundException($"Payment #{id} not found");
            }

            if (entity.Status != PaymentEntryStatus.Draft)
            {
                throw new InvalidOperationException("Only draft payments can be edited");
            }

            if (dto.VendorId.HasValue) entity.VendorId = dto.VendorId.Value;
            if (!string.IsNullOrWhiteSpace(dto.VendorName)) entity.VendorName = dto.VendorName;
            if (dto.PurchaseBillId.HasValue) entity.PurchaseBillId = dto.PurchaseBillId.Value;
            if (!string.IsNullOrWhiteSpace(dto.PurchaseBillNumber)) entity.PurchaseBillNumber = dto.PurchaseBillNumber;
            if (!string.IsNullOrWhiteSpace(dto.BankName)) entity.BankName = dto.BankName;
            if (!string.IsNullOrWhiteSpace(dto.BankAccount)) entity.BankAccount = dto.BankAccount;
            if (dto.ChequeNumber != null) entity.ChequeNumber = dto.ChequeNumber;
            if (dto.ReferenceNumber != null) entity.ReferenceNumber = dto.ReferenceNumber;
            if (!string.IsNullOrWhiteSpace(dto.Remarks)) entity.Remarks = dto.Remarks;

            if (!string.IsNullOrWhiteSpace(dto.PaymentDate) && DateTime.TryParse(dto.PaymentDate, out var parsedDate))
            {
                entity.PaymentDate = parsedDate;
            }

            if (!string.IsNullOrWhiteSpace(dto.PaymentMode) && Enum.TryParse<PaymentMode>(dto.PaymentMode.Replace(" ", ""), true, out var mode))
            {
                entity.PaymentMode = mode;
            }

            if (dto.Amount.HasValue) entity.Amount = dto.Amount.Value;
            if (dto.Tds.HasValue) entity.Tds = dto.Tds.Value;
            entity.NetAmount = entity.Amount - entity.Tds;

            ValidatePaymentFields(entity.Amount, entity.Tds, entity.PaymentMode.ToString(), entity.ChequeNumber, entity.ReferenceNumber);

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

        public async Task<bool> DeletePaymentAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.PaymentEntries
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (entity is null) return false;
            if (entity.Status != PaymentEntryStatus.Draft)
            {
                throw new InvalidOperationException("Only draft payments can be deleted");
            }

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<PaymentEntryDto> ApprovePaymentAsync(int id, StatusActionRequestDto? payload, string user, CancellationToken cancellationToken = default)
        {
            return await TransitionPaymentAsync(id, PaymentEntryStatus.Draft, PaymentEntryStatus.Approved, "Approved", payload, user, cancellationToken);
        }

        public async Task<PaymentEntryDto> PostPaymentAsync(int id, StatusActionRequestDto? payload, string user, CancellationToken cancellationToken = default)
        {
            return await TransitionPaymentAsync(id, PaymentEntryStatus.Approved, PaymentEntryStatus.Posted, "Posted", payload, user, cancellationToken);
        }

        public async Task<PaymentEntryDto> RecordPaidAsync(int id, StatusActionRequestDto? payload, string user, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.PaymentEntries
                .Include(x => x.Notes)
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (entity is null) throw new KeyNotFoundException($"Payment #{id} not found");
            if (entity.Status != PaymentEntryStatus.Posted)
            {
                throw new InvalidOperationException("Payment must be in Posted status to record as Paid");
            }

            // Settle against Vendor Outstanding
            await _outstandingService.ApplySettlementAsync("Vendor", entity.PurchaseBillNumber, entity.NetAmount, cancellationToken);

            entity.Status = PaymentEntryStatus.Paid;
            entity.UpdatedBy = user;
            entity.UpdatedAt = DateTime.UtcNow;

            entity.Timeline.Add(new AccountingTimelineEvent
            {
                Action = "Recorded Paid",
                User = user,
                Date = DateTime.UtcNow,
                FromStatus = "Posted",
                ToStatus = "Paid",
                Remarks = payload?.Remarks
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapToDto(entity);
        }

        public async Task<PaymentEntryDto> CancelPaymentAsync(int id, StatusActionRequestDto? payload, string user, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.PaymentEntries
                .Include(x => x.Notes)
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (entity is null) throw new KeyNotFoundException($"Payment #{id} not found");
            if (entity.Status == PaymentEntryStatus.Paid || entity.Status == PaymentEntryStatus.Cancelled)
            {
                throw new InvalidOperationException("Cannot cancel a Paid or Cancelled payment");
            }

            if (string.IsNullOrWhiteSpace(payload?.Remarks))
            {
                throw new InvalidOperationException("Remarks are mandatory for cancellation");
            }

            var from = entity.Status.ToString();
            entity.Status = PaymentEntryStatus.Cancelled;
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

        public async Task<PaymentEntryDto> DuplicatePaymentAsync(int id, string user, CancellationToken cancellationToken = default)
        {
            var src = await _dbContext.PaymentEntries
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (src is null) throw new KeyNotFoundException($"Payment #{id} not found");

            var paymentNumber = await _numberingService.GenerateNumberAsync(cancellationToken);

            var cloned = new PaymentEntry
            {
                PaymentNumber = paymentNumber,
                VendorId = src.VendorId,
                VendorName = src.VendorName,
                PurchaseBillId = src.PurchaseBillId,
                PurchaseBillNumber = src.PurchaseBillNumber,
                PaymentDate = DateTime.UtcNow,
                PaymentMode = src.PaymentMode,
                BankName = src.BankName,
                BankAccount = src.BankAccount,
                ChequeNumber = src.ChequeNumber,
                ReferenceNumber = src.ReferenceNumber,
                Amount = src.Amount,
                Currency = src.Currency,
                Tds = src.Tds,
                NetAmount = src.NetAmount,
                Status = PaymentEntryStatus.Draft,
                Remarks = $"Copy of {src.PaymentNumber}",
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
                Remarks = $"Duplicated from {src.PaymentNumber}"
            });

            _dbContext.PaymentEntries.Add(cloned);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return MapToDto(cloned);
        }

        public async Task<PaymentDashboardDto> GetPaymentDashboardAsync(CancellationToken cancellationToken = default)
        {
            var today = DateTime.UtcNow.Date;

            var todaysPayments = await _dbContext.PaymentEntries
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.PaymentDate.Date == today)
                .CountAsync(cancellationToken);

            var pendingApproval = await _dbContext.PaymentEntries
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.Status == PaymentEntryStatus.Draft)
                .CountAsync(cancellationToken);

            var posted = await _dbContext.PaymentEntries
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.Status == PaymentEntryStatus.Posted)
                .CountAsync(cancellationToken);

            var paid = await _dbContext.PaymentEntries
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.Status == PaymentEntryStatus.Paid)
                .CountAsync(cancellationToken);

            return new PaymentDashboardDto
            {
                TodaysPayments = todaysPayments,
                PendingApproval = pendingApproval,
                Posted = posted,
                Paid = paid
            };
        }

        private async Task<PaymentEntryDto> TransitionPaymentAsync(
            int id,
            PaymentEntryStatus expectedStatus,
            PaymentEntryStatus targetStatus,
            string action,
            StatusActionRequestDto? payload,
            string user,
            CancellationToken cancellationToken)
        {
            var entity = await _dbContext.PaymentEntries
                .Include(x => x.Notes)
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (entity is null) throw new KeyNotFoundException($"Payment #{id} not found");
            if (entity.Status != expectedStatus)
            {
                throw new InvalidOperationException($"Payment must be in {expectedStatus} status to perform {action}");
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

        private static void ValidatePaymentFields(decimal amount, decimal tds, string mode, string? chequeNumber, string? referenceNumber)
        {
            if (amount <= 0) throw new InvalidOperationException("Amount must be greater than zero");
            if (tds < 0) throw new InvalidOperationException("TDS cannot be negative");
            if (tds > amount) throw new InvalidOperationException("TDS cannot exceed amount");

            var modeClean = mode.Replace(" ", "");
            if (modeClean.Equals("Cheque", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(chequeNumber))
            {
                throw new InvalidOperationException("Cheque number is required for Cheque payments");
            }

            if ((modeClean.Equals("NEFT", StringComparison.OrdinalIgnoreCase) ||
                 modeClean.Equals("RTGS", StringComparison.OrdinalIgnoreCase) ||
                 modeClean.Equals("UPI", StringComparison.OrdinalIgnoreCase) ||
                 modeClean.Equals("BankTransfer", StringComparison.OrdinalIgnoreCase)) &&
                string.IsNullOrWhiteSpace(referenceNumber))
            {
                throw new InvalidOperationException("Reference number is required for electronic payment mode");
            }
        }

        private static PaymentListItemDto MapToListItemDto(PaymentEntry p) => new()
        {
            Id = p.Id,
            PaymentNumber = p.PaymentNumber,
            VendorId = p.VendorId,
            VendorName = p.VendorName,
            PurchaseBillNumber = p.PurchaseBillNumber,
            PaymentDate = p.PaymentDate,
            PaymentMode = p.PaymentMode.ToString(),
            BankName = p.BankName,
            Amount = p.Amount,
            Currency = p.Currency,
            Tds = p.Tds,
            NetAmount = p.NetAmount,
            Status = p.Status.ToString()
        };

        private static PaymentEntryDto MapToDto(PaymentEntry p) => new()
        {
            Id = p.Id,
            PaymentNumber = p.PaymentNumber,
            VendorId = p.VendorId,
            VendorName = p.VendorName,
            PurchaseBillId = p.PurchaseBillId,
            PurchaseBillNumber = p.PurchaseBillNumber,
            PaymentDate = p.PaymentDate,
            PaymentMode = p.PaymentMode.ToString(),
            BankName = p.BankName,
            BankAccount = p.BankAccount,
            ChequeNumber = p.ChequeNumber,
            ReferenceNumber = p.ReferenceNumber,
            Amount = p.Amount,
            Currency = p.Currency,
            Tds = p.Tds,
            NetAmount = p.NetAmount,
            Status = p.Status.ToString(),
            Remarks = p.Remarks,
            Notes = p.Notes.Select(n => new AccountingNoteDto
            {
                Id = n.Id,
                Text = n.Text,
                CreatedBy = n.CreatedBy,
                CreatedAt = n.CreatedAt
            }).ToList(),
            Attachments = p.Attachments.Select(a => new AccountingAttachmentDto
            {
                Id = a.Id,
                Name = a.Name,
                SizeKb = a.SizeKb,
                UploadedBy = a.UploadedBy,
                UploadedAt = a.UploadedAt
            }).ToList(),
            Timeline = p.Timeline.Select(t => new AccountingTimelineEventDto
            {
                Id = t.Id,
                Action = t.Action,
                User = t.User,
                Date = t.Date,
                FromStatus = t.FromStatus,
                ToStatus = t.ToStatus,
                Remarks = t.Remarks
            }).ToList(),
            CreatedBy = p.CreatedBy,
            CreatedAt = p.CreatedAt,
            UpdatedBy = p.UpdatedBy,
            UpdatedAt = p.UpdatedAt
        };
    }
}
