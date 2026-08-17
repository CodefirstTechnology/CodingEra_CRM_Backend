using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using ERP.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Procurement
{
    public class RFQService : IRFQService
    {
        private readonly ERPDbContext _db;
        private readonly RFQNumberingService _numberingService;

        public RFQService(ERPDbContext db, RFQNumberingService numberingService)
        {
            _db = db;
            _numberingService = numberingService;
        }

        public async Task<PagedResult<RFQListItemDto>> GetAllAsync(
            RFQListQueryDto query,
            CancellationToken cancellationToken = default)
        {
            var q = _db.RequestForQuotations
                .Include(r => r.Vendors)
                .Include(r => r.Lines)
                .AsNoTracking()
                .Where(r => !r.IsDeleted);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim().ToLower();
                q = q.Where(r =>
                    r.RFQNumber.ToLower().Contains(term) ||
                    r.PurchaseRequisitionNumber.ToLower().Contains(term) ||
                    r.DeliveryTerms.ToLower().Contains(term) ||
                    r.PaymentTerms.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(query.PRNumber))
            {
                q = q.Where(r => r.PurchaseRequisitionNumber.ToLower() == query.PRNumber.Trim().ToLower());
            }

            if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<RFQStatus>(query.Status, true, out var statusEnum))
            {
                q = q.Where(r => r.Status == statusEnum);
            }

            q = (query.SortBy?.ToLowerInvariant(), query.SortDescending) switch
            {
                ("rfqnumber", true) => q.OrderByDescending(r => r.RFQNumber),
                ("rfqnumber", false) => q.OrderBy(r => r.RFQNumber),
                ("rfqdate", true) => q.OrderByDescending(r => r.RFQDate),
                ("rfqdate", false) => q.OrderBy(r => r.RFQDate),
                ("duedate", true) => q.OrderByDescending(r => r.DueDate),
                ("duedate", false) => q.OrderBy(r => r.DueDate),
                ("status", true) => q.OrderByDescending(r => r.Status),
                ("status", false) => q.OrderBy(r => r.Status),
                _ => query.SortDescending ? q.OrderByDescending(r => r.CreatedAt) : q.OrderBy(r => r.CreatedAt)
            };

            var totalCount = await q.CountAsync(cancellationToken);
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 1000);

            var items = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var dtos = items.Select(MapToListItemDto).ToList();
            return PagedResult<RFQListItemDto>.Create(dtos, totalCount, page, pageSize);
        }

        public async Task<RFQDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var rfq = await _db.RequestForQuotations
                .Include(r => r.Lines)
                .Include(r => r.Vendors)
                .Include(r => r.History.OrderByDescending(h => h.Date))
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);

            return rfq is null ? null : MapToDto(rfq);
        }

        public async Task<RFQDto> CreateAsync(
            RFQCreateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var lineTuples = request.Lines?.Select(l => (l.ItemName, l.Quantity)).ToList() ?? new();
            RFQRules.ValidateCreate(request.RFQDate, request.DueDate, lineTuples);

            var now = DateTime.UtcNow;
            var rfqNumber = await _numberingService.NextRFQNumberAsync(cancellationToken);

            var rfq = new RequestForQuotation
            {
                RFQNumber = rfqNumber,
                RFQDate = request.RFQDate.ToUniversalTime(),
                DueDate = request.DueDate.ToUniversalTime(),
                PurchaseRequisitionId = request.PurchaseRequisitionId,
                Status = RFQStatus.Draft,
                DeliveryTerms = request.DeliveryTerms?.Trim() ?? string.Empty,
                PaymentTerms = request.PaymentTerms?.Trim() ?? string.Empty,
                VendorNotes = request.VendorNotes?.Trim() ?? string.Empty,
                CreatedAt = now,
                CreatedBy = actingUser,
                UpdatedAt = now,
                UpdatedBy = actingUser
            };

            if (request.PurchaseRequisitionId.HasValue && request.PurchaseRequisitionId.Value > 0)
            {
                var pr = await _db.PurchaseRequisitions.FirstOrDefaultAsync(p => p.Id == request.PurchaseRequisitionId.Value && !p.IsDeleted, cancellationToken);
                if (pr != null)
                {
                    rfq.PurchaseRequisitionNumber = pr.PRNumber;
                }
            }

            foreach (var l in request.Lines ?? new List<RFQLineDto>())
            {
                rfq.Lines.Add(new RequestForQuotationLine
                {
                    ItemName = l.ItemName.Trim(),
                    Description = l.Description?.Trim() ?? string.Empty,
                    Quantity = l.Quantity,
                    Uom = string.IsNullOrWhiteSpace(l.Uom) ? "PCS" : l.Uom.Trim(),
                    TargetUnitPrice = l.TargetUnitPrice
                });
            }

            if (request.VendorIds != null && request.VendorIds.Count > 0)
            {
                var vendors = await _db.Vendors
                    .Where(v => request.VendorIds.Contains(v.Id) && !v.IsDeleted)
                    .ToListAsync(cancellationToken);

                foreach (var v in vendors)
                {
                    rfq.Vendors.Add(new RequestForQuotationVendor
                    {
                        VendorId = v.Id,
                        VendorName = v.Name,
                        ContactEmail = v.Email,
                        ContactPhone = v.Phone,
                        Status = RFQVendorStatus.Pending
                    });
                }
            }

            rfq.History.Add(new RequestForQuotationStatusHistory
            {
                Status = RFQStatus.Draft,
                PreviousStatus = null,
                User = actingUser,
                Remarks = "RFQ created in Draft status.",
                Date = now
            });

            _db.RequestForQuotations.Add(rfq);
            await _db.SaveChangesAsync(cancellationToken);

            return (await GetByIdAsync(rfq.Id, cancellationToken))!;
        }

        public async Task<RFQDto?> CreateFromPRAsync(
            int purchaseRequisitionId,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var pr = await _db.PurchaseRequisitions
                .Include(p => p.Lines)
                .FirstOrDefaultAsync(p => p.Id == purchaseRequisitionId && !p.IsDeleted, cancellationToken);

            if (pr is null) return null;

            if (pr.Status != PurchaseRequisitionStatus.Approved)
            {
                throw new InvalidOperationException($"Cannot create RFQ from Purchase Requisition in {pr.Status} status. Must be Approved.");
            }

            var request = new RFQCreateRequestDto
            {
                PurchaseRequisitionId = pr.Id,
                RFQDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(7),
                DeliveryTerms = "Standard Delivery",
                PaymentTerms = "Net 30 Days",
                VendorNotes = $"Generated from PR: {pr.PRNumber}",
                Lines = pr.Lines.Select(l => new RFQLineDto
                {
                    ItemName = l.ItemName,
                    Description = l.Description,
                    Quantity = l.Quantity,
                    Uom = l.Uom,
                    TargetUnitPrice = l.EstimatedPrice
                }).ToList()
            };

            var rfq = await CreateAsync(request, actingUser, cancellationToken);

            pr.Status = PurchaseRequisitionStatus.ConvertedToRFQ;
            pr.RFQId = rfq.Id;
            pr.RFQNumber = rfq.RFQNumber;
            pr.UpdatedAt = DateTime.UtcNow;
            pr.UpdatedBy = actingUser;

            pr.History.Add(new PurchaseRequisitionStatusHistory
            {
                PurchaseRequisitionId = pr.Id,
                Status = PurchaseRequisitionStatus.ConvertedToRFQ,
                PreviousStatus = PurchaseRequisitionStatus.Approved,
                User = actingUser,
                Remarks = $"Converted to RFQ: {rfq.RFQNumber}.",
                Date = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(cancellationToken);
            return rfq;
        }

        public async Task<RFQDto?> UpdateAsync(
            int id,
            RFQUpdateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var rfq = await _db.RequestForQuotations
                .Include(r => r.Lines)
                .Include(r => r.Vendors)
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);

            if (rfq is null) return null;

            if (rfq.Status != RFQStatus.Draft && rfq.Status != RFQStatus.Sent)
            {
                throw new InvalidOperationException($"Cannot update RFQ in {rfq.Status} status.");
            }

            var lineTuples = request.Lines?.Select(l => (l.ItemName, l.Quantity)).ToList() ?? new();
            RFQRules.ValidateCreate(request.RFQDate, request.DueDate, lineTuples);

            var now = DateTime.UtcNow;

            rfq.RFQDate = request.RFQDate.ToUniversalTime();
            rfq.DueDate = request.DueDate.ToUniversalTime();
            rfq.DeliveryTerms = request.DeliveryTerms?.Trim() ?? string.Empty;
            rfq.PaymentTerms = request.PaymentTerms?.Trim() ?? string.Empty;
            rfq.VendorNotes = request.VendorNotes?.Trim() ?? string.Empty;
            rfq.UpdatedAt = now;
            rfq.UpdatedBy = actingUser;

            _db.RequestForQuotationLines.RemoveRange(rfq.Lines);
            rfq.Lines.Clear();

            foreach (var l in request.Lines ?? new List<RFQLineDto>())
            {
                rfq.Lines.Add(new RequestForQuotationLine
                {
                    RequestForQuotationId = id,
                    ItemName = l.ItemName.Trim(),
                    Description = l.Description?.Trim() ?? string.Empty,
                    Quantity = l.Quantity,
                    Uom = string.IsNullOrWhiteSpace(l.Uom) ? "PCS" : l.Uom.Trim(),
                    TargetUnitPrice = l.TargetUnitPrice
                });
            }

            if (request.VendorIds != null)
            {
                _db.RequestForQuotationVendors.RemoveRange(rfq.Vendors);
                rfq.Vendors.Clear();

                var vendors = await _db.Vendors
                    .Where(v => request.VendorIds.Contains(v.Id) && !v.IsDeleted)
                    .ToListAsync(cancellationToken);

                foreach (var v in vendors)
                {
                    rfq.Vendors.Add(new RequestForQuotationVendor
                    {
                        RequestForQuotationId = id,
                        VendorId = v.Id,
                        VendorName = v.Name,
                        ContactEmail = v.Email,
                        ContactPhone = v.Phone,
                        Status = RFQVendorStatus.Pending
                    });
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            return (await GetByIdAsync(id, cancellationToken))!;
        }

        public async Task<bool> DeleteAsync(int id, string actingUser, CancellationToken cancellationToken = default)
        {
            var rfq = await _db.RequestForQuotations.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
            if (rfq is null) return false;

            rfq.IsDeleted = true;
            rfq.UpdatedAt = DateTime.UtcNow;
            rfq.UpdatedBy = actingUser;

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<RFQDto?> SendAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default)
        {
            return await ChangeStatusAsync(id, RFQStatus.Sent, remarks, actingUser, cancellationToken);
        }

        public async Task<RFQDto?> CloseAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default)
        {
            return await ChangeStatusAsync(id, RFQStatus.Closed, remarks, actingUser, cancellationToken);
        }

        public async Task<RFQDto?> CancelAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default)
        {
            return await ChangeStatusAsync(id, RFQStatus.Cancelled, remarks, actingUser, cancellationToken);
        }

        public async Task<string> GetNextRFQNumberAsync(CancellationToken cancellationToken = default)
        {
            return await _numberingService.NextRFQNumberAsync(cancellationToken);
        }

        private async Task<RFQDto?> ChangeStatusAsync(
            int id,
            RFQStatus targetStatus,
            string remarks,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var rfq = await _db.RequestForQuotations
                .Include(r => r.History)
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);

            if (rfq is null) return null;

            if (!RFQRules.CanTransition(rfq.Status, targetStatus))
            {
                throw new InvalidOperationException($"Cannot transition RFQ status from {rfq.Status} to {targetStatus}.");
            }

            var prev = rfq.Status;
            var now = DateTime.UtcNow;

            rfq.Status = targetStatus;
            rfq.UpdatedAt = now;
            rfq.UpdatedBy = actingUser;

            rfq.History.Add(new RequestForQuotationStatusHistory
            {
                RequestForQuotationId = id,
                Status = targetStatus,
                PreviousStatus = prev,
                User = actingUser,
                Remarks = string.IsNullOrWhiteSpace(remarks) ? $"RFQ status changed to {targetStatus}." : remarks.Trim(),
                Date = now
            });

            await _db.SaveChangesAsync(cancellationToken);
            return (await GetByIdAsync(id, cancellationToken))!;
        }

        private static RFQListItemDto MapToListItemDto(RequestForQuotation rfq) => new()
        {
            Id = rfq.Id,
            RFQNumber = rfq.RFQNumber,
            RFQDate = rfq.RFQDate,
            DueDate = rfq.DueDate,
            PurchaseRequisitionId = rfq.PurchaseRequisitionId,
            PurchaseRequisitionNumber = rfq.PurchaseRequisitionNumber,
            Status = rfq.Status,
            VendorCount = rfq.Vendors?.Count ?? 0,
            LineCount = rfq.Lines?.Count ?? 0,
            CreatedAt = rfq.CreatedAt,
            CreatedBy = rfq.CreatedBy
        };

        private static RFQDto MapToDto(RequestForQuotation rfq) => new()
        {
            Id = rfq.Id,
            RFQNumber = rfq.RFQNumber,
            RFQDate = rfq.RFQDate,
            DueDate = rfq.DueDate,
            PurchaseRequisitionId = rfq.PurchaseRequisitionId,
            PurchaseRequisitionNumber = rfq.PurchaseRequisitionNumber,
            Status = rfq.Status,
            DeliveryTerms = rfq.DeliveryTerms,
            PaymentTerms = rfq.PaymentTerms,
            VendorNotes = rfq.VendorNotes,
            ComparisonId = rfq.ComparisonId,
            CreatedAt = rfq.CreatedAt,
            CreatedBy = rfq.CreatedBy,
            UpdatedAt = rfq.UpdatedAt,
            UpdatedBy = rfq.UpdatedBy,
            Lines = rfq.Lines?.Select(l => new RFQLineDto
            {
                Id = l.Id,
                RequestForQuotationId = l.RequestForQuotationId,
                ItemName = l.ItemName,
                Description = l.Description,
                Quantity = l.Quantity,
                Uom = l.Uom,
                TargetUnitPrice = l.TargetUnitPrice
            }).ToList() ?? new(),
            Vendors = rfq.Vendors?.Select(v => new RFQVendorSelectionDto
            {
                Id = v.Id,
                RequestForQuotationId = v.RequestForQuotationId,
                VendorId = v.VendorId,
                VendorName = v.VendorName,
                ContactEmail = v.ContactEmail,
                ContactPhone = v.ContactPhone,
                Status = v.Status,
                RespondedDate = v.RespondedDate
            }).ToList() ?? new(),
            History = rfq.History?.Select(h => new RFQStatusHistoryDto
            {
                Id = h.Id,
                RequestForQuotationId = h.RequestForQuotationId,
                Status = h.Status,
                PreviousStatus = h.PreviousStatus,
                User = h.User,
                Remarks = h.Remarks,
                Date = h.Date
            }).ToList() ?? new()
        };
    }
}
