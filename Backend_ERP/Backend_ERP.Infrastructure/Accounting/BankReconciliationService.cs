using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Accounting;
using ERP.Application.Accounting.Dtos;
using ERP.Domain.Accounting;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Accounting
{
    public class BankReconciliationService : IBankReconciliationService
    {
        private readonly ERPDbContext _dbContext;
        private readonly BankReconNumberingService _numberingService;

        public BankReconciliationService(
            ERPDbContext dbContext,
            BankReconNumberingService numberingService)
        {
            _dbContext = dbContext;
            _numberingService = numberingService;
        }

        public async Task<IReadOnlyList<BankReconListItemDto>> GetBankReconciliationsAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            var q = _dbContext.BankReconciliations
                .AsNoTracking()
                .Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(query?.Status) && Enum.TryParse<BankReconStatus>(query.Status, true, out var st))
            {
                q = q.Where(x => x.Status == st);
            }

            if (!string.IsNullOrWhiteSpace(query?.Search))
            {
                var s = query.Search.Trim().ToLower();
                q = q.Where(x =>
                    x.ReconciliationNumber.ToLower().Contains(s) ||
                    x.BankName.ToLower().Contains(s) ||
                    x.AccountNumber.ToLower().Contains(s));
            }

            var entries = await q
                .OrderByDescending(x => x.StatementDate)
                .ThenByDescending(x => x.Id)
                .ToListAsync(cancellationToken);

            return entries.Select(MapToListItemDto).ToList();
        }

        public async Task<BankReconciliationDto?> GetBankReconciliationByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.BankReconciliations
                .Include(x => x.Notes)
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            return entity is null ? null : MapToDto(entity);
        }

        public async Task<BankReconciliationDto> CreateBankReconciliationAsync(BankReconCreateRequestDto dto, string user, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dto.BankName)) throw new InvalidOperationException("Bank Name is required");
            if (string.IsNullOrWhiteSpace(dto.AccountNumber)) throw new InvalidOperationException("Account Number is required");

            var reconciliationNumber = await _numberingService.GenerateNumberAsync(cancellationToken);
            var difference = Math.Abs(dto.ClosingBalance - dto.BookClosingBalance);
            var matchedCount = (dto.PaymentIds?.Count ?? 0) + (dto.ReceiptIds?.Count ?? 0);
            var unmatchedCount = difference > 0 ? 1 : 0;

            DateTime statementDate = DateTime.TryParse(dto.StatementDate, out var parsedDate) ? parsedDate : DateTime.UtcNow;

            var entity = new BankReconciliation
            {
                ReconciliationNumber = reconciliationNumber,
                BankName = dto.BankName,
                AccountNumber = dto.AccountNumber,
                StatementDate = statementDate,
                OpeningBalance = dto.OpeningBalance,
                ClosingBalance = dto.ClosingBalance,
                BookClosingBalance = dto.BookClosingBalance,
                Difference = difference,
                MatchedCount = matchedCount,
                UnmatchedCount = unmatchedCount,
                Status = BankReconStatus.Draft,
                PaymentIdsJson = JsonSerializer.Serialize(dto.PaymentIds ?? new List<int>()),
                ReceiptIdsJson = JsonSerializer.Serialize(dto.ReceiptIds ?? new List<int>()),
                Remarks = dto.Remarks,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = user,
                UpdatedAt = DateTime.UtcNow
            };

            entity.Timeline.Add(new AccountingTimelineEvent
            {
                Action = "Draft created",
                User = user,
                Date = DateTime.UtcNow,
                ToStatus = "Draft",
                Remarks = dto.Remarks
            });

            _dbContext.BankReconciliations.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return MapToDto(entity);
        }

        public async Task<BankReconciliationDto> UpdateBankReconciliationAsync(int id, BankReconUpdateRequestDto dto, string user, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.BankReconciliations
                .Include(x => x.Notes)
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (entity is null) throw new KeyNotFoundException($"Bank reconciliation #{id} not found");
            if (entity.Status != BankReconStatus.Draft)
            {
                throw new InvalidOperationException("Only Draft reconciliations can be edited");
            }

            if (!string.IsNullOrWhiteSpace(dto.BankName)) entity.BankName = dto.BankName;
            if (!string.IsNullOrWhiteSpace(dto.AccountNumber)) entity.AccountNumber = dto.AccountNumber;
            if (!string.IsNullOrWhiteSpace(dto.StatementDate) && DateTime.TryParse(dto.StatementDate, out var parsedDate))
            {
                entity.StatementDate = parsedDate;
            }

            if (dto.OpeningBalance.HasValue) entity.OpeningBalance = dto.OpeningBalance.Value;
            if (dto.ClosingBalance.HasValue) entity.ClosingBalance = dto.ClosingBalance.Value;
            if (dto.BookClosingBalance.HasValue) entity.BookClosingBalance = dto.BookClosingBalance.Value;
            if (dto.Remarks != null) entity.Remarks = dto.Remarks;

            if (dto.PaymentIds != null) entity.PaymentIdsJson = JsonSerializer.Serialize(dto.PaymentIds);
            if (dto.ReceiptIds != null) entity.ReceiptIdsJson = JsonSerializer.Serialize(dto.ReceiptIds);

            var payCount = ParseIds(entity.PaymentIdsJson).Count;
            var recCount = ParseIds(entity.ReceiptIdsJson).Count;
            entity.MatchedCount = payCount + recCount;
            entity.Difference = Math.Abs(entity.ClosingBalance - entity.BookClosingBalance);
            entity.UnmatchedCount = entity.Difference > 0 ? 1 : 0;
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

        public async Task<BankReconciliationDto> VerifyBankReconciliationAsync(int id, StatusActionRequestDto? payload, string user, CancellationToken cancellationToken = default)
        {
            return await TransitionBankReconAsync(id, BankReconStatus.Draft, BankReconStatus.Verified, "Verified", payload, user, cancellationToken);
        }

        public async Task<BankReconciliationDto> ReconcileBankAsync(int id, StatusActionRequestDto? payload, string user, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(payload?.Remarks))
            {
                throw new InvalidOperationException("Remarks are mandatory for Reconcile");
            }
            return await TransitionBankReconAsync(id, BankReconStatus.Verified, BankReconStatus.Reconciled, "Reconciled", payload, user, cancellationToken);
        }

        public async Task<BankReconciliationDto> CloseBankReconciliationAsync(int id, StatusActionRequestDto? payload, string user, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(payload?.Remarks))
            {
                throw new InvalidOperationException("Remarks are mandatory for Close Reconciliation");
            }
            return await TransitionBankReconAsync(id, BankReconStatus.Reconciled, BankReconStatus.Closed, "Closed", payload, user, cancellationToken);
        }

        public async Task<BankReconDashboardDto> GetBankReconciliationDashboardAsync(CancellationToken cancellationToken = default)
        {
            var recons = await _dbContext.BankReconciliations
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .ToListAsync(cancellationToken);

            var matched = recons.Sum(b => b.MatchedCount);
            var pending = recons.Count(b => b.Status == BankReconStatus.Draft || b.Status == BankReconStatus.Verified);
            var difference = recons.Sum(b => b.Difference);

            return new BankReconDashboardDto
            {
                Matched = matched,
                Pending = pending,
                Difference = difference
            };
        }

        private async Task<BankReconciliationDto> TransitionBankReconAsync(
            int id,
            BankReconStatus expectedStatus,
            BankReconStatus targetStatus,
            string action,
            StatusActionRequestDto? payload,
            string user,
            CancellationToken cancellationToken)
        {
            var entity = await _dbContext.BankReconciliations
                .Include(x => x.Notes)
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (entity is null) throw new KeyNotFoundException($"Bank reconciliation #{id} not found");
            if (entity.Status != expectedStatus)
            {
                throw new InvalidOperationException($"Bank reconciliation must be in {expectedStatus} status to perform {action}");
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

        private static List<int> ParseIds(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<int>();
            try
            {
                return JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
            }
            catch
            {
                return new List<int>();
            }
        }

        private static BankReconListItemDto MapToListItemDto(BankReconciliation b) => new()
        {
            Id = b.Id,
            ReconciliationNumber = b.ReconciliationNumber,
            BankName = b.BankName,
            AccountNumber = b.AccountNumber,
            StatementDate = b.StatementDate,
            OpeningBalance = b.OpeningBalance,
            ClosingBalance = b.ClosingBalance,
            Difference = b.Difference,
            MatchedCount = b.MatchedCount,
            UnmatchedCount = b.UnmatchedCount,
            Status = b.Status.ToString()
        };

        private static BankReconciliationDto MapToDto(BankReconciliation b) => new()
        {
            Id = b.Id,
            ReconciliationNumber = b.ReconciliationNumber,
            BankName = b.BankName,
            AccountNumber = b.AccountNumber,
            StatementDate = b.StatementDate,
            OpeningBalance = b.OpeningBalance,
            ClosingBalance = b.ClosingBalance,
            BookClosingBalance = b.BookClosingBalance,
            Difference = b.Difference,
            MatchedCount = b.MatchedCount,
            UnmatchedCount = b.UnmatchedCount,
            Status = b.Status.ToString(),
            PaymentIds = ParseIds(b.PaymentIdsJson),
            ReceiptIds = ParseIds(b.ReceiptIdsJson),
            Remarks = b.Remarks,
            Notes = b.Notes.Select(n => new AccountingNoteDto
            {
                Id = n.Id,
                Text = n.Text,
                CreatedBy = n.CreatedBy,
                CreatedAt = n.CreatedAt
            }).ToList(),
            Attachments = b.Attachments.Select(a => new AccountingAttachmentDto
            {
                Id = a.Id,
                Name = a.Name,
                SizeKb = a.SizeKb,
                UploadedBy = a.UploadedBy,
                UploadedAt = a.UploadedAt
            }).ToList(),
            Timeline = b.Timeline.Select(tl => new AccountingTimelineEventDto
            {
                Id = tl.Id,
                Action = tl.Action,
                User = tl.User,
                Date = tl.Date,
                FromStatus = tl.FromStatus,
                ToStatus = tl.ToStatus,
                Remarks = tl.Remarks
            }).ToList(),
            CreatedBy = b.CreatedBy,
            CreatedAt = b.CreatedAt,
            UpdatedBy = b.UpdatedBy,
            UpdatedAt = b.UpdatedAt
        };
    }
}
