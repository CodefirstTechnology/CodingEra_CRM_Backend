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
    public class PurchaseRequisitionService : IPurchaseRequisitionService
    {
        private readonly ERPDbContext _db;
        private readonly PurchaseRequisitionNumberingService _numberingService;

        public PurchaseRequisitionService(ERPDbContext db, PurchaseRequisitionNumberingService numberingService)
        {
            _db = db;
            _numberingService = numberingService;
        }

        public async Task<PagedResult<PurchaseRequisitionListItemDto>> GetAllAsync(
            PurchaseRequisitionListQueryDto query,
            CancellationToken cancellationToken = default)
        {
            var q = _db.PurchaseRequisitions.AsNoTracking().Where(p => !p.IsDeleted);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim().ToLower();
                q = q.Where(p =>
                    p.PRNumber.ToLower().Contains(term) ||
                    p.Department.ToLower().Contains(term) ||
                    p.Requestor.ToLower().Contains(term) ||
                    p.Remarks.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(query.Department))
            {
                q = q.Where(p => p.Department.ToLower() == query.Department.Trim().ToLower());
            }

            if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<PurchaseRequisitionStatus>(query.Status, true, out var statusEnum))
            {
                q = q.Where(p => p.Status == statusEnum);
            }

            if (!string.IsNullOrWhiteSpace(query.Priority) && Enum.TryParse<PurchaseRequisitionPriority>(query.Priority, true, out var priorityEnum))
            {
                q = q.Where(p => p.Priority == priorityEnum);
            }

            q = (query.SortBy?.ToLowerInvariant(), query.SortDescending) switch
            {
                ("prnumber", true) => q.OrderByDescending(p => p.PRNumber),
                ("prnumber", false) => q.OrderBy(p => p.PRNumber),
                ("department", true) => q.OrderByDescending(p => p.Department),
                ("department", false) => q.OrderBy(p => p.Department),
                ("status", true) => q.OrderByDescending(p => p.Status),
                ("status", false) => q.OrderBy(p => p.Status),
                ("totalestimatedamount", true) => q.OrderByDescending(p => p.TotalEstimatedAmount),
                ("totalestimatedamount", false) => q.OrderBy(p => p.TotalEstimatedAmount),
                _ => query.SortDescending ? q.OrderByDescending(p => p.CreatedAt) : q.OrderBy(p => p.CreatedAt)
            };

            var totalCount = await q.CountAsync(cancellationToken);
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 1000);

            var items = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var dtos = items.Select(MapToListItemDto).ToList();
            return PagedResult<PurchaseRequisitionListItemDto>.Create(dtos, totalCount, page, pageSize);
        }

        public async Task<PurchaseRequisitionDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var pr = await _db.PurchaseRequisitions
                .Include(p => p.Lines)
                .Include(p => p.History.OrderByDescending(h => h.Date))
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);

            return pr is null ? null : MapToDto(pr);
        }

        public async Task<PurchaseRequisitionDto> CreateAsync(
            PurchaseRequisitionCreateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var lineTuples = request.Lines?.Select(l => (l.ItemName, l.Quantity, l.EstimatedPrice)).ToList() ?? new();
            PurchaseRequisitionRules.ValidateCreate(request.Department, request.Requestor, request.RequiredDate, lineTuples);

            var now = DateTime.UtcNow;
            var prNumber = await _numberingService.NextPRNumberAsync(cancellationToken);

            var pr = new PurchaseRequisition
            {
                PRNumber = prNumber,
                Department = request.Department.Trim(),
                Requestor = request.Requestor.Trim(),
                RequiredDate = request.RequiredDate.ToUniversalTime(),
                Priority = request.Priority,
                Status = PurchaseRequisitionStatus.Draft,
                Remarks = request.Remarks?.Trim() ?? string.Empty,
                InternalNotes = request.InternalNotes?.Trim() ?? string.Empty,
                CreatedAt = now,
                CreatedBy = actingUser,
                UpdatedAt = now,
                UpdatedBy = actingUser
            };

            decimal totalAmount = 0m;
            foreach (var l in request.Lines ?? new List<PurchaseRequisitionLineDto>())
            {
                var lineTotal = Math.Round(l.Quantity * l.EstimatedPrice, 4);
                totalAmount += lineTotal;

                pr.Lines.Add(new PurchaseRequisitionLine
                {
                    ItemName = l.ItemName.Trim(),
                    Description = l.Description?.Trim() ?? string.Empty,
                    Quantity = l.Quantity,
                    Uom = string.IsNullOrWhiteSpace(l.Uom) ? "PCS" : l.Uom.Trim(),
                    EstimatedPrice = l.EstimatedPrice,
                    TotalAmount = lineTotal
                });
            }

            pr.TotalEstimatedAmount = Math.Round(totalAmount, 4);
            pr.History.Add(new PurchaseRequisitionStatusHistory
            {
                Status = PurchaseRequisitionStatus.Draft,
                PreviousStatus = null,
                User = actingUser,
                Remarks = "Purchase Requisition created in Draft state.",
                Date = now
            });

            _db.PurchaseRequisitions.Add(pr);
            await _db.SaveChangesAsync(cancellationToken);

            return (await GetByIdAsync(pr.Id, cancellationToken))!;
        }

        public async Task<PurchaseRequisitionDto?> UpdateAsync(
            int id,
            PurchaseRequisitionUpdateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var pr = await _db.PurchaseRequisitions
                .Include(p => p.Lines)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);

            if (pr is null) return null;

            if (pr.Status != PurchaseRequisitionStatus.Draft && pr.Status != PurchaseRequisitionStatus.Submitted)
            {
                throw new InvalidOperationException($"Cannot update Purchase Requisition in {pr.Status} status.");
            }

            var lineTuples = request.Lines?.Select(l => (l.ItemName, l.Quantity, l.EstimatedPrice)).ToList() ?? new();
            PurchaseRequisitionRules.ValidateCreate(request.Department, request.Requestor, request.RequiredDate, lineTuples);

            var now = DateTime.UtcNow;

            pr.Department = request.Department.Trim();
            pr.Requestor = request.Requestor.Trim();
            pr.RequiredDate = request.RequiredDate.ToUniversalTime();
            pr.Priority = request.Priority;
            pr.Remarks = request.Remarks?.Trim() ?? string.Empty;
            pr.InternalNotes = request.InternalNotes?.Trim() ?? string.Empty;
            pr.UpdatedAt = now;
            pr.UpdatedBy = actingUser;

            _db.PurchaseRequisitionLines.RemoveRange(pr.Lines);
            pr.Lines.Clear();

            decimal totalAmount = 0m;
            foreach (var l in request.Lines ?? new List<PurchaseRequisitionLineDto>())
            {
                var lineTotal = Math.Round(l.Quantity * l.EstimatedPrice, 4);
                totalAmount += lineTotal;

                pr.Lines.Add(new PurchaseRequisitionLine
                {
                    PurchaseRequisitionId = id,
                    ItemName = l.ItemName.Trim(),
                    Description = l.Description?.Trim() ?? string.Empty,
                    Quantity = l.Quantity,
                    Uom = string.IsNullOrWhiteSpace(l.Uom) ? "PCS" : l.Uom.Trim(),
                    EstimatedPrice = l.EstimatedPrice,
                    TotalAmount = lineTotal
                });
            }

            pr.TotalEstimatedAmount = Math.Round(totalAmount, 4);

            await _db.SaveChangesAsync(cancellationToken);
            return (await GetByIdAsync(id, cancellationToken))!;
        }

        public async Task<bool> DeleteAsync(int id, string actingUser, CancellationToken cancellationToken = default)
        {
            var pr = await _db.PurchaseRequisitions.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
            if (pr is null) return false;

            pr.IsDeleted = true;
            pr.UpdatedAt = DateTime.UtcNow;
            pr.UpdatedBy = actingUser;

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<PurchaseRequisitionDto?> SubmitAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default)
        {
            return await ChangeStatusAsync(id, PurchaseRequisitionStatus.Submitted, remarks, actingUser, cancellationToken);
        }

        public async Task<PurchaseRequisitionDto?> ApproveAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default)
        {
            return await ChangeStatusAsync(id, PurchaseRequisitionStatus.Approved, remarks, actingUser, cancellationToken);
        }

        public async Task<PurchaseRequisitionDto?> RejectAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default)
        {
            return await ChangeStatusAsync(id, PurchaseRequisitionStatus.Rejected, remarks, actingUser, cancellationToken);
        }

        public async Task<string> GetNextPRNumberAsync(CancellationToken cancellationToken = default)
        {
            return await _numberingService.NextPRNumberAsync(cancellationToken);
        }

        private async Task<PurchaseRequisitionDto?> ChangeStatusAsync(
            int id,
            PurchaseRequisitionStatus targetStatus,
            string remarks,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var pr = await _db.PurchaseRequisitions
                .Include(p => p.History)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);

            if (pr is null) return null;

            if (!PurchaseRequisitionRules.CanTransition(pr.Status, targetStatus))
            {
                throw new InvalidOperationException($"Cannot transition Purchase Requisition from {pr.Status} to {targetStatus}.");
            }

            var prev = pr.Status;
            var now = DateTime.UtcNow;

            pr.Status = targetStatus;
            pr.UpdatedAt = now;
            pr.UpdatedBy = actingUser;

            pr.History.Add(new PurchaseRequisitionStatusHistory
            {
                PurchaseRequisitionId = id,
                Status = targetStatus,
                PreviousStatus = prev,
                User = actingUser,
                Remarks = string.IsNullOrWhiteSpace(remarks) ? $"Status updated to {targetStatus}." : remarks.Trim(),
                Date = now
            });

            await _db.SaveChangesAsync(cancellationToken);
            return (await GetByIdAsync(id, cancellationToken))!;
        }

        private static PurchaseRequisitionListItemDto MapToListItemDto(PurchaseRequisition pr) => new()
        {
            Id = pr.Id,
            PRNumber = pr.PRNumber,
            Department = pr.Department,
            Requestor = pr.Requestor,
            RequiredDate = pr.RequiredDate,
            Priority = pr.Priority,
            Status = pr.Status,
            TotalEstimatedAmount = pr.TotalEstimatedAmount,
            RFQId = pr.RFQId,
            RFQNumber = pr.RFQNumber,
            CreatedAt = pr.CreatedAt,
            CreatedBy = pr.CreatedBy
        };

        private static PurchaseRequisitionDto MapToDto(PurchaseRequisition pr) => new()
        {
            Id = pr.Id,
            PRNumber = pr.PRNumber,
            Department = pr.Department,
            Requestor = pr.Requestor,
            RequiredDate = pr.RequiredDate,
            Priority = pr.Priority,
            Status = pr.Status,
            Remarks = pr.Remarks,
            InternalNotes = pr.InternalNotes,
            TotalEstimatedAmount = pr.TotalEstimatedAmount,
            RFQId = pr.RFQId,
            RFQNumber = pr.RFQNumber,
            CreatedAt = pr.CreatedAt,
            CreatedBy = pr.CreatedBy,
            UpdatedAt = pr.UpdatedAt,
            UpdatedBy = pr.UpdatedBy,
            Lines = pr.Lines?.Select(l => new PurchaseRequisitionLineDto
            {
                Id = l.Id,
                PurchaseRequisitionId = l.PurchaseRequisitionId,
                ItemName = l.ItemName,
                Description = l.Description,
                Quantity = l.Quantity,
                Uom = l.Uom,
                EstimatedPrice = l.EstimatedPrice,
                TotalAmount = l.TotalAmount
            }).ToList() ?? new(),
            History = pr.History?.Select(h => new PurchaseRequisitionStatusHistoryDto
            {
                Id = h.Id,
                PurchaseRequisitionId = h.PurchaseRequisitionId,
                Status = h.Status,
                PreviousStatus = h.PreviousStatus,
                User = h.User,
                Remarks = h.Remarks,
                Date = h.Date
            }).ToList() ?? new()
        };
    }
}
