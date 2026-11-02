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
        private readonly IInventoryValuationEngine _valuationEngine;

        public GoodsReceiptService(
            ERPDbContext db, 
            GoodsReceiptNumberingService numberingService,
            IInventoryValuationEngine valuationEngine)
        {
            _db = db;
            _numberingService = numberingService;
            _valuationEngine = valuationEngine;
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
                              (gri.GoodsReceipt.Status == GoodsReceiptStatus.Completed || gri.GoodsReceipt.Status == GoodsReceiptStatus.Posted) &&
                              !gri.GoodsReceipt.IsDeleted);

            if (excludeGrnId.HasValue && excludeGrnId.Value > 0)
            {
                completedGrnQuery = completedGrnQuery.Where(gri => gri.GoodsReceiptId != excludeGrnId.Value);
            }

            var prevReceivedMap = await completedGrnQuery
                .GroupBy(gri => gri.PurchaseOrderLineId)
                .Select(g => new { LineId = g.Key, TotalReceived = g.Sum(x => x.AcceptedQuantity > 0 ? x.AcceptedQuantity : x.ReceivedQuantity) })
                .ToDictionaryAsync(x => x.LineId, x => x.TotalReceived, cancellationToken);

            var result = new List<GoodsReceiptItemDto>();
            foreach (var l in po.Lines)
            {
                var prev = prevReceivedMap.TryGetValue(l.Id, out var pQty) ? pQty : 0m;
                var remaining = Math.Max(0m, Math.Round(l.Quantity - prev, 4));

                if (remaining <= 0) continue;

                result.Add(new GoodsReceiptItemDto
                {
                    Id = $"tmp-{l.Id}",
                    PurchaseOrderLineId = l.Id.ToString(),
                    ItemId = l.ItemId,
                    ItemName = l.ItemName,
                    Description = l.Description,
                    Unit = string.IsNullOrWhiteSpace(l.Unit) ? "Nos" : l.Unit,
                    OrderedQuantity = l.Quantity,
                    PreviouslyReceivedQuantity = prev,
                    RemainingQuantity = remaining,
                    ReceivedQuantity = remaining,
                    AcceptedQuantity = remaining,
                    RejectedQuantity = 0m,
                    PoRate = l.Rate,
                    LandedCostPerUnit = l.Rate,
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

            decimal? gross = request.WeighbridgeGrossWeight;
            decimal? tare = request.WeighbridgeTareWeight;
            decimal? net = (gross.HasValue && tare.HasValue) ? Math.Max(0m, gross.Value - tare.Value) : null;

            var gr = new GoodsReceipt
            {
                GRNNumber = grnNumber,
                PurchaseOrderId = po.Id,
                PurchaseOrderNumber = po.PurchaseOrderNumber,
                VendorId = po.VendorId,
                VendorName = po.VendorName,
                ReceiptDate = request.ReceiptDate.ToUniversalTime(),
                Warehouse = string.IsNullOrWhiteSpace(request.Warehouse) ? "Main Store — Sanand" : request.Warehouse.Trim(),
                Status = request.Status,
                GateInwardNumber = request.GateInwardNumber,
                VehicleNumber = request.VehicleNumber,
                TransporterName = request.TransporterName,
                LRNumber = request.LRNumber,
                EWayBillNumber = request.EWayBillNumber,
                WeighbridgeGrossWeight = gross,
                WeighbridgeTareWeight = tare,
                WeighbridgeNetWeight = net,
                InspectionLotNumber = $"LOT-GRN-{grnNumber}",
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
                var accepted = reqItem.AcceptedQuantity > 0 ? reqItem.AcceptedQuantity : Math.Max(0m, Math.Round(reqItem.ReceivedQuantity - reqItem.RejectedQuantity, 4));
                var poRate = draftInfo?.PoRate ?? reqItem.PoRate;
                var landedCost = reqItem.LandedCostPerUnit > 0 ? reqItem.LandedCostPerUnit : poRate;

                // Over-delivery tolerance check: discrete 0%, bulk max 2%
                var poLine = po.Lines.FirstOrDefault(l => l.Id == poLineIdInt);
                decimal allowedTolerance = (poLine != null && poLine.OverDeliveryTolerancePercent > 0)
                    ? poLine.OverDeliveryTolerancePercent / 100m
                    : 0.02m; // max 2% bulk allowance

                if (reqItem.ReceivedQuantity > remaining * (1m + allowedTolerance))
                {
                    throw new InvalidOperationException($"Over-delivery rejected for item '{reqItem.ItemName}'. Remaining deliverable: {remaining:N2}, Allowed limit: {remaining * (1m + allowedTolerance):N2}, Received: {reqItem.ReceivedQuantity:N2}.");
                }

                gr.Items.Add(new GoodsReceiptItem
                {
                    PurchaseOrderLineId = poLineIdInt,
                    ItemId = reqItem.ItemId,
                    ItemName = reqItem.ItemName.Trim(),
                    Description = reqItem.Description?.Trim() ?? string.Empty,
                    Unit = string.IsNullOrWhiteSpace(reqItem.Unit) ? "Nos" : reqItem.Unit.Trim(),
                    OrderedQuantity = ordered,
                    PreviouslyReceivedQuantity = prev,
                    RemainingQuantity = remaining,
                    ReceivedQuantity = reqItem.ReceivedQuantity,
                    AcceptedQuantity = accepted,
                    RejectedQuantity = reqItem.RejectedQuantity,
                    BatchNumber = reqItem.BatchNumber,
                    HeatNumber = reqItem.HeatNumber,
                    MtcNumber = reqItem.MtcNumber,
                    ExpiryDate = reqItem.ExpiryDate?.ToUniversalTime(),
                    PoRate = poRate,
                    LandedCostPerUnit = landedCost,
                    ValuationAmount = Math.Round(accepted * landedCost, 2, MidpointRounding.AwayFromZero),
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

            using (var tx = await _db.Database.BeginTransactionAsync(cancellationToken))
            {
                _db.GoodsReceipts.Add(gr);
                await _db.SaveChangesAsync(cancellationToken);

                if (gr.Status == GoodsReceiptStatus.Completed || gr.Status == GoodsReceiptStatus.Posted)
                {
                    await SyncPurchaseOrderReceivingStatusAsync(po.Id, actingUser, cancellationToken);
                }

                await tx.CommitAsync(cancellationToken);
            }

            return (await GetByIdAsync(gr.Id, cancellationToken))!;
        }

        public async Task<GoodsReceiptDto> ProcessGateInwardAsync(
            GateInwardRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var po = await _db.PurchaseOrders
                .Include(p => p.Lines)
                .FirstOrDefaultAsync(p => p.Id == request.PurchaseOrderId && !p.IsDeleted, cancellationToken);

            if (po == null)
            {
                throw new KeyNotFoundException($"Purchase Order ID {request.PurchaseOrderId} not found.");
            }

            var now = DateTime.UtcNow;
            var grnNumber = await _numberingService.NextGRNNumberAsync(cancellationToken);
            var gateNumber = $"GATE-IN-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}";

            decimal? gross = request.WeighbridgeGrossWeight;
            decimal? tare = request.WeighbridgeTareWeight;
            decimal? net = (gross.HasValue && tare.HasValue) ? Math.Max(0m, gross.Value - tare.Value) : null;

            var gr = new GoodsReceipt
            {
                GRNNumber = grnNumber,
                PurchaseOrderId = po.Id,
                PurchaseOrderNumber = po.PurchaseOrderNumber,
                VendorId = po.VendorId,
                VendorName = po.VendorName,
                ReceiptDate = now,
                Warehouse = string.IsNullOrWhiteSpace(request.Warehouse) ? "Main Store — Sanand" : request.Warehouse.Trim(),
                Status = GoodsReceiptStatus.Submitted,
                QualityStatus = QualityInspectionStatus.UnderInspection,
                GateInwardNumber = gateNumber,
                GateInwardTime = now,
                VehicleNumber = request.VehicleNumber,
                TransporterName = request.TransporterName,
                LRNumber = request.LRNumber,
                EWayBillNumber = request.EWayBillNumber,
                WeighbridgeGrossWeight = gross,
                WeighbridgeTareWeight = tare,
                WeighbridgeNetWeight = net,
                InspectionLotNumber = $"LOT-GRN-{grnNumber}",
                Remarks = request.Remarks?.Trim() ?? "Gate inward manifest recorded.",
                CreatedAt = now,
                CreatedBy = actingUser,
                UpdatedAt = now,
                UpdatedBy = actingUser
            };

            foreach (var itemReq in request.Items)
            {
                var poLine = po.Lines.FirstOrDefault(l => l.Id == itemReq.PurchaseOrderLineId);
                decimal ordered = poLine?.Quantity ?? itemReq.ReceivedQuantity;
                decimal poRate = poLine?.Rate ?? 0m;

                gr.Items.Add(new GoodsReceiptItem
                {
                    PurchaseOrderLineId = itemReq.PurchaseOrderLineId,
                    ItemId = poLine?.ItemId,
                    ItemName = poLine?.ItemName ?? "Material Item",
                    Description = poLine?.Description ?? string.Empty,
                    Unit = poLine?.Unit ?? "Nos",
                    OrderedQuantity = ordered,
                    ReceivedQuantity = itemReq.ReceivedQuantity,
                    AcceptedQuantity = 0m,
                    RejectedQuantity = 0m,
                    BatchNumber = itemReq.BatchNumber,
                    HeatNumber = itemReq.HeatNumber,
                    MtcNumber = itemReq.MtcNumber,
                    ExpiryDate = itemReq.ExpiryDate?.ToUniversalTime(),
                    PoRate = poRate,
                    LandedCostPerUnit = poRate,
                    Remarks = itemReq.Remarks ?? string.Empty
                });
            }

            gr.History.Add(new GoodsReceiptStatusHistory
            {
                Status = gr.Status,
                PreviousStatus = null,
                Date = now,
                User = actingUser,
                Remarks = $"Gate Inward manifest logged. Vehicle: {request.VehicleNumber}, Weighbridge Net: {net:N2} kg.",
                Label = "GateInward"
            });

            _db.GoodsReceipts.Add(gr);
            await _db.SaveChangesAsync(cancellationToken);

            return (await GetByIdAsync(gr.Id, cancellationToken))!;
        }

        public async Task<GoodsReceiptDto> ProcessQcDispositionAsync(
            QcDispositionRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var gr = await _db.GoodsReceipts
                .Include(g => g.Items)
                .FirstOrDefaultAsync(g => g.Id == request.GoodsReceiptId && !g.IsDeleted, cancellationToken);

            if (gr == null)
            {
                throw new KeyNotFoundException($"Goods Receipt ID {request.GoodsReceiptId} not found.");
            }

            var now = DateTime.UtcNow;
            gr.QualityStatus = request.OverallStatus;
            gr.InspectionLotNumber = string.IsNullOrWhiteSpace(request.InspectionLotNumber) ? gr.InspectionLotNumber : request.InspectionLotNumber;
            gr.UpdatedAt = now;
            gr.UpdatedBy = actingUser;

            decimal totalAccepted = 0m;
            foreach (var itemDisp in request.Items)
            {
                var item = gr.Items.FirstOrDefault(i => i.Id == itemDisp.GoodsReceiptItemId);
                if (item == null) continue;

                if (itemDisp.QcAcceptedQuantity + itemDisp.QcRejectedQuantity + itemDisp.QcReworkQuantity > item.ReceivedQuantity)
                {
                    throw new InvalidOperationException($"QC disposition quantities for '{item.ItemName}' exceed total received quantity ({item.ReceivedQuantity:N2}).");
                }

                item.QcInspectedQuantity = itemDisp.QcInspectedQuantity > 0 ? itemDisp.QcInspectedQuantity : item.ReceivedQuantity;
                item.QcAcceptedQuantity = itemDisp.QcAcceptedQuantity;
                item.QcRejectedQuantity = itemDisp.QcRejectedQuantity;
                item.QcReworkQuantity = itemDisp.QcReworkQuantity;
                item.AcceptedQuantity = itemDisp.QcAcceptedQuantity;
                item.RejectedQuantity = itemDisp.QcRejectedQuantity;
                item.RejectionReasonCode = itemDisp.RejectionReasonCode ?? string.Empty;
                item.QcDispositionNotes = itemDisp.QcDispositionNotes;
                if (!string.IsNullOrWhiteSpace(itemDisp.BatchNumber)) item.BatchNumber = itemDisp.BatchNumber;
                if (!string.IsNullOrWhiteSpace(itemDisp.HeatNumber)) item.HeatNumber = itemDisp.HeatNumber;
                if (!string.IsNullOrWhiteSpace(itemDisp.MtcNumber)) item.MtcNumber = itemDisp.MtcNumber;
                if (itemDisp.ExpiryDate.HasValue) item.ExpiryDate = itemDisp.ExpiryDate.Value.ToUniversalTime();

                totalAccepted += item.AcceptedQuantity;
            }

            gr.Status = GoodsReceiptStatus.QcCompleted;
            gr.History.Add(new GoodsReceiptStatusHistory
            {
                Status = gr.Status,
                PreviousStatus = GoodsReceiptStatus.Submitted,
                Date = now,
                User = actingUser,
                Remarks = $"QC Disposition recorded. Result: {request.OverallStatus}, Accepted Qty: {totalAccepted:N2}.",
                Label = "QcDisposition"
            });

            await _db.SaveChangesAsync(cancellationToken);
            return (await GetByIdAsync(gr.Id, cancellationToken))!;
        }

        public async Task<InventoryValuationResultDto> PostToInventoryAsync(
            int goodsReceiptId,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var gr = await _db.GoodsReceipts
                .Include(g => g.Items)
                .FirstOrDefaultAsync(g => g.Id == goodsReceiptId && !g.IsDeleted, cancellationToken);

            if (gr == null)
            {
                throw new KeyNotFoundException($"Goods Receipt ID {goodsReceiptId} not found.");
            }

            var result = await _valuationEngine.ProcessGrnInventoryValuationAsync(goodsReceiptId, actingUser, cancellationToken);

            // Rollup PO receiving status
            await SyncPurchaseOrderReceivingStatusAsync(gr.PurchaseOrderId, actingUser, cancellationToken);

            return result;
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

            if (gr.Status == GoodsReceiptStatus.Completed || gr.Status == GoodsReceiptStatus.Posted)
            {
                throw new InvalidOperationException("Completed or posted goods receipt cannot be edited.");
            }

            var now = DateTime.UtcNow;
            gr.ReceiptDate = request.ReceiptDate.ToUniversalTime();
            gr.Warehouse = string.IsNullOrWhiteSpace(request.Warehouse) ? gr.Warehouse : request.Warehouse.Trim();
            gr.VehicleNumber = request.VehicleNumber ?? gr.VehicleNumber;
            gr.TransporterName = request.TransporterName ?? gr.TransporterName;
            gr.LRNumber = request.LRNumber ?? gr.LRNumber;
            gr.EWayBillNumber = request.EWayBillNumber ?? gr.EWayBillNumber;
            gr.WeighbridgeGrossWeight = request.WeighbridgeGrossWeight ?? gr.WeighbridgeGrossWeight;
            gr.WeighbridgeTareWeight = request.WeighbridgeTareWeight ?? gr.WeighbridgeTareWeight;
            if (gr.WeighbridgeGrossWeight.HasValue && gr.WeighbridgeTareWeight.HasValue)
            {
                gr.WeighbridgeNetWeight = Math.Max(0m, gr.WeighbridgeGrossWeight.Value - gr.WeighbridgeTareWeight.Value);
            }

            gr.Notes = request.Notes?.Trim() ?? gr.Notes;
            gr.Remarks = request.Remarks?.Trim() ?? gr.Remarks;
            gr.UpdatedAt = now;
            gr.UpdatedBy = actingUser;

            await _db.SaveChangesAsync(cancellationToken);
            return await GetByIdAsync(id, cancellationToken);
        }

        public async Task<bool> DeleteAsync(int id, string actingUser, CancellationToken cancellationToken = default)
        {
            var gr = await _db.GoodsReceipts.FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted, cancellationToken);
            if (gr is null) return false;

            if (gr.Status == GoodsReceiptStatus.Completed || gr.Status == GoodsReceiptStatus.Posted)
            {
                throw new InvalidOperationException("Completed goods receipt cannot be deleted.");
            }

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
            var gr = await _db.GoodsReceipts
                .Include(g => g.Items)
                .Include(g => g.History)
                .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted, cancellationToken);

            if (gr is null) return null;

            var oldStatus = gr.Status;
            gr.Status = request.Status;
            gr.UpdatedAt = DateTime.UtcNow;
            gr.UpdatedBy = actingUser;

            gr.History.Add(new GoodsReceiptStatusHistory
            {
                GoodsReceiptId = gr.Id,
                Status = gr.Status,
                PreviousStatus = oldStatus,
                Date = DateTime.UtcNow,
                User = actingUser,
                Remarks = request.Remarks?.Trim() ?? string.Empty,
                Label = request.Status.ToString()
            });

            await _db.SaveChangesAsync(cancellationToken);

            if (gr.Status == GoodsReceiptStatus.Completed || gr.Status == GoodsReceiptStatus.Posted)
            {
                await SyncPurchaseOrderReceivingStatusAsync(gr.PurchaseOrderId, actingUser, cancellationToken);
            }

            return await GetByIdAsync(id, cancellationToken);
        }

        public async Task<GoodsReceiptDto?> CancelAsync(
            int id,
            string remarks,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            return await UpdateStatusAsync(
                id,
                new GoodsReceiptStatusUpdateRequestDto { Status = GoodsReceiptStatus.Cancelled, Remarks = remarks },
                actingUser,
                cancellationToken);
        }

        public async Task<List<GoodsReceiptHistoryDto>> GetStatusHistoryAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _db.GoodsReceiptStatusHistories
                .AsNoTracking()
                .Where(h => h.GoodsReceiptId == id)
                .OrderByDescending(h => h.Date)
                .Select(h => MapToHistoryDto(h))
                .ToListAsync(cancellationToken);
        }

        public async Task<string> GetNextNumberAsync(CancellationToken cancellationToken = default)
        {
            return await _numberingService.NextGRNNumberAsync(cancellationToken);
        }

        private async Task SyncPurchaseOrderReceivingStatusAsync(int poId, string actingUser, CancellationToken cancellationToken)
        {
            var po = await _db.PurchaseOrders
                .Include(p => p.Lines)
                .FirstOrDefaultAsync(p => p.Id == poId && !p.IsDeleted, cancellationToken);

            if (po is null) return;

            var activeGrns = await _db.GoodsReceipts
                .Include(g => g.Items)
                .Where(g => g.PurchaseOrderId == poId && 
                            (g.Status == GoodsReceiptStatus.Completed || g.Status == GoodsReceiptStatus.Posted) && 
                            !g.IsDeleted)
                .ToListAsync(cancellationToken);

            var lineReceivedMap = activeGrns
                .SelectMany(g => g.Items)
                .GroupBy(i => i.PurchaseOrderLineId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.AcceptedQuantity > 0 ? x.AcceptedQuantity : x.ReceivedQuantity));

            bool anyReceived = false;
            bool allFullyReceived = true;

            foreach (var line in po.Lines)
            {
                var totalRec = lineReceivedMap.TryGetValue(line.Id, out var rec) ? rec : 0m;
                line.ReceivedQuantity = totalRec;

                if (totalRec > 0) anyReceived = true;
                if (totalRec < line.Quantity) allFullyReceived = false;

                if (totalRec >= line.Quantity)
                {
                    line.LineStatus = PurchaseOrderLineStatus.FullyReceived;
                }
                else if (totalRec > 0)
                {
                    line.LineStatus = PurchaseOrderLineStatus.PartiallyReceived;
                }
            }

            if (allFullyReceived)
            {
                po.Status = PurchaseOrderStatus.FullyReceived;
            }
            else if (anyReceived)
            {
                po.Status = PurchaseOrderStatus.PartiallyReceived;
            }

            po.UpdatedAt = DateTime.UtcNow;
            po.UpdatedBy = actingUser;

            await _db.SaveChangesAsync(cancellationToken);
        }

        private static IQueryable<GoodsReceipt> FilterQuery(IQueryable<GoodsReceipt> q, GoodsReceiptListQueryDto query)
        {
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim().ToLower();
                q = q.Where(gr =>
                    gr.GRNNumber.ToLower().Contains(term) ||
                    gr.PurchaseOrderNumber.ToLower().Contains(term) ||
                    gr.VendorName.ToLower().Contains(term) ||
                    (gr.VehicleNumber != null && gr.VehicleNumber.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<GoodsReceiptStatus>(query.Status, true, out var status))
            {
                q = q.Where(gr => gr.Status == status);
            }

            if (query.DateFrom.HasValue)
            {
                q = q.Where(gr => gr.ReceiptDate >= query.DateFrom.Value.ToUniversalTime());
            }

            if (query.DateTo.HasValue)
            {
                q = q.Where(gr => gr.ReceiptDate <= query.DateTo.Value.ToUniversalTime());
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

            return q;
        }

        private static GoodsReceiptListItemDto MapToListItemDto(GoodsReceipt gr)
        {
            var (_, _, _, _, pct) = GoodsReceiptRules.CalculateSummary(gr.Items);
            return new GoodsReceiptListItemDto
            {
                Id = gr.Id,
                GRNNumber = gr.GRNNumber,
                PurchaseOrderNumber = gr.PurchaseOrderNumber,
                PurchaseOrderId = gr.PurchaseOrderId,
                VendorName = gr.VendorName,
                ReceiptDate = gr.ReceiptDate,
                Status = gr.Status,
                QualityStatus = gr.QualityStatus,
                ReceivedPercent = pct,
                IsPostedToInventory = gr.IsPostedToInventory,
                TotalAcceptedAmount = gr.TotalAcceptedAmount,
                VehicleNumber = gr.VehicleNumber,
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
                VendorId = gr.VendorId,
                VendorName = gr.VendorName,
                ReceiptDate = gr.ReceiptDate,
                WarehouseId = gr.WarehouseId,
                Warehouse = gr.Warehouse,
                Status = gr.Status,
                QualityStatus = gr.QualityStatus,
                GateInwardNumber = gr.GateInwardNumber,
                GateInwardTime = gr.GateInwardTime,
                VehicleNumber = gr.VehicleNumber,
                TransporterName = gr.TransporterName,
                LRNumber = gr.LRNumber,
                EWayBillNumber = gr.EWayBillNumber,
                WeighbridgeGrossWeight = gr.WeighbridgeGrossWeight,
                WeighbridgeTareWeight = gr.WeighbridgeTareWeight,
                WeighbridgeNetWeight = gr.WeighbridgeNetWeight,
                InspectionLotNumber = gr.InspectionLotNumber,
                TotalAcceptedAmount = gr.TotalAcceptedAmount,
                TotalLandedCost = gr.TotalLandedCost,
                IsPostedToInventory = gr.IsPostedToInventory,
                PostedToInventoryAt = gr.PostedToInventoryAt,
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
                    ItemId = i.ItemId,
                    ItemName = i.ItemName,
                    Description = i.Description,
                    Unit = i.Unit,
                    OrderedQuantity = i.OrderedQuantity,
                    PreviouslyReceivedQuantity = i.PreviouslyReceivedQuantity,
                    RemainingQuantity = i.RemainingQuantity,
                    ReceivedQuantity = i.ReceivedQuantity,
                    AcceptedQuantity = i.AcceptedQuantity,
                    RejectedQuantity = i.RejectedQuantity,
                    RejectionReasonCode = i.RejectionReasonCode,
                    BatchNumber = i.BatchNumber,
                    HeatNumber = i.HeatNumber,
                    MtcNumber = i.MtcNumber,
                    ExpiryDate = i.ExpiryDate,
                    PoRate = i.PoRate,
                    LandedCostPerUnit = i.LandedCostPerUnit,
                    ValuationAmount = i.ValuationAmount,
                    QcInspectedQuantity = i.QcInspectedQuantity,
                    QcAcceptedQuantity = i.QcAcceptedQuantity,
                    QcRejectedQuantity = i.QcRejectedQuantity,
                    QcReworkQuantity = i.QcReworkQuantity,
                    QcDispositionNotes = i.QcDispositionNotes,
                    BilledQuantity = i.BilledQuantity,
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
