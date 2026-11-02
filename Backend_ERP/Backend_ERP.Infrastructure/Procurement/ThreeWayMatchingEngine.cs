using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Procurement
{
    public class ThreeWayMatchingEngine : IThreeWayMatchingEngine
    {
        private readonly ERPDbContext _db;

        public ThreeWayMatchingEngine(ERPDbContext db)
        {
            _db = db;
        }

        public async Task<PurchaseBill3WayMatchResultDto> EvaluateMatchAsync(
            PurchaseBillCreateRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var result = new PurchaseBill3WayMatchResultDto();

            PurchaseOrder? po = null;
            if (request.PurchaseOrderId.HasValue && request.PurchaseOrderId.Value > 0)
            {
                po = await _db.PurchaseOrders
                    .Include(p => p.Lines)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == request.PurchaseOrderId.Value && !p.IsDeleted, cancellationToken);
            }

            GoodsReceipt? grn = null;
            if (request.GRNId.HasValue && request.GRNId.Value > 0)
            {
                grn = await _db.GoodsReceipts
                    .Include(g => g.Items)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(g => g.Id == request.GRNId.Value && !g.IsDeleted, cancellationToken);
            }

            bool hasVarianceWarning = false;
            bool isBlocked = false;
            var remarks = new List<string>();

            foreach (var line in request.Lines)
            {
                var lineResult = new PurchaseBillLineMatchResultDto
                {
                    ItemName = line.ItemName,
                    BillRate = line.UnitPrice,
                    BillQuantity = line.Quantity
                };

                // Find matching PO Line
                PurchaseOrderLine? poLine = null;
                if (po != null)
                {
                    if (line.PurchaseOrderLineId.HasValue && line.PurchaseOrderLineId.Value > 0)
                        poLine = po.Lines.FirstOrDefault(l => l.Id == line.PurchaseOrderLineId.Value);
                    else
                        poLine = po.Lines.FirstOrDefault(l => l.ItemName.Equals(line.ItemName, StringComparison.OrdinalIgnoreCase));
                }

                // Find matching GRN Item
                GoodsReceiptItem? grnItem = null;
                if (grn != null)
                {
                    if (line.GoodsReceiptItemId.HasValue && line.GoodsReceiptItemId.Value > 0)
                        grnItem = grn.Items.FirstOrDefault(i => i.Id == line.GoodsReceiptItemId.Value);
                    else
                        grnItem = grn.Items.FirstOrDefault(i => i.ItemName.Equals(line.ItemName, StringComparison.OrdinalIgnoreCase));
                }

                // 1. Evaluate Rate Match
                if (poLine != null)
                {
                    lineResult.PoRate = poLine.Rate;
                    decimal rateDiff = Math.Abs(line.UnitPrice - poLine.Rate);
                    decimal rateDiffPercent = poLine.Rate > 0 ? (rateDiff / poLine.Rate) * 100m : 0m;
                    lineResult.RateVariancePercent = Math.Round(rateDiffPercent, 2);

                    // Tolerance: <= 0.5% or <= 5.00 absolute
                    if (rateDiffPercent > 0.5m && rateDiff > 5.00m)
                    {
                        hasVarianceWarning = true;
                        lineResult.Status = "Warning";
                        lineResult.Remarks += $"Rate variance ({lineResult.RateVariancePercent:N2}%, diff ₹{rateDiff:N2}) exceeds tolerance. ";
                    }
                }

                // 2. Evaluate Quantity Match (Strict 0% positive tolerance above GRN accepted qty)
                if (grnItem != null)
                {
                    decimal acceptedQty = grnItem.AcceptedQuantity > 0 ? grnItem.AcceptedQuantity : grnItem.ReceivedQuantity;
                    lineResult.GrnAcceptedQuantity = acceptedQty;

                    decimal availableToBill = Math.Max(0m, acceptedQty - grnItem.BilledQuantity);
                    if (line.Quantity > availableToBill)
                    {
                        isBlocked = true;
                        lineResult.ExceedsBilledLimit = true;
                        lineResult.Status = "Blocked";
                        lineResult.Remarks += $"Bill quantity ({line.Quantity}) exceeds remaining GRN accepted unbilled quantity ({availableToBill:N2}). Hard-stop applied. ";
                    }
                }
                else if (poLine != null)
                {
                    // Direct PO bill validation if no GRN attached
                    decimal remainingPoQty = Math.Max(0m, (poLine.Quantity * 1.02m) - poLine.BilledQuantity); // 2% max bulk PO tolerance
                    if (line.Quantity > remainingPoQty)
                    {
                        isBlocked = true;
                        lineResult.ExceedsBilledLimit = true;
                        lineResult.Status = "Blocked";
                        lineResult.Remarks += $"Bill quantity ({line.Quantity}) exceeds allowed deliverable PO line quantity ({remainingPoQty:N2}). ";
                    }
                }

                // 3. Evaluate Tax / HSN Alignment
                if (poLine != null && poLine.TaxPercentage > 0)
                {
                    if (Math.Abs(line.TaxPercent - poLine.TaxPercentage) > 0.01m)
                    {
                        hasVarianceWarning = true;
                        if (lineResult.Status != "Blocked") lineResult.Status = "Warning";
                        lineResult.Remarks += $"GST Tax slab mismatch: Bill has {line.TaxPercent:N2}%, PO line expects {poLine.TaxPercentage:N2}%. ";
                    }
                }

                if (string.IsNullOrWhiteSpace(lineResult.Remarks))
                {
                    lineResult.Remarks = "Verified: Rate, Quantity, and Tax match within enterprise tolerances.";
                }

                result.LineResults.Add(lineResult);
            }

            result.IsBlocked = isBlocked;
            result.HasVarianceWarning = hasVarianceWarning;
            result.IsMatched = !isBlocked && !hasVarianceWarning;

            if (isBlocked)
            {
                result.Status = "Blocked";
                result.DiagnosticRemarks = "Hard validation halt: One or more bill lines exceed physical GRN accepted or PO deliverable quantities.";
            }
            else if (hasVarianceWarning)
            {
                result.Status = "Warning";
                result.DiagnosticRemarks = "Discrepancy detected: Unit price or tax variance detected. Requires managerial override before posting.";
            }
            else
            {
                result.Status = "Matched";
                result.DiagnosticRemarks = "All lines strictly verified against upstream Purchase Order and Goods Receipt records.";
            }

            return result;
        }

        public async Task<PurchaseBill3WayMatchResultDto> ProcessBillPostingValidationAsync(
            int purchaseBillId,
            CancellationToken cancellationToken = default)
        {
            var bill = await _db.PurchaseBills
                .Include(b => b.Lines)
                    .ThenInclude(l => l.PurchaseOrderLine)
                .Include(b => b.Lines)
                    .ThenInclude(l => l.GoodsReceiptItem)
                .FirstOrDefaultAsync(b => b.Id == purchaseBillId && !b.IsDeleted, cancellationToken);

            if (bill == null)
            {
                throw new KeyNotFoundException($"Purchase Bill ID {purchaseBillId} was not found.");
            }

            var requestDto = new PurchaseBillCreateRequestDto
            {
                PurchaseOrderId = bill.PurchaseOrderId,
                GRNId = bill.GRNId,
                Lines = bill.Lines.Select(l => new PurchaseBillLineReqDto
                {
                    PurchaseOrderLineId = l.PurchaseOrderLineId,
                    GoodsReceiptItemId = l.GoodsReceiptItemId,
                    ItemId = l.ItemId,
                    ItemName = l.ItemName,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice,
                    TaxPercent = l.TaxPercent
                }).ToList()
            };

            var eval = await EvaluateMatchAsync(requestDto, cancellationToken);

            // Audit log every line
            foreach (var lr in eval.LineResults)
            {
                var auditLog = new ThreeWayMatchAuditLog
                {
                    PurchaseBillId = bill.Id,
                    BillNumber = bill.BillNumber,
                    PurchaseOrderId = bill.PurchaseOrderId,
                    GoodsReceiptId = bill.GRNId,
                    ItemName = lr.ItemName,
                    CheckType = lr.ExceedsBilledLimit ? "Quantity" : (lr.RateVariancePercent > 0.5m ? "Rate" : "Comprehensive"),
                    PoValue = lr.PoRate,
                    GrnValue = lr.GrnAcceptedQuantity,
                    BillValue = lr.BillRate,
                    Variance = Math.Abs(lr.BillRate - lr.PoRate),
                    VariancePercentage = lr.RateVariancePercent,
                    IsWithinTolerance = lr.Status != "Blocked" && lr.Status != "Warning",
                    Status = lr.Status,
                    DiscrepancyMessage = lr.Remarks,
                    CheckedAt = DateTime.UtcNow
                };

                _db.ThreeWayMatchAuditLogs.Add(auditLog);
            }

            if (eval.IsBlocked)
            {
                bill.ThreeWayMatchStatus = ThreeWayMatchStatus.QuantityVarianceHold;
                bill.Status = PurchaseBillStatus.VarianceHold;
            }
            else if (eval.HasVarianceWarning)
            {
                bill.ThreeWayMatchStatus = ThreeWayMatchStatus.PriceVarianceHold;
                bill.Status = PurchaseBillStatus.VarianceHold;
            }
            else
            {
                bill.ThreeWayMatchStatus = ThreeWayMatchStatus.Matched;
                bill.IsThreeWayMatched = true;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return eval;
        }

        public async Task<bool> AuthorizeVarianceOverrideAsync(
            int purchaseBillId,
            string overrideReason,
            string managerUserId,
            CancellationToken cancellationToken = default)
        {
            var bill = await _db.PurchaseBills
                .FirstOrDefaultAsync(b => b.Id == purchaseBillId && !b.IsDeleted, cancellationToken);

            if (bill == null)
            {
                throw new KeyNotFoundException($"Purchase Bill ID {purchaseBillId} was not found.");
            }

            if (bill.ThreeWayMatchStatus == ThreeWayMatchStatus.QuantityVarianceHold)
            {
                throw new InvalidOperationException("Quantity variance exceeding physical GRN acceptance cannot be overridden. Invoice must be amended or debit note created.");
            }

            var now = DateTime.UtcNow;
            bill.ThreeWayMatchStatus = ThreeWayMatchStatus.ManuallyApproved;
            bill.IsThreeWayMatched = true;
            bill.VarianceOverrideReason = overrideReason;
            bill.VarianceOverriddenBy = managerUserId;
            bill.VarianceOverriddenAt = now;
            bill.Status = PurchaseBillStatus.Approved;
            bill.UpdatedAt = now;
            bill.UpdatedBy = managerUserId;

            // Audit the override
            _db.ThreeWayMatchAuditLogs.Add(new ThreeWayMatchAuditLog
            {
                PurchaseBillId = bill.Id,
                BillNumber = bill.BillNumber,
                PurchaseOrderId = bill.PurchaseOrderId,
                GoodsReceiptId = bill.GRNId,
                CheckType = "ManagerialOverride",
                Status = "Approved",
                IsWithinTolerance = true,
                DiscrepancyMessage = $"Managerial override granted: {overrideReason}",
                OverriddenBy = managerUserId,
                OverrideReason = overrideReason,
                CheckedAt = now
            });

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}

