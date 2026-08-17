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
    public class VendorComparisonService : IVendorComparisonService
    {
        private readonly ERPDbContext _db;
        private readonly VendorComparisonNumberingService _numberingService;

        public VendorComparisonService(ERPDbContext db, VendorComparisonNumberingService numberingService)
        {
            _db = db;
            _numberingService = numberingService;
        }

        public async Task<PagedResult<VendorComparisonListItemDto>> GetAllAsync(
            VendorComparisonListQueryDto query,
            CancellationToken cancellationToken = default)
        {
            var q = _db.VendorComparisons.AsNoTracking().Where(c => !c.IsDeleted);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim().ToLower();
                q = q.Where(c =>
                    c.ComparisonNumber.ToLower().Contains(term) ||
                    c.Title.ToLower().Contains(term) ||
                    c.RFQNumber.ToLower().Contains(term) ||
                    c.SelectedWinnerVendorName.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<VendorComparisonStatus>(query.Status, true, out var statusEnum))
            {
                q = q.Where(c => c.Status == statusEnum);
            }

            q = (query.SortBy?.ToLowerInvariant(), query.SortDescending) switch
            {
                ("comparisonnumber", true) => q.OrderByDescending(c => c.ComparisonNumber),
                ("comparisonnumber", false) => q.OrderBy(c => c.ComparisonNumber),
                ("comparisondate", true) => q.OrderByDescending(c => c.ComparisonDate),
                ("comparisondate", false) => q.OrderBy(c => c.ComparisonDate),
                ("status", true) => q.OrderByDescending(c => c.Status),
                ("status", false) => q.OrderBy(c => c.Status),
                _ => query.SortDescending ? q.OrderByDescending(c => c.CreatedAt) : q.OrderBy(c => c.CreatedAt)
            };

            var totalCount = await q.CountAsync(cancellationToken);
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 1000);

            var items = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var dtos = items.Select(MapToListItemDto).ToList();
            return PagedResult<VendorComparisonListItemDto>.Create(dtos, totalCount, page, pageSize);
        }

        public async Task<VendorComparisonDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var vc = await _db.VendorComparisons
                .Include(c => c.Entries)
                .ThenInclude(e => e.Lines)
                .Include(c => c.History.OrderByDescending(h => h.Date))
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

            return vc is null ? null : MapToDto(vc);
        }

        public async Task<VendorComparisonDto> CreateAsync(
            VendorComparisonCreateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var comparisonNumber = await _numberingService.NextComparisonNumberAsync(cancellationToken);

            var vc = new VendorComparison
            {
                ComparisonNumber = comparisonNumber,
                RFQId = request.RFQId,
                Title = string.IsNullOrWhiteSpace(request.Title) ? $"Vendor Comparison {comparisonNumber}" : request.Title.Trim(),
                ComparisonDate = request.ComparisonDate.ToUniversalTime(),
                RecommendationNotes = request.RecommendationNotes?.Trim() ?? string.Empty,
                RecommendedVendorId = request.RecommendedVendorId ?? 0,
                SelectedWinnerVendorId = request.SelectedWinnerVendorId,
                SelectedWinnerVendorName = request.SelectedWinnerVendorName?.Trim() ?? string.Empty,
                Status = VendorComparisonStatus.Pending,
                CreatedAt = now,
                CreatedBy = actingUser,
                UpdatedAt = now,
                UpdatedBy = actingUser
            };

            if (request.RFQId.HasValue && request.RFQId.Value > 0)
            {
                var rfq = await _db.RequestForQuotations.FirstOrDefaultAsync(r => r.Id == request.RFQId.Value && !r.IsDeleted, cancellationToken);
                if (rfq != null)
                {
                    vc.RFQNumber = rfq.RFQNumber;
                }
            }

            foreach (var e in request.Vendors ?? new List<VendorQuotationEntryDto>())
            {
                var entry = new VendorComparisonEntry
                {
                    VendorId = e.VendorId,
                    VendorName = e.VendorName.Trim(),
                    QuotationRef = e.QuotationRef?.Trim() ?? string.Empty,
                    VendorQuotationId = e.VendorQuotationId,
                    UnitPrice = e.UnitPrice,
                    TaxPercent = e.TaxPercent,
                    DiscountPercent = e.DiscountPercent,
                    DeliveryTimeDays = e.DeliveryTimeDays,
                    LeadTime = e.LeadTime?.Trim() ?? string.Empty,
                    WarrantyPeriod = e.WarrantyPeriod?.Trim() ?? string.Empty,
                    PaymentTerms = e.PaymentTerms?.Trim() ?? string.Empty,
                    TotalCost = e.TotalCost > 0 ? e.TotalCost : e.UnitPrice,
                    Remarks = e.Remarks?.Trim() ?? string.Empty
                };

                foreach (var l in e.Lines ?? new List<VendorQuotationLineDto>())
                {
                    entry.Lines.Add(new VendorQuotationLine
                    {
                        ItemName = l.ItemName.Trim(),
                        Quantity = l.Quantity,
                        Uom = string.IsNullOrWhiteSpace(l.Uom) ? "PCS" : l.Uom.Trim(),
                        UnitPrice = l.UnitPrice,
                        TaxPercent = l.TaxPercent,
                        DiscountPercent = l.DiscountPercent,
                        Amount = l.Amount
                    });
                }

                vc.Entries.Add(entry);
            }

            VendorComparisonRules.EvaluateMetrics(vc.Entries);

            if (vc.RecommendedVendorId <= 0 && vc.Entries.Count > 0)
            {
                var lowest = vc.Entries.FirstOrDefault(e => e.IsLowestPrice);
                if (lowest != null)
                {
                    vc.RecommendedVendorId = lowest.VendorId;
                    if (string.IsNullOrWhiteSpace(vc.RecommendationNotes))
                    {
                        vc.RecommendationNotes = $"Recommended Vendor: {lowest.VendorName} based on lowest total cost.";
                    }
                }
                else
                {
                    vc.RecommendedVendorId = vc.Entries[0].VendorId;
                }
            }

            if (vc.SelectedWinnerVendorId is null || vc.SelectedWinnerVendorId <= 0)
            {
                vc.SelectedWinnerVendorId = vc.RecommendedVendorId;
                var winner = vc.Entries.FirstOrDefault(e => e.VendorId == vc.RecommendedVendorId);
                if (winner != null)
                {
                    vc.SelectedWinnerVendorName = winner.VendorName;
                }
            }

            vc.History.Add(new VendorComparisonStatusHistory
            {
                Status = VendorComparisonStatus.Pending,
                PreviousStatus = null,
                User = actingUser,
                Remarks = "Vendor comparison matrix generated.",
                Date = now
            });

            _db.VendorComparisons.Add(vc);
            await _db.SaveChangesAsync(cancellationToken);

            return (await GetByIdAsync(vc.Id, cancellationToken))!;
        }

        public async Task<VendorComparisonDto?> UpdateAsync(
            int id,
            VendorComparisonUpdateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var vc = await _db.VendorComparisons
                .Include(c => c.Entries)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

            if (vc is null) return null;

            if (vc.Status == VendorComparisonStatus.Awarded)
            {
                throw new InvalidOperationException("Cannot update an awarded vendor comparison matrix.");
            }

            var now = DateTime.UtcNow;

            vc.Title = string.IsNullOrWhiteSpace(request.Title) ? vc.Title : request.Title.Trim();
            vc.ComparisonDate = request.ComparisonDate.ToUniversalTime();
            vc.RecommendationNotes = request.RecommendationNotes?.Trim() ?? string.Empty;
            vc.RecommendedVendorId = request.RecommendedVendorId;
            vc.SelectedWinnerVendorId = request.SelectedWinnerVendorId;
            vc.SelectedWinnerVendorName = request.SelectedWinnerVendorName?.Trim() ?? string.Empty;
            vc.UpdatedAt = now;
            vc.UpdatedBy = actingUser;

            _db.VendorComparisonEntries.RemoveRange(vc.Entries);
            vc.Entries.Clear();

            foreach (var e in request.Vendors ?? new List<VendorQuotationEntryDto>())
            {
                var entry = new VendorComparisonEntry
                {
                    VendorComparisonId = id,
                    VendorId = e.VendorId,
                    VendorName = e.VendorName.Trim(),
                    QuotationRef = e.QuotationRef?.Trim() ?? string.Empty,
                    VendorQuotationId = e.VendorQuotationId,
                    UnitPrice = e.UnitPrice,
                    TaxPercent = e.TaxPercent,
                    DiscountPercent = e.DiscountPercent,
                    DeliveryTimeDays = e.DeliveryTimeDays,
                    LeadTime = e.LeadTime?.Trim() ?? string.Empty,
                    WarrantyPeriod = e.WarrantyPeriod?.Trim() ?? string.Empty,
                    PaymentTerms = e.PaymentTerms?.Trim() ?? string.Empty,
                    TotalCost = e.TotalCost > 0 ? e.TotalCost : e.UnitPrice,
                    Remarks = e.Remarks?.Trim() ?? string.Empty
                };

                foreach (var l in e.Lines ?? new List<VendorQuotationLineDto>())
                {
                    entry.Lines.Add(new VendorQuotationLine
                    {
                        ItemName = l.ItemName.Trim(),
                        Quantity = l.Quantity,
                        Uom = string.IsNullOrWhiteSpace(l.Uom) ? "PCS" : l.Uom.Trim(),
                        UnitPrice = l.UnitPrice,
                        TaxPercent = l.TaxPercent,
                        DiscountPercent = l.DiscountPercent,
                        Amount = l.Amount
                    });
                }

                vc.Entries.Add(entry);
            }

            VendorComparisonRules.EvaluateMetrics(vc.Entries);

            await _db.SaveChangesAsync(cancellationToken);
            return (await GetByIdAsync(id, cancellationToken))!;
        }

        public async Task<bool> DeleteAsync(int id, string actingUser, CancellationToken cancellationToken = default)
        {
            var vc = await _db.VendorComparisons.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);
            if (vc is null) return false;

            vc.IsDeleted = true;
            vc.UpdatedAt = DateTime.UtcNow;
            vc.UpdatedBy = actingUser;

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<VendorComparisonDto?> MarkComparedAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default)
        {
            return await ChangeStatusAsync(id, VendorComparisonStatus.Compared, remarks, actingUser, cancellationToken);
        }

        public async Task<VendorComparisonDto?> ApproveAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default)
        {
            return await ChangeStatusAsync(id, VendorComparisonStatus.Approved, remarks, actingUser, cancellationToken);
        }

        public async Task<VendorComparisonDto?> AwardVendorAsync(
            int id,
            VendorAwardRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var vc = await _db.VendorComparisons
                .Include(c => c.Entries)
                .Include(c => c.History)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

            if (vc is null) return null;

            if (!VendorComparisonRules.CanTransition(vc.Status, VendorComparisonStatus.Awarded))
            {
                throw new InvalidOperationException($"Cannot transition vendor comparison from {vc.Status} to Awarded.");
            }

            var winner = vc.Entries.FirstOrDefault(e => e.VendorId == request.VendorId);
            var winnerName = winner != null ? winner.VendorName : $"Vendor ID {request.VendorId}";

            var prev = vc.Status;
            var now = DateTime.UtcNow;

            vc.SelectedWinnerVendorId = request.VendorId;
            vc.SelectedWinnerVendorName = winnerName;

            if (request.PurchaseOrderId.HasValue && request.PurchaseOrderId.Value > 0)
            {
                vc.PurchaseOrderId = request.PurchaseOrderId.Value;
                vc.PurchaseOrderNumber = request.PurchaseOrderNumber;
            }

            vc.Status = VendorComparisonStatus.Awarded;
            vc.UpdatedAt = now;
            vc.UpdatedBy = actingUser;

            var rmk = string.IsNullOrWhiteSpace(request.Remarks)
                ? $"Awarded to {winnerName}."
                : request.Remarks.Trim();

            vc.History.Add(new VendorComparisonStatusHistory
            {
                VendorComparisonId = id,
                Status = VendorComparisonStatus.Awarded,
                PreviousStatus = prev,
                User = actingUser,
                Remarks = rmk,
                Date = now
            });

            await _db.SaveChangesAsync(cancellationToken);
            return (await GetByIdAsync(id, cancellationToken))!;
        }

        public async Task<string> GetNextComparisonNumberAsync(CancellationToken cancellationToken = default)
        {
            return await _numberingService.NextComparisonNumberAsync(cancellationToken);
        }

        private async Task<VendorComparisonDto?> ChangeStatusAsync(
            int id,
            VendorComparisonStatus targetStatus,
            string remarks,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var vc = await _db.VendorComparisons
                .Include(c => c.History)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

            if (vc is null) return null;

            if (!VendorComparisonRules.CanTransition(vc.Status, targetStatus))
            {
                throw new InvalidOperationException($"Cannot transition vendor comparison status from {vc.Status} to {targetStatus}.");
            }

            var prev = vc.Status;
            var now = DateTime.UtcNow;

            vc.Status = targetStatus;
            vc.UpdatedAt = now;
            vc.UpdatedBy = actingUser;

            vc.History.Add(new VendorComparisonStatusHistory
            {
                VendorComparisonId = id,
                Status = targetStatus,
                PreviousStatus = prev,
                User = actingUser,
                Remarks = string.IsNullOrWhiteSpace(remarks) ? $"Status changed to {targetStatus}." : remarks.Trim(),
                Date = now
            });

            await _db.SaveChangesAsync(cancellationToken);
            return (await GetByIdAsync(id, cancellationToken))!;
        }

        private static VendorComparisonListItemDto MapToListItemDto(VendorComparison vc) => new()
        {
            Id = vc.Id,
            ComparisonNumber = vc.ComparisonNumber,
            RFQId = vc.RFQId,
            RFQNumber = vc.RFQNumber,
            Title = vc.Title,
            ComparisonDate = vc.ComparisonDate,
            Status = vc.Status,
            RecommendedVendorId = vc.RecommendedVendorId,
            SelectedWinnerVendorId = vc.SelectedWinnerVendorId,
            SelectedWinnerVendorName = vc.SelectedWinnerVendorName,
            PurchaseOrderId = vc.PurchaseOrderId,
            PurchaseOrderNumber = vc.PurchaseOrderNumber,
            CreatedAt = vc.CreatedAt,
            CreatedBy = vc.CreatedBy
        };

        private static VendorComparisonDto MapToDto(VendorComparison vc) => new()
        {
            Id = vc.Id,
            ComparisonNumber = vc.ComparisonNumber,
            RFQId = vc.RFQId,
            RFQNumber = vc.RFQNumber,
            Title = vc.Title,
            ComparisonDate = vc.ComparisonDate,
            RecommendationNotes = vc.RecommendationNotes,
            RecommendedVendorId = vc.RecommendedVendorId,
            SelectedWinnerVendorId = vc.SelectedWinnerVendorId,
            SelectedWinnerVendorName = vc.SelectedWinnerVendorName,
            PurchaseOrderId = vc.PurchaseOrderId,
            PurchaseOrderNumber = vc.PurchaseOrderNumber,
            Status = vc.Status,
            CreatedAt = vc.CreatedAt,
            CreatedBy = vc.CreatedBy,
            UpdatedAt = vc.UpdatedAt,
            UpdatedBy = vc.UpdatedBy,
            Vendors = vc.Entries?.Select(e => new VendorQuotationEntryDto
            {
                Id = e.Id,
                VendorComparisonId = e.VendorComparisonId,
                VendorId = e.VendorId,
                VendorName = e.VendorName,
                QuotationRef = e.QuotationRef,
                VendorQuotationId = e.VendorQuotationId,
                UnitPrice = e.UnitPrice,
                TaxPercent = e.TaxPercent,
                DiscountPercent = e.DiscountPercent,
                DeliveryTimeDays = e.DeliveryTimeDays,
                LeadTime = e.LeadTime,
                WarrantyPeriod = e.WarrantyPeriod,
                PaymentTerms = e.PaymentTerms,
                TotalCost = e.TotalCost,
                Ranking = e.Ranking,
                IsLowestPrice = e.IsLowestPrice,
                IsBestDelivery = e.IsBestDelivery,
                RecommendationScore = e.RecommendationScore,
                Remarks = e.Remarks,
                Lines = e.Lines?.Select(l => new VendorQuotationLineDto
                {
                    Id = l.Id,
                    VendorQuotationId = l.VendorQuotationId,
                    ItemName = l.ItemName,
                    Quantity = l.Quantity,
                    Uom = l.Uom,
                    UnitPrice = l.UnitPrice,
                    TaxPercent = l.TaxPercent,
                    DiscountPercent = l.DiscountPercent,
                    Amount = l.Amount
                }).ToList() ?? new()
            }).ToList() ?? new(),
            History = vc.History?.Select(h => new VendorComparisonStatusHistoryDto
            {
                Id = h.Id,
                VendorComparisonId = h.VendorComparisonId,
                Status = h.Status,
                PreviousStatus = h.PreviousStatus,
                User = h.User,
                Remarks = h.Remarks,
                Date = h.Date
            }).ToList() ?? new()
        };
    }
}
