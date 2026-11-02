using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
    public class PurchaseBillService : IPurchaseBillService
    {
        private readonly ERPDbContext _dbContext;
        private readonly PurchaseBillNumberingService _numberingService;
        private readonly IThreeWayMatchingEngine _matchingEngine;

        public PurchaseBillService(
            ERPDbContext dbContext, 
            PurchaseBillNumberingService numberingService,
            IThreeWayMatchingEngine matchingEngine)
        {
            _dbContext = dbContext;
            _numberingService = numberingService;
            _matchingEngine = matchingEngine;
        }

        public async Task<PurchaseBill3WayMatchResultDto> Validate3WayMatchAsync(PurchaseBillCreateRequestDto request, CancellationToken cancellationToken = default)
        {
            return await _matchingEngine.EvaluateMatchAsync(request, cancellationToken);
        }

        public async Task<PagedResult<PurchaseBillDto>> GetPurchaseBillsAsync(PurchaseBillFilterQueryDto query, CancellationToken cancellationToken = default)
        {
            var q = _dbContext.PurchaseBills.Include(x => x.Lines).Include(x => x.History).Where(x => !x.IsDeleted).AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim().ToLower();
                q = q.Where(x => x.BillNumber.ToLower().Contains(term) ||
                                 x.InvoiceNumber.ToLower().Contains(term) ||
                                 x.VendorName.ToLower().Contains(term) ||
                                 (x.PurchaseOrderNumber != null && x.PurchaseOrderNumber.ToLower().Contains(term)) ||
                                 (x.GRNNumber != null && x.GRNNumber.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<PurchaseBillStatus>(query.Status, true, out var status))
            {
                q = q.Where(x => x.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(query.PaymentStatus) && Enum.TryParse<PurchaseBillPaymentStatus>(query.PaymentStatus, true, out var pStatus))
            {
                q = q.Where(x => x.PaymentStatus == pStatus);
            }

            if (!string.IsNullOrWhiteSpace(query.VendorName))
            {
                var vTerm = query.VendorName.Trim().ToLower();
                q = q.Where(x => x.VendorName.ToLower().Contains(vTerm));
            }

            var totalCount = await q.CountAsync(cancellationToken);
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);

            var items = await q.OrderByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<PurchaseBillDto>
            {
                Items = items.Select(MapBill).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<PurchaseBillDto?> GetPurchaseBillByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var item = await _dbContext.PurchaseBills
                .Include(x => x.Lines)
                .Include(x => x.History)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            return item is null ? null : MapBill(item);
        }

        public async Task<PurchaseBillDto> CreatePurchaseBillAsync(PurchaseBillCreateRequestDto request, string currentUser, CancellationToken cancellationToken = default)
        {
            if (request.GRNId is > 0)
            {
                var existingActiveBill = await _dbContext.PurchaseBills
                    .FirstOrDefaultAsync(x => x.GRNId == request.GRNId.Value && x.Status != PurchaseBillStatus.Void && !x.IsDeleted, cancellationToken);

                if (existingActiveBill is not null)
                {
                    throw new InvalidOperationException($"Purchase Bill {existingActiveBill.BillNumber} already exists for GRN {request.GRNNumber ?? request.GRNId.Value.ToString()}.");
                }

                var grn = await _dbContext.GoodsReceipts
                    .FirstOrDefaultAsync(x => x.Id == request.GRNId.Value && !x.IsDeleted, cancellationToken);

                if (grn is not null && grn.Status != GoodsReceiptStatus.Completed && grn.Status != GoodsReceiptStatus.Posted)
                {
                    throw new InvalidOperationException("Only Completed or Posted GRNs can be billed.");
                }
            }

            // Execute 3-Way Match validation
            var matchResult = await _matchingEngine.EvaluateMatchAsync(request, cancellationToken);
            if (matchResult.IsBlocked)
            {
                throw new InvalidOperationException($"Cannot create Purchase Bill due to 3-Way Match Block: {matchResult.DiagnosticRemarks}");
            }

            var billNumber = await _numberingService.GenerateNumberAsync(cancellationToken);
            var now = DateTime.UtcNow;

            // Vendor & Statutory MSME 43B(h) / TDS 194Q evaluation
            var vendor = await _dbContext.Vendors
                .FirstOrDefaultAsync(v => v.Id == request.VendorId && !v.IsDeleted, cancellationToken);

            bool isMsmeSubject = false;
            DateTime? msmeDueDate = null;
            int msmeAllowedDays = 45;

            if (vendor != null && vendor.IsMsmeRegistered && 
                (vendor.MsmeCategory == MsmeCategory.Micro || vendor.MsmeCategory == MsmeCategory.Small))
            {
                isMsmeSubject = true;
                bool hasWrittenContract = !string.IsNullOrWhiteSpace(request.PaymentTerms) && !request.PaymentTerms.Equals("Immediate", StringComparison.OrdinalIgnoreCase);
                msmeAllowedDays = hasWrittenContract ? 45 : 15;
                msmeDueDate = request.InvoiceDate.ToUniversalTime().AddDays(msmeAllowedDays);
            }

            var initialStatus = matchResult.HasVarianceWarning
                ? PurchaseBillStatus.VarianceHold
                : PurchaseBillStatus.PendingApproval;

            var matchStatus = matchResult.HasVarianceWarning
                ? ThreeWayMatchStatus.PriceVarianceHold
                : ThreeWayMatchStatus.Matched;

            var entity = new PurchaseBill
            {
                BillNumber = billNumber,
                InvoiceNumber = string.IsNullOrWhiteSpace(request.InvoiceNumber) ? $"INV-{now:yyyyMMddHHmmss}" : request.InvoiceNumber,
                VendorId = request.VendorId,
                VendorName = request.VendorName,
                PurchaseOrderId = request.PurchaseOrderId,
                PurchaseOrderNumber = request.PurchaseOrderNumber,
                GRNId = request.GRNId,
                GRNNumber = request.GRNNumber,
                ConsolidatedGrnIds = request.ConsolidatedGrnIds,
                InvoiceDate = request.InvoiceDate.ToUniversalTime(),
                DueDate = msmeDueDate ?? request.DueDate.ToUniversalTime(),
                Currency = request.Currency ?? "INR",
                PaymentTerms = request.PaymentTerms ?? (isMsmeSubject ? $"Net {msmeAllowedDays} Days (MSME 43B(h))" : "Net 30 Days"),
                CgstAmount = request.CgstAmount,
                SgstAmount = request.SgstAmount,
                IgstAmount = request.IgstAmount,
                ThreeWayMatchStatus = matchStatus,
                IsThreeWayMatched = !matchResult.HasVarianceWarning,
                IsMsmeSubjectTo43B = isMsmeSubject,
                MsmeDueDate = msmeDueDate,
                MsmeAllowedDays = msmeAllowedDays,
                VarianceReason = matchResult.HasVarianceWarning ? matchResult.DiagnosticRemarks : null,
                PaymentStatus = PurchaseBillPaymentStatus.Unpaid,
                Status = initialStatus,
                Remarks = request.Remarks ?? string.Empty,
                CreatedBy = currentUser,
                CreatedAt = now,
                UpdatedBy = currentUser,
                UpdatedAt = now
            };

            foreach (var lineReq in request.Lines)
            {
                var q = lineReq.Quantity;
                var p = lineReq.UnitPrice;
                var disc = lineReq.DiscountAmount;
                var taxable = Math.Max(0m, q * p - disc);
                var taxPct = lineReq.TaxPercent > 0 ? lineReq.TaxPercent : 18m;
                var taxAmt = (taxable * taxPct) / 100m;
                var total = taxable + taxAmt;

                entity.Lines.Add(new PurchaseBillLine
                {
                    PurchaseOrderLineId = lineReq.PurchaseOrderLineId,
                    GoodsReceiptId = lineReq.GoodsReceiptId ?? request.GRNId,
                    GoodsReceiptItemId = lineReq.GoodsReceiptItemId,
                    ItemId = lineReq.ItemId,
                    ItemName = lineReq.ItemName,
                    Description = lineReq.Description ?? string.Empty,
                    Quantity = q,
                    UnitPrice = p,
                    DiscountAmount = disc,
                    TaxPercent = taxPct,
                    TaxAmount = taxAmt,
                    TotalAmount = total,
                    PoRate = p,
                    LineMatchStatus = matchResult.HasVarianceWarning ? ThreeWayMatchStatus.PriceVarianceHold : ThreeWayMatchStatus.Matched
                });
            }

            var totals = PurchaseBillRules.CalculateTotals(entity.Lines.Select(l => (l.Quantity, l.UnitPrice, l.DiscountAmount, l.TaxPercent)));
            entity.SubTotal = totals.SubTotal;
            entity.DiscountTotal = totals.DiscountTotal;
            entity.TaxTotal = totals.TaxTotal;
            entity.RoundOff = totals.RoundOff;

            // TDS Section 194Q Threshold Check (> ₹50 Lakhs cumulative purchase)
            decimal currentFiscalYearPurchases = await _dbContext.PurchaseBills
                .Where(b => b.VendorId == request.VendorId && 
                            b.Status != PurchaseBillStatus.Void && 
                            !b.IsDeleted && 
                            b.InvoiceDate.Year == now.Year)
                .SumAsync(b => b.SubTotal, cancellationToken);

            decimal totalCumulativePurchase = currentFiscalYearPurchases + totals.SubTotal;
            if (totalCumulativePurchase > 5000000m) // ₹50 Lakhs threshold
            {
                entity.IsTds194QApplicable = true;
                // If vendor lacks valid PAN, Section 206AA rate is 5.0%, otherwise 0.1%
                bool hasValidPan = vendor != null && !string.IsNullOrWhiteSpace(vendor.PAN) && vendor.PAN.Trim().Length == 10;
                entity.Tds194QPercentage = hasValidPan ? 0.10m : 5.00m;
                entity.TdsSection = "194Q";

                decimal taxableExcess = Math.Min(totals.SubTotal, totalCumulativePurchase - 5000000m);
                entity.Tds194QAmount = Math.Round(taxableExcess * (entity.Tds194QPercentage / 100m), 2, MidpointRounding.AwayFromZero);
                entity.TdsAmount = entity.Tds194QAmount;
                entity.TdsPercentage = entity.Tds194QPercentage;
            }
            else
            {
                entity.TdsSection = request.TdsSection;
                entity.TdsPercentage = request.TdsPercentage;
                entity.TdsAmount = request.TdsAmount;
            }

            var calculatedGrandTotal = totals.SubTotal - totals.DiscountTotal + request.CgstAmount + request.SgstAmount + request.IgstAmount - entity.TdsAmount;
            entity.GrandTotal = Math.Max(0m, calculatedGrandTotal > 0 ? calculatedGrandTotal : totals.GrandTotal);
            entity.PaidAmount = 0m;
            entity.BalanceAmount = entity.GrandTotal;
            entity.MatchedAmount = entity.GrandTotal;

            entity.History.Add(new PurchaseBillHistory
            {
                Status = initialStatus,
                Date = now,
                User = currentUser,
                Remarks = matchResult.HasVarianceWarning
                    ? $"Created under Variance Hold: {matchResult.DiagnosticRemarks}"
                    : "Bill created and validated against 3-Way Match"
            });

            _dbContext.PurchaseBills.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return MapBill(entity);
        }

        public async Task<PurchaseBillDto> BuildFromGrnsAsync(
            MultiGrnBillBuildRequestDto request,
            string currentUser,
            CancellationToken cancellationToken = default)
        {
            var vendor = await _dbContext.Vendors
                .FirstOrDefaultAsync(v => v.Id == request.VendorId && !v.IsDeleted, cancellationToken);

            if (vendor == null)
            {
                throw new KeyNotFoundException($"Vendor ID {request.VendorId} not found.");
            }

            var grns = await _dbContext.GoodsReceipts
                .Include(g => g.Items)
                    .ThenInclude(i => i.PurchaseOrderLine)
                .Where(g => request.GrnIds.Contains(g.Id) && 
                            g.VendorId == request.VendorId && 
                            (g.Status == GoodsReceiptStatus.Completed || g.Status == GoodsReceiptStatus.Posted) && 
                            !g.IsDeleted)
                .ToListAsync(cancellationToken);

            if (grns.Count == 0)
            {
                throw new InvalidOperationException("No completed or posted GRNs were found for the selected IDs and vendor.");
            }

            var billReq = new PurchaseBillCreateRequestDto
            {
                VendorId = vendor.Id,
                VendorName = vendor.Name,
                InvoiceNumber = $"INV-CONSOL-{DateTime.UtcNow:yyyyMMddHHmmss}",
                ConsolidatedGrnIds = JsonSerializer.Serialize(grns.Select(g => g.Id).ToList()),
                PurchaseOrderId = grns[0].PurchaseOrderId,
                PurchaseOrderNumber = grns[0].PurchaseOrderNumber,
                GRNId = grns[0].Id,
                GRNNumber = grns[0].GRNNumber,
                InvoiceDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(30),
                Currency = "INR",
                Remarks = $"Consolidated invoice built from {grns.Count} GRNs ({string.Join(", ", grns.Select(g => g.GRNNumber))})."
            };

            foreach (var grn in grns)
            {
                foreach (var item in grn.Items)
                {
                    decimal acceptedQty = item.AcceptedQuantity > 0 ? item.AcceptedQuantity : item.ReceivedQuantity;
                    decimal availableToBill = Math.Max(0m, acceptedQty - item.BilledQuantity);
                    if (availableToBill <= 0) continue;

                    decimal poRate = item.PoRate > 0 ? item.PoRate : (item.PurchaseOrderLine?.Rate ?? 0m);
                    decimal taxPct = item.PurchaseOrderLine?.TaxPercentage ?? 18m;

                    billReq.Lines.Add(new PurchaseBillLineReqDto
                    {
                        GoodsReceiptId = grn.Id,
                        GoodsReceiptItemId = item.Id,
                        PurchaseOrderLineId = item.PurchaseOrderLineId,
                        ItemId = item.ItemId,
                        ItemName = item.ItemName,
                        Description = item.Description,
                        Quantity = availableToBill,
                        UnitPrice = poRate,
                        TaxPercent = taxPct
                    });
                }
            }

            if (billReq.Lines.Count == 0)
            {
                throw new InvalidOperationException("All items across the selected GRNs have already been fully billed.");
            }

            return await CreatePurchaseBillAsync(billReq, currentUser, cancellationToken);
        }

        public async Task<bool> OverrideVarianceAsync(
            VarianceOverrideRequestDto request,
            string currentUser,
            CancellationToken cancellationToken = default)
        {
            return await _matchingEngine.AuthorizeVarianceOverrideAsync(
                request.BillId, 
                request.OverrideReason, 
                currentUser, 
                cancellationToken);
        }

        public async Task<MsmeComplianceSummaryDto> GetMsmeComplianceSummaryAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow.Date;
            var msmeBills = await _dbContext.PurchaseBills
                .Include(b => b.Lines)
                .Where(b => b.IsMsmeSubjectTo43B && 
                            b.PaymentStatus != PurchaseBillPaymentStatus.Paid && 
                            b.Status != PurchaseBillStatus.Void && 
                            !b.IsDeleted)
                .ToListAsync(cancellationToken);

            var summary = new MsmeComplianceSummaryDto
            {
                TotalMsmeBillsCount = msmeBills.Count,
                TotalMsmePayableAmount = msmeBills.Sum(b => b.BalanceAmount)
            };

            foreach (var bill in msmeBills)
            {
                var dueDate = bill.MsmeDueDate?.Date ?? bill.DueDate.Date;
                var remainingDays = (dueDate - now).Days;

                if (remainingDays < 0)
                {
                    summary.OverdueDisallowanceCount++;
                    summary.OverdueDisallowanceAmount += bill.BalanceAmount;
                    summary.OverdueBills.Add(new MsmeOverdueBillItemDto
                    {
                        BillId = bill.Id,
                        BillNumber = bill.BillNumber,
                        InvoiceNumber = bill.InvoiceNumber,
                        VendorId = bill.VendorId,
                        VendorName = bill.VendorName,
                        MsmeCategory = "Micro/Small",
                        InvoiceDate = bill.InvoiceDate,
                        MsmeDueDate = dueDate,
                        OverdueDays = Math.Abs(remainingDays),
                        OutstandingAmount = bill.BalanceAmount,
                        DisallowanceWarning = $"CRITICAL: Overdue by {Math.Abs(remainingDays)} days under Section 43B(h). Amount added back to taxable income."
                    });
                }
                else if (remainingDays <= 5)
                {
                    summary.DueWithin5DaysCount++;
                    summary.DueWithin5DaysAmount += bill.BalanceAmount;
                }
                else
                {
                    summary.NormalCount++;
                    summary.NormalAmount += bill.BalanceAmount;
                }
            }

            return summary;
        }

        public async Task<bool> ReconcileGstr2BAsync(
            Gstr2BReconcileDto request,
            string currentUser,
            CancellationToken cancellationToken = default)
        {
            var bill = await _dbContext.PurchaseBills
                .FirstOrDefaultAsync(b => b.Id == request.BillId && !b.IsDeleted, cancellationToken);

            if (bill == null) return false;

            bill.Gstr2BStatus = request.Gstr2BStatus;
            bill.Gstr2BRef = request.Gstr2BRef;
            bill.UpdatedAt = DateTime.UtcNow;
            bill.UpdatedBy = currentUser;

            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<PurchaseBillDto?> UpdatePurchaseBillAsync(int id, PurchaseBillCreateRequestDto request, string currentUser, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.PurchaseBills
                .Include(x => x.Lines)
                .Include(x => x.History)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (entity is null) return null;

            if (entity.Status != PurchaseBillStatus.Draft && entity.Status != PurchaseBillStatus.PendingApproval && entity.Status != PurchaseBillStatus.VarianceHold)
            {
                throw new InvalidOperationException($"Only Draft, PendingApproval, or VarianceHold bills can be edited. Current status: {entity.Status}.");
            }

            var now = DateTime.UtcNow;
            entity.InvoiceNumber = request.InvoiceNumber;
            entity.VendorId = request.VendorId;
            entity.VendorName = request.VendorName;
            entity.InvoiceDate = request.InvoiceDate.ToUniversalTime();
            entity.DueDate = request.DueDate.ToUniversalTime();
            entity.CgstAmount = request.CgstAmount;
            entity.SgstAmount = request.SgstAmount;
            entity.IgstAmount = request.IgstAmount;
            entity.TdsSection = request.TdsSection;
            entity.TdsPercentage = request.TdsPercentage;
            entity.TdsAmount = request.TdsAmount;
            entity.Remarks = request.Remarks ?? entity.Remarks;
            entity.UpdatedAt = now;
            entity.UpdatedBy = currentUser;

            _dbContext.PurchaseBillLines.RemoveRange(entity.Lines);
            entity.Lines.Clear();

            foreach (var lineReq in request.Lines)
            {
                var q = lineReq.Quantity;
                var p = lineReq.UnitPrice;
                var disc = lineReq.DiscountAmount;
                var taxable = Math.Max(0m, q * p - disc);
                var taxPct = lineReq.TaxPercent > 0 ? lineReq.TaxPercent : 18m;
                var taxAmt = (taxable * taxPct) / 100m;
                var total = taxable + taxAmt;

                entity.Lines.Add(new PurchaseBillLine
                {
                    PurchaseBillId = entity.Id,
                    PurchaseOrderLineId = lineReq.PurchaseOrderLineId,
                    GoodsReceiptId = lineReq.GoodsReceiptId ?? entity.GRNId,
                    GoodsReceiptItemId = lineReq.GoodsReceiptItemId,
                    ItemId = lineReq.ItemId,
                    ItemName = lineReq.ItemName,
                    Description = lineReq.Description ?? string.Empty,
                    Quantity = q,
                    UnitPrice = p,
                    DiscountAmount = disc,
                    TaxPercent = taxPct,
                    TaxAmount = taxAmt,
                    TotalAmount = total,
                    PoRate = p
                });
            }

            var totals = PurchaseBillRules.CalculateTotals(entity.Lines.Select(l => (l.Quantity, l.UnitPrice, l.DiscountAmount, l.TaxPercent)));
            entity.SubTotal = totals.SubTotal;
            entity.DiscountTotal = totals.DiscountTotal;
            entity.TaxTotal = totals.TaxTotal;
            entity.RoundOff = totals.RoundOff;

            var calculatedGrandTotal = totals.SubTotal - totals.DiscountTotal + request.CgstAmount + request.SgstAmount + request.IgstAmount - request.TdsAmount;
            entity.GrandTotal = Math.Max(0m, calculatedGrandTotal > 0 ? calculatedGrandTotal : totals.GrandTotal);
            entity.BalanceAmount = entity.GrandTotal - entity.PaidAmount;

            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapBill(entity);
        }

        public async Task<bool> DeletePurchaseBillAsync(int id, string currentUser, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.PurchaseBills.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            if (entity is null) return false;

            if (entity.Status == PurchaseBillStatus.Posted || entity.Status == PurchaseBillStatus.Paid)
            {
                throw new InvalidOperationException("Posted or Paid bills cannot be deleted.");
            }

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = currentUser;

            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<PurchaseBillDto?> ApprovePurchaseBillAsync(int id, string remarks, string currentUser, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.PurchaseBills
                .Include(x => x.Lines)
                .Include(x => x.History)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (entity is null) return null;

            if (entity.Status == PurchaseBillStatus.VarianceHold && !entity.IsThreeWayMatched)
            {
                throw new InvalidOperationException("Cannot approve bill in Variance Hold without managerial override.");
            }

            var now = DateTime.UtcNow;
            entity.Status = PurchaseBillStatus.Approved;
            entity.UpdatedAt = now;
            entity.UpdatedBy = currentUser;

            entity.History.Add(new PurchaseBillHistory
            {
                Status = PurchaseBillStatus.Approved,
                Date = now,
                User = currentUser,
                Remarks = string.IsNullOrWhiteSpace(remarks) ? "Bill approved for accounting liability posting" : remarks
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapBill(entity);
        }

        public async Task<PurchaseBillDto?> PostPurchaseBillAsync(int id, string remarks, string currentUser, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.PurchaseBills
                .Include(x => x.Lines)
                .Include(x => x.History)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (entity is null) return null;

            if (entity.Status == PurchaseBillStatus.VarianceHold && !entity.IsThreeWayMatched)
            {
                throw new InvalidOperationException("Bill is held due to 3-Way matching variance and cannot be posted.");
            }

            var now = DateTime.UtcNow;
            int? actingUserId = int.TryParse(currentUser, out var uid) ? uid : null;

            // 1. Double-Entry AP Subledger: Dr GRN/IR Clearing / Dr GST Input / Cr Vendor Payable / Cr TDS 194Q
            var ledgerEntry = new VendorLedgerEntry
            {
                VendorId = entity.VendorId,
                PurchaseBillId = entity.Id,
                VoucherNumber = $"VOUCH-PB-{entity.BillNumber}",
                EntryDate = now,
                EntryType = "Bill",
                ReferenceId = entity.Id,
                ReferenceNumber = entity.BillNumber,
                DebitAmount = 0m,
                CreditAmount = entity.GrandTotal,
                Narration = $"Liability booked for Purchase Bill #{entity.BillNumber}. Dr GRN/IR Clearing: ₹{entity.SubTotal:N2}, Cr AP: ₹{entity.GrandTotal:N2}, TDS 194Q: ₹{entity.TdsAmount:N2}.",
                CreatedAt = now,
                CreatedByUserId = actingUserId
            };
            _dbContext.VendorLedgerEntries.Add(ledgerEntry);

            // 2. Synchronize BilledQuantity on PO lines and GRN items
            foreach (var line in entity.Lines)
            {
                if (line.PurchaseOrderLineId.HasValue && line.PurchaseOrderLineId.Value > 0)
                {
                    var poLine = await _dbContext.PurchaseOrderLines.FirstOrDefaultAsync(l => l.Id == line.PurchaseOrderLineId.Value, cancellationToken);
                    if (poLine != null)
                    {
                        poLine.BilledQuantity += line.Quantity;
                    }
                }

                if (line.GoodsReceiptItemId.HasValue && line.GoodsReceiptItemId.Value > 0)
                {
                    var grnItem = await _dbContext.GoodsReceiptItems.FirstOrDefaultAsync(i => i.Id == line.GoodsReceiptItemId.Value, cancellationToken);
                    if (grnItem != null)
                    {
                        grnItem.BilledQuantity += line.Quantity;
                    }
                }
            }

            entity.Status = PurchaseBillStatus.Posted;
            entity.PostedAt = now;
            entity.PostedByUserId = actingUserId;
            entity.UpdatedAt = now;
            entity.UpdatedBy = currentUser;

            entity.History.Add(new PurchaseBillHistory
            {
                Status = PurchaseBillStatus.Posted,
                Date = now,
                User = currentUser,
                Remarks = string.IsNullOrWhiteSpace(remarks) ? "Posted to General Ledger (Dr GRN/IR Clearing / Cr Accounts Payable)" : remarks
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapBill(entity);
        }

        public async Task<PurchaseBillDto?> PayPurchaseBillAsync(int id, string remarks, string currentUser, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.PurchaseBills
                .Include(x => x.Lines)
                .Include(x => x.History)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (entity is null) return null;

            var now = DateTime.UtcNow;
            entity.PaidAmount = entity.GrandTotal;
            entity.BalanceAmount = 0m;
            entity.PaymentStatus = PurchaseBillPaymentStatus.Paid;
            entity.Status = PurchaseBillStatus.Paid;
            entity.UpdatedAt = now;
            entity.UpdatedBy = currentUser;

            entity.History.Add(new PurchaseBillHistory
            {
                Status = PurchaseBillStatus.Paid,
                Date = now,
                User = currentUser,
                Remarks = string.IsNullOrWhiteSpace(remarks) ? "Paid full outstanding balance" : remarks
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapBill(entity);
        }

        public async Task<PurchaseBillDto?> VoidPurchaseBillAsync(int id, string remarks, string currentUser, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.PurchaseBills
                .Include(x => x.Lines)
                .Include(x => x.History)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (entity is null) return null;

            var now = DateTime.UtcNow;
            entity.Status = PurchaseBillStatus.Void;
            entity.UpdatedAt = now;
            entity.UpdatedBy = currentUser;

            entity.History.Add(new PurchaseBillHistory
            {
                Status = PurchaseBillStatus.Void,
                Date = now,
                User = currentUser,
                Remarks = string.IsNullOrWhiteSpace(remarks) ? "Bill marked void" : remarks
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapBill(entity);
        }

        public async Task<PurchaseBillDto?> DuplicatePurchaseBillAsync(int id, string currentUser, CancellationToken cancellationToken = default)
        {
            var existing = await _dbContext.PurchaseBills
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (existing is null) return null;

            var createReq = new PurchaseBillCreateRequestDto
            {
                InvoiceNumber = $"{existing.InvoiceNumber}-COPY",
                VendorId = existing.VendorId,
                VendorName = existing.VendorName,
                PurchaseOrderId = existing.PurchaseOrderId,
                PurchaseOrderNumber = existing.PurchaseOrderNumber,
                GRNId = existing.GRNId,
                GRNNumber = existing.GRNNumber,
                InvoiceDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(30),
                Currency = existing.Currency,
                PaymentTerms = existing.PaymentTerms,
                CgstAmount = existing.CgstAmount,
                SgstAmount = existing.SgstAmount,
                IgstAmount = existing.IgstAmount,
                TdsSection = existing.TdsSection,
                TdsPercentage = existing.TdsPercentage,
                TdsAmount = existing.TdsAmount,
                Remarks = $"Duplicated from {existing.BillNumber}",
                Lines = existing.Lines.Select(l => new PurchaseBillLineReqDto
                {
                    PurchaseOrderLineId = l.PurchaseOrderLineId,
                    GoodsReceiptItemId = l.GoodsReceiptItemId,
                    ItemId = l.ItemId,
                    ItemName = l.ItemName,
                    Description = l.Description,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice,
                    DiscountAmount = l.DiscountAmount,
                    TaxPercent = l.TaxPercent
                }).ToList()
            };

            return await CreatePurchaseBillAsync(createReq, currentUser, cancellationToken);
        }

        public async Task<PurchaseBillDashboardDto> GetPurchaseBillDashboardAsync(CancellationToken cancellationToken = default)
        {
            var bills = await _dbContext.PurchaseBills.Where(x => !x.IsDeleted).AsNoTracking().ToListAsync(cancellationToken);

            return new PurchaseBillDashboardDto
            {
                TotalBills = bills.Count,
                DraftCount = bills.Count(x => x.Status == PurchaseBillStatus.Draft),
                ApprovedCount = bills.Count(x => x.Status == PurchaseBillStatus.Approved),
                PostedCount = bills.Count(x => x.Status == PurchaseBillStatus.Posted),
                PaidCount = bills.Count(x => x.Status == PurchaseBillStatus.Paid),
                TotalBilledAmount = bills.Sum(x => x.GrandTotal),
                TotalOutstandingAmount = bills.Sum(x => x.BalanceAmount)
            };
        }

        private static PurchaseBillDto MapBill(PurchaseBill entity)
        {
            return new PurchaseBillDto
            {
                Id = entity.Id,
                BillNumber = entity.BillNumber,
                InvoiceNumber = entity.InvoiceNumber,
                VendorId = entity.VendorId,
                VendorName = entity.VendorName,
                PurchaseOrderId = entity.PurchaseOrderId,
                PurchaseOrderNumber = entity.PurchaseOrderNumber,
                GRNId = entity.GRNId,
                GRNNumber = entity.GRNNumber,
                ConsolidatedGrnIds = entity.ConsolidatedGrnIds,
                InvoiceDate = entity.InvoiceDate,
                DueDate = entity.DueDate,
                Currency = entity.Currency,
                SubTotal = entity.SubTotal,
                DiscountTotal = entity.DiscountTotal,
                TaxTotal = entity.TaxTotal,
                CgstAmount = entity.CgstAmount,
                SgstAmount = entity.SgstAmount,
                IgstAmount = entity.IgstAmount,
                ThreeWayMatchStatus = entity.ThreeWayMatchStatus,
                IsThreeWayMatched = entity.IsThreeWayMatched,
                MatchedAmount = entity.MatchedAmount,
                VarianceAmount = entity.VarianceAmount,
                VarianceOverrideReason = entity.VarianceOverrideReason,
                VarianceOverriddenBy = entity.VarianceOverriddenBy,
                IsMsmeSubjectTo43B = entity.IsMsmeSubjectTo43B,
                MsmeDueDate = entity.MsmeDueDate,
                MsmeAllowedDays = entity.MsmeAllowedDays,
                IsTds194QApplicable = entity.IsTds194QApplicable,
                Tds194QPercentage = entity.Tds194QPercentage,
                Tds194QAmount = entity.Tds194QAmount,
                TdsSection = entity.TdsSection,
                TdsPercentage = entity.TdsPercentage,
                TdsAmount = entity.TdsAmount,
                Gstr2BStatus = entity.Gstr2BStatus,
                Gstr2BRef = entity.Gstr2BRef,
                VarianceReason = entity.VarianceReason,
                PostedAt = entity.PostedAt,
                PostedByUserId = entity.PostedByUserId,
                RoundOff = entity.RoundOff,
                GrandTotal = entity.GrandTotal,
                PaidAmount = entity.PaidAmount,
                BalanceAmount = entity.BalanceAmount,
                PaymentTerms = entity.PaymentTerms,
                PaymentStatus = entity.PaymentStatus,
                Status = entity.Status,
                Remarks = entity.Remarks,
                AttachmentsCount = entity.AttachmentsCount,
                CreatedBy = entity.CreatedBy,
                CreatedAt = entity.CreatedAt,
                UpdatedBy = entity.UpdatedBy,
                UpdatedAt = entity.UpdatedAt,
                Lines = entity.Lines.Select(l => new PurchaseBillLineDto
                {
                    Id = l.Id.ToString(),
                    PurchaseOrderLineId = l.PurchaseOrderLineId,
                    GoodsReceiptId = l.GoodsReceiptId,
                    GoodsReceiptItemId = l.GoodsReceiptItemId,
                    ItemId = l.ItemId,
                    ItemName = l.ItemName,
                    Description = l.Description,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice,
                    DiscountAmount = l.DiscountAmount,
                    TaxPercent = l.TaxPercent,
                    TaxAmount = l.TaxAmount,
                    TotalAmount = l.TotalAmount,
                    PoRate = l.PoRate,
                    GrnAcceptedQuantity = l.GrnAcceptedQuantity,
                    RateVariance = l.RateVariance,
                    QuantityVariance = l.QuantityVariance,
                    TaxVariance = l.TaxVariance,
                    LineMatchStatus = l.LineMatchStatus,
                    MatchRemarks = l.MatchRemarks
                }).ToList(),
                History = entity.History.Select(h => new PurchaseBillHistoryDto
                {
                    Id = h.Id.ToString(),
                    Status = h.Status,
                    Date = h.Date,
                    User = h.User,
                    Remarks = h.Remarks
                }).ToList()
            };
        }
    }
}
