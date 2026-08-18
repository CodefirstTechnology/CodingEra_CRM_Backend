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
    public class GoodsReceiptService : IGoodsReceiptService
    {
        private readonly ERPDbContext _db;
        private readonly GoodsReceiptNumberingService _numberingService;

        public GoodsReceiptService(ERPDbContext db, GoodsReceiptNumberingService numberingService)
        {
            _db = db;
            _numberingService = numberingService;
        }

        public async Task<PagedResult<GoodsReceiptListItemDto>> GetAllAsync(
            GoodsReceiptListQueryDto query,
            CancellationToken cancellationToken = default)
        {
            var q = FilterQuery(_db.GoodsReceipts.AsNoTracking().Where(gr => !gr.IsDeleted), query);

            var totalCount = await q.CountAsync(cancellationToken);
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 1000);

            var items = await q
                .Include(gr => gr.Items)
                .OrderByDescending(gr => gr.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var dtos = items.Select(MapToListItemDto).ToList();
            return PagedResult<GoodsReceiptListItemDto>.Create(dtos, totalCount, page, pageSize);
        }

        public async Task<GoodsReceiptDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var gr = await _db.GoodsReceipts
                .Include(g => g.Items)
                .Include(g => g.History.OrderByDescending(h => h.Date))
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted, cancellationToken);

            return gr is null ? null : MapToDto(gr);
        }

        public async Task<PagedResult<GoodsReceiptListItemDto>> GetByPurchaseOrderAsync(
            int purchaseOrderId,
            CancellationToken cancellationToken = default)
        {
            var items = await _db.GoodsReceipts
                .Include(g => g.Items)
                .AsNoTracking()
                .Where(g => g.PurchaseOrderId == purchaseOrderId && !g.IsDeleted)
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync(cancellationToken);

            var dtos = items.Select(MapToListItemDto).ToList();
            return PagedResult<GoodsReceiptListItemDto>.Create(dtos, dtos.Count, 1, Math.Max(1, dtos.Count));
        }

        public async Task<List<GoodsReceiptItemDto>> BuildDraftItemsForPurchaseOrderAsync(
            int purchaseOrderId,
            int? excludeGrnId = null,
            CancellationToken cancellationToken = default)
        {
            var po = await _db.PurchaseOrders
                .Include(p => p.Lines)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == purchaseOrderId && !p.IsDeleted, cancellationToken);

            if (po is null)
            {
                throw new InvalidOperationException($"Purchase order with ID {purchaseOrderId} not found.");
            }

            var completedGrnQuery = _db.GoodsReceiptItems
                .AsNoTracking()
                .Where(gri => gri.GoodsReceipt!.PurchaseOrderId == purchaseOrderId &&
                              gri.GoodsReceipt.Status == GoodsReceiptStatus.Completed &&
                              !gri.GoodsReceipt.IsDeleted);

            if (excludeGrnId.HasValue && excludeGrnId.Value > 0)
            {
                completedGrnQuery = completedGrnQuery.Where(gri => gri.GoodsReceiptId != excludeGrnId.Value);
            }

            var prevReceivedMap = await completedGrnQuery
                .GroupBy(gri => gri.PurchaseOrderLineId)
                .Select(g => new { LineId = g.Key, TotalReceived = g.Sum(x => x.ReceivedQuantity) })
                .ToDictionaryAsync(x => x.LineId, x => x.TotalReceived, cancellationToken);

            var result = new List<GoodsReceiptItemDto>();
            foreach (var l in po.Lines)
            {
                var prev = prevReceivedMap.TryGetValue(l.Id, out var pQty) ? pQty : 0m;
                var remaining = Math.Max(0m, Math.Round(l.Quantity - prev, 4));

                result.Add(new GoodsReceiptItemDto
                {
                    Id = $"tmp-{l.Id}",
                    PurchaseOrderLineId = l.Id.ToString(),
                    ItemName = l.ItemName,
                    Description = l.Description,
                    Unit = string.IsNullOrWhiteSpace(l.Unit) ? "Nos" : l.Unit,
                    OrderedQuantity = l.Quantity,
                    PreviouslyReceivedQuantity = prev,
                    RemainingQuantity = remaining,
                    ReceivedQuantity = remaining,
                    RejectedQuantity = 0m,
                    Remarks = string.Empty
                });
            }

            return result;
        }

        public async Task<GoodsReceiptDto> CreateAsync(
            GoodsReceiptCreateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var po = await _db.PurchaseOrders
                .Include(p => p.Lines)
                .FirstOrDefaultAsync(p => p.Id == request.PurchaseOrderId && !p.IsDeleted, cancellationToken);

            if (po is null)
            {
                throw new InvalidOperationException($"Purchase Order with ID {request.PurchaseOrderId} not found.");
            }

            var now = DateTime.UtcNow;
            var grnNumber = await _numberingService.NextGRNNumberAsync(cancellationToken);

            var draftItems = await BuildDraftItemsForPurchaseOrderAsync(request.PurchaseOrderId, null, cancellationToken);
            var draftItemMap = draftItems.ToDictionary(x => x.PurchaseOrderLineId, x => x);

            var gr = new GoodsReceipt
            {
                GRNNumber = grnNumber,
                PurchaseOrderId = po.Id,
                PurchaseOrderNumber = po.PurchaseOrderNumber,
                VendorName = po.VendorName,
                ReceiptDate = request.ReceiptDate.ToUniversalTime(),
                Warehouse = string.IsNullOrWhiteSpace(request.Warehouse) ? "Main Store — Sanand" : request.Warehouse.Trim(),
                Status = request.Status,
                Remarks = request.Remarks?.Trim() ?? string.Empty,
                Notes = request.Notes?.Trim() ?? string.Empty,
                CreatedAt = now,
                CreatedBy = actingUser,
                UpdatedAt = now,
                UpdatedBy = actingUser
            };

            foreach (var reqItem in request.Items ?? new List<GoodsReceiptItemDto>())
            {
                var poLineIdStr = reqItem.PurchaseOrderLineId;
                if (!int.TryParse(poLineIdStr, out var poLineIdInt))
                {
                    var matchingLine = po.Lines.FirstOrDefault(l => l.ItemName.Equals(reqItem.ItemName, StringComparison.OrdinalIgnoreCase));
                    if (matchingLine != null) poLineIdInt = matchingLine.Id;
                }

                draftItemMap.TryGetValue(poLineIdInt.ToString(), out var draftInfo);

                var ordered = draftInfo?.OrderedQuantity ?? reqItem.OrderedQuantity;
                var prev = draftInfo?.PreviouslyReceivedQuantity ?? reqItem.PreviouslyReceivedQuantity;
                var remaining = Math.Max(0m, Math.Round(ordered - prev, 4));

                gr.Items.Add(new GoodsReceiptItem
                {
                    PurchaseOrderLineId = poLineIdInt,
                    ItemName = reqItem.ItemName.Trim(),
                    Description = reqItem.Description?.Trim() ?? string.Empty,
                    Unit = string.IsNullOrWhiteSpace(reqItem.Unit) ? "Nos" : reqItem.Unit.Trim(),
                    OrderedQuantity = ordered,
                    PreviouslyReceivedQuantity = prev,
                    RemainingQuantity = remaining,
                    ReceivedQuantity = reqItem.ReceivedQuantity,
                    RejectedQuantity = reqItem.RejectedQuantity,
                    Remarks = reqItem.Remarks?.Trim() ?? string.Empty
                });
            }

            var valErr = GoodsReceiptRules.ValidateItems(gr.Items);
            if (valErr != null)
            {
                throw new InvalidOperationException(valErr);
            }

            gr.History.Add(new GoodsReceiptStatusHistory
            {
                Status = gr.Status,
                PreviousStatus = null,
                Date = now,
                User = actingUser,
                Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? "Goods receipt created." : request.Remarks.Trim(),
                Label = "Created"
            });

            _db.GoodsReceipts.Add(gr);
            await _db.SaveChangesAsync(cancellationToken);

            if (gr.Status == GoodsReceiptStatus.Completed)
            {
                await SyncPurchaseOrderReceivingStatusAsync(po.Id, actingUser, cancellationToken);
            }

            return (await GetByIdAsync(gr.Id, cancellationToken))!;
        }

        public async Task<GoodsReceiptDto?> UpdateAsync(
            int id,
            GoodsReceiptUpdateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var gr = await _db.GoodsReceipts
                .Include(g => g.Items)
                .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted, cancellationToken);

            if (gr is null) return null;

            if (gr.Status == GoodsReceiptStatus.Completed)
            {
                throw new InvalidOperationException("Completed goods receipt cannot be edited.");
            }

            var now = DateTime.UtcNow;

            gr.ReceiptDate = request.ReceiptDate.ToUniversalTime();
            gr.Warehouse = string.IsNullOrWhiteSpace(request.Warehouse) ? gr.Warehouse : request.Warehouse.Trim();
            gr.Notes = request.Notes?.Trim() ?? gr.Notes;
            gr.Remarks = request.Remarks?.Trim() ?? gr.Remarks;
            gr.UpdatedAt = now;
            gr.UpdatedBy = actingUser;

            var draftItems = await BuildDraftItemsForPurchaseOrderAsync(gr.PurchaseOrderId, gr.Id, cancellationToken);
            var draftItemMap = draftItems.ToDictionary(x => x.PurchaseOrderLineId, x => x);

            _db.GoodsReceiptItems.RemoveRange(gr.Items);
            gr.Items.Clear();

            foreach (var reqItem in request.Items ?? new List<GoodsReceiptItemDto>())
            {
                var poLineIdStr = reqItem.PurchaseOrderLineId;
                if (!int.TryParse(poLineIdStr, out var poLineIdInt))
                {
                    var po = await _db.PurchaseOrders.Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == gr.PurchaseOrderId, cancellationToken);
                    var matchingLine = po?.Lines.FirstOrDefault(l => l.ItemName.Equals(reqItem.ItemName, StringComparison.OrdinalIgnoreCase));
                    if (matchingLine != null) poLineIdInt = matchingLine.Id;
                }

                draftItemMap.TryGetValue(poLineIdInt.ToString(), out var draftInfo);

                var ordered = draftInfo?.OrderedQuantity ?? reqItem.OrderedQuantity;
                var prev = draftInfo?.PreviouslyReceivedQuantity ?? reqItem.PreviouslyReceivedQuantity;
                var remaining = Math.Max(0m, Math.Round(ordered - prev, 4));

                gr.Items.Add(new GoodsReceiptItem
                {
                    GoodsReceiptId = id,
                    PurchaseOrderLineId = poLineIdInt,
                    ItemName = reqItem.ItemName.Trim(),
                    Description = reqItem.Description?.Trim() ?? string.Empty,
                    Unit = string.IsNullOrWhiteSpace(reqItem.Unit) ? "Nos" : reqItem.Unit.Trim(),
                    OrderedQuantity = ordered,
                    PreviouslyReceivedQuantity = prev,
                    RemainingQuantity = remaining,
                    ReceivedQuantity = reqItem.ReceivedQuantity,
                    RejectedQuantity = reqItem.RejectedQuantity,
                    Remarks = reqItem.Remarks?.Trim() ?? string.Empty
                });
            }

            var valErr = GoodsReceiptRules.ValidateItems(gr.Items);
            if (valErr != null)
            {
                throw new InvalidOperationException(valErr);
            }

            await _db.SaveChangesAsync(cancellationToken);
            return (await GetByIdAsync(id, cancellationToken))!;
        }

        public async Task<bool> DeleteAsync(int id, string actingUser, CancellationToken cancellationToken = default)
        {
            var gr = await _db.GoodsReceipts.FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted, cancellationToken);
            if (gr is null) return false;

            gr.IsDeleted = true;
            gr.UpdatedAt = DateTime.UtcNow;
            gr.UpdatedBy = actingUser;

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<GoodsReceiptDto?> UpdateStatusAsync(
            int id,
            GoodsReceiptStatusUpdateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            return await ChangeStatusAsync(id, request.Status, request.Remarks ?? string.Empty, actingUser, cancellationToken);
        }

        public async Task<GoodsReceiptDto?> CancelAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default)
        {
            return await ChangeStatusAsync(id, GoodsReceiptStatus.Cancelled, remarks, actingUser, cancellationToken);
        }

        public async Task<List<GoodsReceiptHistoryDto>> GetStatusHistoryAsync(int id, CancellationToken cancellationToken = default)
        {
            var list = await _db.GoodsReceiptStatusHistories
                .AsNoTracking()
                .Where(h => h.GoodsReceiptId == id)
                .OrderByDescending(h => h.Date)
                .ToListAsync(cancellationToken);

            return list.Select(MapToHistoryDto).ToList();
        }

        public async Task<string> GetNextNumberAsync(CancellationToken cancellationToken = default)
        {
            return await _numberingService.NextGRNNumberAsync(cancellationToken);
        }

        private async Task<GoodsReceiptDto?> ChangeStatusAsync(
            int id,
            GoodsReceiptStatus targetStatus,
            string remarks,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var gr = await _db.GoodsReceipts
                .Include(g => g.Items)
                .Include(g => g.History)
                .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted, cancellationToken);

            if (gr is null) return null;

            if (!GoodsReceiptRules.CanTransition(gr.Status, targetStatus))
            {
                throw new InvalidOperationException($"Cannot transition goods receipt status from {gr.Status} to {targetStatus}.");
            }

            if (targetStatus == GoodsReceiptStatus.Completed)
            {
                var valErr = GoodsReceiptRules.ValidateItems(gr.Items);
                if (valErr != null)
                {
                    throw new InvalidOperationException(valErr);
                }
            }

            var prev = gr.Status;
            var now = DateTime.UtcNow;

            gr.Status = targetStatus;
            gr.UpdatedAt = now;
            gr.UpdatedBy = actingUser;

            var rmk = string.IsNullOrWhiteSpace(remarks) ? $"Status updated to {targetStatus}." : remarks.Trim();

            gr.History.Add(new GoodsReceiptStatusHistory
            {
                GoodsReceiptId = id,
                Status = targetStatus,
                PreviousStatus = prev,
                Date = now,
                User = actingUser,
                Remarks = rmk,
                Label = targetStatus.ToString()
            });

            await _db.SaveChangesAsync(cancellationToken);

            if (targetStatus == GoodsReceiptStatus.Completed)
            {
                await SyncPurchaseOrderReceivingStatusAsync(gr.PurchaseOrderId, actingUser, cancellationToken);
            }

            return (await GetByIdAsync(id, cancellationToken))!;
        }

        private async Task SyncPurchaseOrderReceivingStatusAsync(int purchaseOrderId, string actingUser, CancellationToken cancellationToken)
        {
            var po = await _db.PurchaseOrders.Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == purchaseOrderId && !p.IsDeleted, cancellationToken);
            if (po is null) return;

            var grnItems = await _db.GoodsReceiptItems
                .AsNoTracking()
                .Where(gri => gri.GoodsReceipt!.PurchaseOrderId == purchaseOrderId &&
                              gri.GoodsReceipt.Status == GoodsReceiptStatus.Completed &&
                              !gri.GoodsReceipt.IsDeleted)
                .ToListAsync(cancellationToken);

            var totalOrdered = po.Lines.Sum(l => l.Quantity);
            var totalReceived = grnItems.Sum(gri => gri.ReceivedQuantity);

            if (totalReceived <= 0) return;

            var now = DateTime.UtcNow;
            var targetPoStatus = totalReceived >= totalOrdered ? PurchaseOrderStatus.Completed : PurchaseOrderStatus.PartiallyReceived;

            if (po.Status != targetPoStatus && (po.Status == PurchaseOrderStatus.Ordered || po.Status == PurchaseOrderStatus.Approved || po.Status == PurchaseOrderStatus.PartiallyReceived))
            {
                var prevPoStatus = po.Status;
                po.Status = targetPoStatus;
                po.UpdatedAt = now;
                po.UpdatedBy = actingUser;

                _db.PurchaseOrderStatusHistories.Add(new PurchaseOrderStatusHistory
                {
                    PurchaseOrderId = po.Id,
                    Status = targetPoStatus,
                    PreviousStatus = prevPoStatus,
                    Date = now,
                    User = actingUser,
                    Remarks = $"Automatic status update from Goods Receipt completion. Received: {totalReceived} / {totalOrdered}.",
                    Label = targetPoStatus.ToString()
                });

                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        private static IQueryable<GoodsReceipt> FilterQuery(IQueryable<GoodsReceipt> q, GoodsReceiptListQueryDto query)
        {
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim().ToLower();
                q = q.Where(gr =>
                    gr.GRNNumber.ToLower().Contains(term) ||
                    gr.PurchaseOrderNumber.ToLower().Contains(term) ||
                    gr.VendorName.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<GoodsReceiptStatus>(query.Status, true, out var statusEnum))
            {
                q = q.Where(gr => gr.Status == statusEnum);
            }

            if (query.PurchaseOrderId.HasValue && query.PurchaseOrderId.Value > 0)
            {
                q = q.Where(gr => gr.PurchaseOrderId == query.PurchaseOrderId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.VendorName))
            {
                var vTerm = query.VendorName.Trim().ToLower();
                q = q.Where(gr => gr.VendorName.ToLower().Contains(vTerm));
            }

            if (query.DateFrom.HasValue)
            {
                q = q.Where(gr => gr.ReceiptDate >= query.DateFrom.Value.ToUniversalTime());
            }

            if (query.DateTo.HasValue)
            {
                q = q.Where(gr => gr.ReceiptDate <= query.DateTo.Value.ToUniversalTime());
            }

            return q;
        }

        private static GoodsReceiptListItemDto MapToListItemDto(GoodsReceipt gr)
        {
            var (ordered, received, _, _, pct) = GoodsReceiptRules.CalculateSummary(gr.Items);
            return new GoodsReceiptListItemDto
            {
                Id = gr.Id,
                GRNNumber = gr.GRNNumber,
                PurchaseOrderNumber = gr.PurchaseOrderNumber,
                PurchaseOrderId = gr.PurchaseOrderId,
                VendorName = gr.VendorName,
                ReceiptDate = gr.ReceiptDate,
                Status = gr.Status,
                ReceivedPercent = pct,
                CreatedBy = gr.CreatedBy
            };
        }

        private static GoodsReceiptDto MapToDto(GoodsReceipt gr)
        {
            var (ordered, received, remaining, rejected, pct) = GoodsReceiptRules.CalculateSummary(gr.Items);
            return new GoodsReceiptDto
            {
                Id = gr.Id,
                GRNNumber = gr.GRNNumber,
                PurchaseOrderId = gr.PurchaseOrderId,
                PurchaseOrderNumber = gr.PurchaseOrderNumber,
                VendorName = gr.VendorName,
                ReceiptDate = gr.ReceiptDate,
                Warehouse = gr.Warehouse,
                Status = gr.Status,
                Remarks = gr.Remarks,
                Notes = gr.Notes,
                CreatedBy = gr.CreatedBy,
                CreatedAt = gr.CreatedAt,
                UpdatedBy = gr.UpdatedBy,
                UpdatedAt = gr.UpdatedAt,
                Items = gr.Items?.Select(i => new GoodsReceiptItemDto
                {
                    Id = i.Id.ToString(),
                    PurchaseOrderLineId = i.PurchaseOrderLineId.ToString(),
                    ItemName = i.ItemName,
                    Description = i.Description,
                    Unit = i.Unit,
                    OrderedQuantity = i.OrderedQuantity,
                    PreviouslyReceivedQuantity = i.PreviouslyReceivedQuantity,
                    RemainingQuantity = i.RemainingQuantity,
                    ReceivedQuantity = i.ReceivedQuantity,
                    RejectedQuantity = i.RejectedQuantity,
                    Remarks = i.Remarks
                }).ToList() ?? new(),
                Summary = new GoodsReceiptSummaryDto
                {
                    OrderedQuantity = ordered,
                    ReceivedQuantity = received,
                    RemainingQuantity = remaining,
                    RejectedQuantity = rejected,
                    ReceiptPercentage = pct
                },
                History = gr.History?.Select(MapToHistoryDto).ToList() ?? new()
            };
        }

        private static GoodsReceiptHistoryDto MapToHistoryDto(GoodsReceiptStatusHistory h) => new()
        {
            Id = h.Id.ToString(),
            Status = h.Status,
            PreviousStatus = h.PreviousStatus,
            Date = h.Date,
            User = h.User,
            Remarks = h.Remarks,
            Label = string.IsNullOrWhiteSpace(h.Label) ? h.Status.ToString() : h.Label
        };
    }
}
