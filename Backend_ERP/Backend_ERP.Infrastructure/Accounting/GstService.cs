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
    public class GstService : IGstService
    {
        private readonly ERPDbContext _dbContext;
        private readonly GstNumberingService _numberingService;

        public GstService(ERPDbContext dbContext, GstNumberingService numberingService)
        {
            _dbContext = dbContext;
            _numberingService = numberingService;
        }

        public async Task<IReadOnlyList<GstTransactionListItemDto>> GetGstTransactionsAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            var q = _dbContext.GstTransactions
                .AsNoTracking()
                .Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(query?.Status) && Enum.TryParse<GstReturnStatus>(query.Status, true, out var st))
            {
                q = q.Where(x => x.Status == st);
            }

            if (!string.IsNullOrWhiteSpace(query?.ReturnPeriod))
            {
                q = q.Where(x => x.ReturnPeriod == query.ReturnPeriod);
            }

            if (!string.IsNullOrWhiteSpace(query?.Search))
            {
                var s = query.Search.Trim().ToLower();
                q = q.Where(x =>
                    x.GstNumber.ToLower().Contains(s) ||
                    x.InvoiceNumber.ToLower().Contains(s) ||
                    (x.CustomerName != null && x.CustomerName.ToLower().Contains(s)) ||
                    (x.VendorName != null && x.VendorName.ToLower().Contains(s)));
            }

            var entries = await q
                .OrderByDescending(x => x.InvoiceDate)
                .ThenByDescending(x => x.Id)
                .ToListAsync(cancellationToken);

            return entries.Select(MapToListItemDto).ToList();
        }

        public async Task<GstTransactionDto?> GetGstTransactionByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.GstTransactions
                .Include(x => x.Notes)
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            return entity is null ? null : MapToDto(entity);
        }

        public async Task<GstTransactionDto> CreateGstTransactionAsync(GstTransactionDto dto, string user, CancellationToken cancellationToken = default)
        {
            var gstNumber = string.IsNullOrWhiteSpace(dto.GstNumber)
                ? await _numberingService.GenerateNumberAsync(cancellationToken)
                : dto.GstNumber;

            Enum.TryParse<GstTxnType>(dto.TxnType.Replace(" ", ""), true, out var txnType);
            Enum.TryParse<GstReturnStatus>(dto.Status, true, out var status);

            var taxAmount = dto.Cgst + dto.Sgst + dto.Igst + dto.Cess;
            if (taxAmount <= 0 && dto.TaxAmount > 0)
            {
                taxAmount = dto.TaxAmount;
            }

            var returnPeriod = string.IsNullOrWhiteSpace(dto.ReturnPeriod)
                ? dto.InvoiceDate.ToString("yyyy-MM")
                : dto.ReturnPeriod;

            var entity = new GstTransaction
            {
                GstNumber = gstNumber,
                InvoiceId = dto.InvoiceId,
                InvoiceNumber = dto.InvoiceNumber,
                CustomerId = dto.CustomerId,
                CustomerName = dto.CustomerName,
                VendorId = dto.VendorId,
                VendorName = dto.VendorName,
                TxnType = txnType,
                TaxableValue = dto.TaxableValue,
                Cgst = dto.Cgst,
                Sgst = dto.Sgst,
                Igst = dto.Igst,
                Cess = dto.Cess,
                TaxAmount = taxAmount,
                InvoiceDate = dto.InvoiceDate == default ? DateTime.UtcNow : dto.InvoiceDate,
                ReturnPeriod = returnPeriod,
                Status = status,
                Remarks = dto.Remarks,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = user,
                UpdatedAt = DateTime.UtcNow
            };

            entity.Timeline.Add(new AccountingTimelineEvent
            {
                Action = "Created",
                User = user,
                Date = DateTime.UtcNow,
                ToStatus = entity.Status.ToString(),
                Remarks = dto.Remarks
            });

            _dbContext.GstTransactions.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Sync Period Return
            await EnsurePeriodReturnSyncedAsync(returnPeriod, cancellationToken);

            return MapToDto(entity);
        }

        public async Task<GstTransactionDto> VerifyGstTransactionAsync(int id, StatusActionRequestDto? payload, string user, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.GstTransactions
                .Include(x => x.Notes)
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (entity is null) throw new KeyNotFoundException($"GST Transaction #{id} not found");

            entity.Status = GstReturnStatus.Verified;
            entity.UpdatedBy = user;
            entity.UpdatedAt = DateTime.UtcNow;

            entity.Timeline.Add(new AccountingTimelineEvent
            {
                Action = "Verified",
                User = user,
                Date = DateTime.UtcNow,
                FromStatus = "Draft",
                ToStatus = "Verified",
                Remarks = payload?.Remarks
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapToDto(entity);
        }

        public async Task<IReadOnlyList<GstReturnDto>> GetGstReturnsAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            var periods = await _dbContext.GstTransactions
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Select(x => x.ReturnPeriod)
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var p in periods)
            {
                await EnsurePeriodReturnSyncedAsync(p, cancellationToken);
            }

            var q = _dbContext.GstReturns
                .Include(x => x.Timeline)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query?.Status) && Enum.TryParse<GstReturnStatus>(query.Status, true, out var st))
            {
                q = q.Where(x => x.Status == st);
            }

            if (!string.IsNullOrWhiteSpace(query?.ReturnPeriod))
            {
                q = q.Where(x => x.ReturnPeriod == query.ReturnPeriod);
            }

            var returns = await q
                .OrderByDescending(x => x.ReturnPeriod)
                .ToListAsync(cancellationToken);

            return returns.Select(MapToReturnDto).ToList();
        }

        public async Task<GstReturnDto> VerifyGstReturnAsync(int id, StatusActionRequestDto? payload, string user, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.GstReturns
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (entity is null) throw new KeyNotFoundException($"GST Return #{id} not found");
            if (entity.Status != GstReturnStatus.Draft)
            {
                throw new InvalidOperationException("GST return must be in Draft status to verify");
            }

            entity.Status = GstReturnStatus.Verified;
            entity.UpdatedBy = user;
            entity.UpdatedAt = DateTime.UtcNow;

            entity.Timeline.Add(new AccountingTimelineEvent
            {
                Action = "Verified",
                User = user,
                Date = DateTime.UtcNow,
                FromStatus = "Draft",
                ToStatus = "Verified",
                Remarks = payload?.Remarks
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapToReturnDto(entity);
        }

        public async Task<GstReturnDto> FileGstReturnAsync(int id, StatusActionRequestDto? payload, string user, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.GstReturns
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (entity is null) throw new KeyNotFoundException($"GST Return #{id} not found");
            if (entity.Status != GstReturnStatus.Verified)
            {
                throw new InvalidOperationException("GST return must be Verified before filing");
            }

            entity.Status = GstReturnStatus.Filed;
            entity.FiledAt = DateTime.UtcNow;
            entity.FiledBy = user;
            entity.UpdatedBy = user;
            entity.UpdatedAt = DateTime.UtcNow;

            entity.Timeline.Add(new AccountingTimelineEvent
            {
                Action = "Filed",
                User = user,
                Date = DateTime.UtcNow,
                FromStatus = "Verified",
                ToStatus = "Filed",
                Remarks = payload?.Remarks
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapToReturnDto(entity);
        }

        public async Task<GstReturnDto> CloseGstReturnAsync(int id, StatusActionRequestDto? payload, string user, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.GstReturns
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (entity is null) throw new KeyNotFoundException($"GST Return #{id} not found");
            if (entity.Status != GstReturnStatus.Filed)
            {
                throw new InvalidOperationException("GST return must be Filed before closing");
            }

            entity.Status = GstReturnStatus.Closed;
            entity.UpdatedBy = user;
            entity.UpdatedAt = DateTime.UtcNow;

            entity.Timeline.Add(new AccountingTimelineEvent
            {
                Action = "Closed",
                User = user,
                Date = DateTime.UtcNow,
                FromStatus = "Filed",
                ToStatus = "Closed",
                Remarks = payload?.Remarks
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapToReturnDto(entity);
        }

        public async Task<IReadOnlyList<GstSummaryDto>> GetGstSummaryAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            var q = _dbContext.GstTransactions
                .AsNoTracking()
                .Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(query?.ReturnPeriod))
            {
                q = q.Where(x => x.ReturnPeriod == query.ReturnPeriod);
            }

            var txns = await q.ToListAsync(cancellationToken);

            var grouped = txns
                .GroupBy(x => x.ReturnPeriod)
                .OrderByDescending(g => g.Key)
                .Select(g =>
                {
                    var collected = g.Where(t => t.TxnType == GstTxnType.SalesInvoice || t.TxnType == GstTxnType.DebitNote).Sum(t => t.TaxAmount);
                    var paid = g.Where(t => t.TxnType == GstTxnType.PurchaseBill || t.TxnType == GstTxnType.CreditNote).Sum(t => t.TaxAmount);

                    return new GstSummaryDto
                    {
                        ReturnPeriod = g.Key,
                        TaxableValue = g.Sum(t => t.TaxableValue),
                        Cgst = g.Sum(t => t.Cgst),
                        Sgst = g.Sum(t => t.Sgst),
                        Igst = g.Sum(t => t.Igst),
                        Cess = g.Sum(t => t.Cess),
                        TaxAmount = g.Sum(t => t.TaxAmount),
                        Collected = collected,
                        Paid = paid,
                        Net = collected - paid
                    };
                })
                .ToList();

            return grouped;
        }

        public async Task<GstDashboardDto> GetGstDashboardAsync(CancellationToken cancellationToken = default)
        {
            var txns = await _dbContext.GstTransactions
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .ToListAsync(cancellationToken);

            var gstCollected = txns
                .Where(t => t.TxnType == GstTxnType.SalesInvoice || t.TxnType == GstTxnType.DebitNote)
                .Sum(t => t.TaxAmount);

            var gstPaid = txns
                .Where(t => t.TxnType == GstTxnType.PurchaseBill || t.TxnType == GstTxnType.CreditNote)
                .Sum(t => t.TaxAmount);

            var returns = await _dbContext.GstReturns
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var pendingReturns = returns.Count(r => r.Status == GstReturnStatus.Draft || r.Status == GstReturnStatus.Verified);
            var filedReturns = returns.Count(r => r.Status == GstReturnStatus.Filed || r.Status == GstReturnStatus.Closed);

            return new GstDashboardDto
            {
                GstCollected = gstCollected,
                GstPaid = gstPaid,
                PendingReturns = pendingReturns,
                FiledReturns = filedReturns
            };
        }

        private async Task EnsurePeriodReturnSyncedAsync(string returnPeriod, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(returnPeriod)) return;

            var txns = await _dbContext.GstTransactions
                .AsNoTracking()
                .Where(x => x.ReturnPeriod == returnPeriod && !x.IsDeleted)
                .ToListAsync(cancellationToken);

            if (!txns.Any()) return;

            var collected = txns.Where(t => t.TxnType == GstTxnType.SalesInvoice || t.TxnType == GstTxnType.DebitNote).Sum(t => t.TaxAmount);
            var paid = txns.Where(t => t.TxnType == GstTxnType.PurchaseBill || t.TxnType == GstTxnType.CreditNote).Sum(t => t.TaxAmount);

            var ret = await _dbContext.GstReturns
                .FirstOrDefaultAsync(x => x.ReturnPeriod == returnPeriod, cancellationToken);

            if (ret is null)
            {
                ret = new GstReturn
                {
                    ReturnPeriod = returnPeriod,
                    Status = GstReturnStatus.Draft,
                    GstCollected = collected,
                    GstPaid = paid,
                    NetPayable = collected - paid,
                    TransactionCount = txns.Count,
                    CreatedBy = "System",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedBy = "System",
                    UpdatedAt = DateTime.UtcNow
                };
                _dbContext.GstReturns.Add(ret);
            }
            else if (ret.Status == GstReturnStatus.Draft)
            {
                ret.GstCollected = collected;
                ret.GstPaid = paid;
                ret.NetPayable = collected - paid;
                ret.TransactionCount = txns.Count;
                ret.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private static GstTransactionListItemDto MapToListItemDto(GstTransaction t) => new()
        {
            Id = t.Id,
            GstNumber = t.GstNumber,
            InvoiceNumber = t.InvoiceNumber,
            CustomerName = t.CustomerName,
            VendorName = t.VendorName,
            TxnType = t.TxnType switch
            {
                GstTxnType.SalesInvoice => "Sales Invoice",
                GstTxnType.PurchaseBill => "Purchase Bill",
                GstTxnType.DebitNote => "Debit Note",
                GstTxnType.CreditNote => "Credit Note",
                _ => t.TxnType.ToString()
            },
            TaxableValue = t.TaxableValue,
            Cgst = t.Cgst,
            Sgst = t.Sgst,
            Igst = t.Igst,
            Cess = t.Cess,
            TaxAmount = t.TaxAmount,
            InvoiceDate = t.InvoiceDate,
            ReturnPeriod = t.ReturnPeriod,
            Status = t.Status.ToString()
        };

        private static GstTransactionDto MapToDto(GstTransaction t) => new()
        {
            Id = t.Id,
            GstNumber = t.GstNumber,
            InvoiceId = t.InvoiceId,
            InvoiceNumber = t.InvoiceNumber,
            CustomerId = t.CustomerId,
            CustomerName = t.CustomerName,
            VendorId = t.VendorId,
            VendorName = t.VendorName,
            TxnType = t.TxnType switch
            {
                GstTxnType.SalesInvoice => "Sales Invoice",
                GstTxnType.PurchaseBill => "Purchase Bill",
                GstTxnType.DebitNote => "Debit Note",
                GstTxnType.CreditNote => "Credit Note",
                _ => t.TxnType.ToString()
            },
            TaxableValue = t.TaxableValue,
            Cgst = t.Cgst,
            Sgst = t.Sgst,
            Igst = t.Igst,
            Cess = t.Cess,
            TaxAmount = t.TaxAmount,
            InvoiceDate = t.InvoiceDate,
            ReturnPeriod = t.ReturnPeriod,
            Status = t.Status.ToString(),
            Remarks = t.Remarks,
            Notes = t.Notes.Select(n => new AccountingNoteDto
            {
                Id = n.Id,
                Text = n.Text,
                CreatedBy = n.CreatedBy,
                CreatedAt = n.CreatedAt
            }).ToList(),
            Attachments = t.Attachments.Select(a => new AccountingAttachmentDto
            {
                Id = a.Id,
                Name = a.Name,
                SizeKb = a.SizeKb,
                UploadedBy = a.UploadedBy,
                UploadedAt = a.UploadedAt
            }).ToList(),
            Timeline = t.Timeline.Select(tl => new AccountingTimelineEventDto
            {
                Id = tl.Id,
                Action = tl.Action,
                User = tl.User,
                Date = tl.Date,
                FromStatus = tl.FromStatus,
                ToStatus = tl.ToStatus,
                Remarks = tl.Remarks
            }).ToList(),
            CreatedBy = t.CreatedBy,
            CreatedAt = t.CreatedAt,
            UpdatedBy = t.UpdatedBy,
            UpdatedAt = t.UpdatedAt
        };

        private static GstReturnDto MapToReturnDto(GstReturn r) => new()
        {
            Id = r.Id,
            ReturnPeriod = r.ReturnPeriod,
            Status = r.Status.ToString(),
            GstCollected = r.GstCollected,
            GstPaid = r.GstPaid,
            NetPayable = r.NetPayable,
            TransactionCount = r.TransactionCount,
            FiledAt = r.FiledAt,
            FiledBy = r.FiledBy,
            Remarks = r.Remarks,
            Timeline = r.Timeline.Select(tl => new AccountingTimelineEventDto
            {
                Id = tl.Id,
                Action = tl.Action,
                User = tl.User,
                Date = tl.Date,
                FromStatus = tl.FromStatus,
                ToStatus = tl.ToStatus,
                Remarks = tl.Remarks
            }).ToList()
        };
    }
}
