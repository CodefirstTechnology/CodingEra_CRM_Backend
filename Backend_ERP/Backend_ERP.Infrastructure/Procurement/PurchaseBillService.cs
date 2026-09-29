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
    public class PurchaseBillService : IPurchaseBillService
    {
        private readonly ERPDbContext _dbContext;
        private readonly PurchaseBillNumberingService _numberingService;

        public PurchaseBillService(ERPDbContext dbContext, PurchaseBillNumberingService numberingService)
        {
            _dbContext = dbContext;
            _numberingService = numberingService;
        }

        public async Task<PurchaseBill3WayMatchResultDto> Validate3WayMatchAsync(PurchaseBillCreateRequestDto request, CancellationToken cancellationToken = default)
        {
            var result = new PurchaseBill3WayMatchResultDto();

            PurchaseOrder? po = null;
            if (request.PurchaseOrderId is > 0)
            {
                po = await _dbContext.PurchaseOrders
                    .Include(x => x.Lines)
                    .FirstOrDefaultAsync(x => x.Id == request.PurchaseOrderId.Value && !x.IsDeleted, cancellationToken);
            }

            GoodsReceipt? grn = null;
            if (request.GRNId is > 0)
            {
                grn = await _dbContext.GoodsReceipts
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x => x.Id == request.GRNId.Value && !x.IsDeleted, cancellationToken);
            }

            bool hasWarning = false;
            bool isBlocked = false;
            var remarksList = new List<string>();

            foreach (var lineReq in request.Lines)
            {
                var lineResult = new PurchaseBillLineMatchResultDto
                {
                    ItemName = lineReq.ItemName,
                    BillRate = lineReq.UnitPrice,
                    BillQuantity = lineReq.Quantity
                };

                // Find matching PO line
                PurchaseOrderLine? poLine = null;
                if (po != null)
                {
                    if (lineReq.PurchaseOrderLineId is > 0)
                        poLine = po.Lines.FirstOrDefault(x => x.Id == lineReq.PurchaseOrderLineId.Value);
                    else
                        poLine = po.Lines.FirstOrDefault(x => x.ItemName.Equals(lineReq.ItemName, StringComparison.OrdinalIgnoreCase));
                }

                // Find matching GRN item
                GoodsReceiptItem? grnItem = null;
                if (grn != null)
                {
                    if (lineReq.GoodsReceiptItemId is > 0)
                        grnItem = grn.Items.FirstOrDefault(x => x.Id == lineReq.GoodsReceiptItemId.Value);
                    else
                        grnItem = grn.Items.FirstOrDefault(x => x.ItemName.Equals(lineReq.ItemName, StringComparison.OrdinalIgnoreCase));
                }

                if (poLine != null)
                {
                    lineResult.PoRate = poLine.Rate;
                    if (poLine.Rate > 0)
                    {
                        lineResult.RateVariancePercent = Math.Abs(lineReq.UnitPrice - poLine.Rate) / poLine.Rate * 100m;
                    }

                    // Check over-billing overflow (5% max allowed above PO quantity)
                    if (poLine.BilledQuantity + lineReq.Quantity > poLine.Quantity * 1.05m)
                    {
                        lineResult.ExceedsBilledLimit = true;
                        isBlocked = true;
                        lineResult.Status = "Blocked";
                        lineResult.Remarks = $"Billed quantity ({poLine.BilledQuantity + lineReq.Quantity}) exceeds allowed limit for PO line ({poLine.Quantity * 1.05m:N2}).";
                        remarksList.Add(lineResult.Remarks);
                    }
                }

                if (grnItem != null)
                {
                    lineResult.GrnAcceptedQuantity = grnItem.AcceptedQuantity > 0 ? grnItem.AcceptedQuantity : grnItem.ReceivedQuantity;
                    if (lineResult.GrnAcceptedQuantity > 0)
                    {
                        lineResult.QuantityVariancePercent = Math.Abs(lineReq.Quantity - lineResult.GrnAcceptedQuantity) / lineResult.GrnAcceptedQuantity * 100m;
                    }
                }

                // Rate variance tolerance: 2.0%, Qty variance tolerance: 5.0%
                if (lineResult.Status != "Blocked")
                {
                    if (lineResult.RateVariancePercent > 2.0m || lineResult.QuantityVariancePercent > 5.0m)
                    {
                        hasWarning = true;
                        lineResult.Status = "Warning";
                        lineResult.Remarks = $"Variance detected: Rate Diff={lineResult.RateVariancePercent:N2}%, Qty Diff={lineResult.QuantityVariancePercent:N2}%.";
                        remarksList.Add($"Line '{lineReq.ItemName}': Rate Diff {lineResult.RateVariancePercent:N2}%, Qty Diff {lineResult.QuantityVariancePercent:N2}%");
                    }
                    else
                    {
                        lineResult.Status = "Matched";
                    }
                }

                result.LineResults.Add(lineResult);
            }

            result.IsBlocked = isBlocked;
            result.HasVarianceWarning = hasWarning;
            result.IsMatched = !isBlocked && !hasWarning;

            if (isBlocked)
            {
                result.Status = "Blocked";
                result.DiagnosticRemarks = "Blocked: " + string.Join(" | ", remarksList);
            }
            else if (hasWarning)
            {
                result.Status = "Warning";
                result.DiagnosticRemarks = "Variance Hold: " + string.Join(" | ", remarksList);
            }
            else
            {
                result.Status = "Matched";
                result.DiagnosticRemarks = "3-Way Match Passed successfully.";
            }

            return result;
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

                if (grn is not null && grn.Status != GoodsReceiptStatus.Completed)
                {
                    throw new InvalidOperationException("Only Completed GRNs can be billed.");
                }
            }

            // Execute 3-Way Match validation
            var matchResult = await Validate3WayMatchAsync(request, cancellationToken);
            if (matchResult.IsBlocked)
            {
                throw new InvalidOperationException($"Cannot create Purchase Bill due to 3-Way Match Block: {matchResult.DiagnosticRemarks}");
            }

            var billNumber = await _numberingService.GenerateNumberAsync(cancellationToken);
            var now = DateTime.UtcNow;

            var initialStatus = matchResult.HasVarianceWarning
                ? PurchaseBillStatus.VarianceHold
                : PurchaseBillStatus.PendingApproval;

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
                InvoiceDate = request.InvoiceDate.ToUniversalTime(),
                DueDate = request.DueDate.ToUniversalTime(),
                Currency = request.Currency ?? "INR",
                PaymentTerms = request.PaymentTerms ?? "Net 30 Days",
                CgstAmount = request.CgstAmount,
                SgstAmount = request.SgstAmount,
                IgstAmount = request.IgstAmount,
                TdsSection = request.TdsSection,
                TdsPercentage = request.TdsPercentage,
                TdsAmount = request.TdsAmount,
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
                    GoodsReceiptItemId = lineReq.GoodsReceiptItemId,
                    ItemId = lineReq.ItemId,
                    ItemName = lineReq.ItemName,
                    Description = lineReq.Description ?? string.Empty,
                    Quantity = q,
                    UnitPrice = p,
                    DiscountAmount = disc,
                    TaxPercent = taxPct,
                    TaxAmount = taxAmt,
                    TotalAmount = total
                });
            }

            var totals = PurchaseBillRules.CalculateTotals(entity.Lines.Select(l => (l.Quantity, l.UnitPrice, l.DiscountAmount, l.TaxPercent)));
            entity.SubTotal = totals.SubTotal;
            entity.DiscountTotal = totals.DiscountTotal;
            entity.TaxTotal = totals.TaxTotal;
            entity.RoundOff = totals.RoundOff;

            // Grand Total incorporating GST and deducting TDS
            var calculatedGrandTotal = totals.SubTotal - totals.DiscountTotal + request.CgstAmount + request.SgstAmount + request.IgstAmount - request.TdsAmount;
            entity.GrandTotal = Math.Max(0m, calculatedGrandTotal > 0 ? calculatedGrandTotal : totals.GrandTotal);
            entity.PaidAmount = 0m;
            entity.BalanceAmount = entity.GrandTotal;

            entity.History.Add(new PurchaseBillHistory
            {
                Status = initialStatus,
                Date = now,
                User = currentUser,
                Remarks = matchResult.HasVarianceWarning
                    ? $"Created under Variance Hold: {matchResult.DiagnosticRemarks}"
                    : "Bill created and submitted for approval"
            });

            _dbContext.PurchaseBills.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return MapBill(entity);
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
            if (!string.IsNullOrWhiteSpace(request.PaymentTerms)) entity.PaymentTerms = request.PaymentTerms;
            if (!string.IsNullOrWhiteSpace(request.Remarks)) entity.Remarks = request.Remarks;
            entity.UpdatedBy = currentUser;
            entity.UpdatedAt = now;

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
                    PurchaseOrderLineId = lineReq.PurchaseOrderLineId,
                    GoodsReceiptItemId = lineReq.GoodsReceiptItemId,
                    ItemId = lineReq.ItemId,
                    ItemName = lineReq.ItemName,
                    Description = lineReq.Description ?? string.Empty,
                    Quantity = q,
                    UnitPrice = p,
                    DiscountAmount = disc,
                    TaxPercent = taxPct,
                    TaxAmount = taxAmt,
                    TotalAmount = total
                });
            }

            var totals = PurchaseBillRules.CalculateTotals(entity.Lines.Select(l => (l.Quantity, l.UnitPrice, l.DiscountAmount, l.TaxPercent)));
            entity.SubTotal = totals.SubTotal;
            entity.DiscountTotal = totals.DiscountTotal;
            entity.TaxTotal = totals.TaxTotal;
            entity.RoundOff = totals.RoundOff;

            var calculatedGrandTotal = totals.SubTotal - totals.DiscountTotal + request.CgstAmount + request.SgstAmount + request.IgstAmount - request.TdsAmount;
            entity.GrandTotal = Math.Max(0m, calculatedGrandTotal > 0 ? calculatedGrandTotal : totals.GrandTotal);
            entity.BalanceAmount = Math.Max(0m, entity.GrandTotal - entity.PaidAmount);

            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapBill(entity);
        }

        public async Task<bool> DeletePurchaseBillAsync(int id, string currentUser, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.PurchaseBills.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            if (entity is null) return false;

            entity.IsDeleted = true;
            entity.UpdatedBy = currentUser;
            entity.UpdatedAt = DateTime.UtcNow;
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

            if (entity.Status != PurchaseBillStatus.Draft && entity.Status != PurchaseBillStatus.PendingApproval && entity.Status != PurchaseBillStatus.VarianceHold)
            {
                throw new InvalidOperationException($"Cannot approve bill in status {entity.Status}.");
            }

            var now = DateTime.UtcNow;
            entity.Status = PurchaseBillStatus.Approved;
            entity.UpdatedBy = currentUser;
            entity.UpdatedAt = now;

            entity.History.Add(new PurchaseBillHistory
            {
                Status = PurchaseBillStatus.Approved,
                Date = now,
                User = currentUser,
                Remarks = string.IsNullOrWhiteSpace(remarks) ? "Purchase Bill approved" : remarks
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

            if (entity.Status != PurchaseBillStatus.Approved)
            {
                throw new InvalidOperationException($"Only Approved Purchase Bills can be posted to Accounts Payable Ledger. Current status: {entity.Status}.");
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var now = DateTime.UtcNow;

                // Atomic line locking and PO BilledQuantity increment
                if (entity.PurchaseOrderId is > 0)
                {
                    var poLines = await _dbContext.PurchaseOrderLines
                        .Where(x => x.PurchaseOrderId == entity.PurchaseOrderId.Value)
                        .ToListAsync(cancellationToken);

                    foreach (var billLine in entity.Lines)
                    {
                        PurchaseOrderLine? poLine = null;
                        if (billLine.PurchaseOrderLineId is > 0)
                            poLine = poLines.FirstOrDefault(x => x.Id == billLine.PurchaseOrderLineId.Value);
                        else
                            poLine = poLines.FirstOrDefault(x => x.ItemName.Equals(billLine.ItemName, StringComparison.OrdinalIgnoreCase));

                        if (poLine != null)
                        {
                            poLine.BilledQuantity += billLine.Quantity;
                        }
                    }
                }

                // Insert Credit Entry to Vendor Ledger (AP Liability)
                var ledgerEntry = new VendorLedgerEntry
                {
                    VendorId = entity.VendorId,
                    VoucherNumber = entity.BillNumber,
                    EntryDate = entity.InvoiceDate,
                    EntryType = "Bill",
                    ReferenceId = entity.Id,
                    ReferenceNumber = entity.InvoiceNumber,
                    DebitAmount = 0m,
                    CreditAmount = entity.GrandTotal,
                    Narration = $"Purchase Bill Posted: {entity.BillNumber} (Inv: {entity.InvoiceNumber})",
                    CreatedAt = now
                };

                _dbContext.VendorLedgerEntries.Add(ledgerEntry);

                entity.Status = PurchaseBillStatus.Posted;
                entity.PostedAt = now;
                entity.UpdatedBy = currentUser;
                entity.UpdatedAt = now;

                entity.History.Add(new PurchaseBillHistory
                {
                    Status = PurchaseBillStatus.Posted,
                    Date = now,
                    User = currentUser,
                    Remarks = string.IsNullOrWhiteSpace(remarks) ? "Posted to Vendor AP Ledger" : remarks
                });

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return MapBill(entity);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<PurchaseBillDto?> PayPurchaseBillAsync(int id, string remarks, string currentUser, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.PurchaseBills
                .Include(x => x.Lines)
                .Include(x => x.History)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (entity is null) return null;

            entity.Status = PurchaseBillStatus.Paid;
            entity.PaymentStatus = PurchaseBillPaymentStatus.Paid;
            entity.PaidAmount = entity.GrandTotal;
            entity.BalanceAmount = 0m;
            entity.UpdatedBy = currentUser;
            entity.UpdatedAt = DateTime.UtcNow;

            entity.History.Add(new PurchaseBillHistory
            {
                Status = PurchaseBillStatus.Paid,
                Date = DateTime.UtcNow,
                User = currentUser,
                Remarks = string.IsNullOrWhiteSpace(remarks) ? "Full payment recorded" : remarks
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

            if (entity.Status == PurchaseBillStatus.Void)
            {
                return MapBill(entity);
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var now = DateTime.UtcNow;

                // Revert PO BilledQuantity if previously posted
                if (entity.Status == PurchaseBillStatus.Posted && entity.PurchaseOrderId is > 0)
                {
                    var poLines = await _dbContext.PurchaseOrderLines
                        .Where(x => x.PurchaseOrderId == entity.PurchaseOrderId.Value)
                        .ToListAsync(cancellationToken);

                    foreach (var billLine in entity.Lines)
                    {
                        PurchaseOrderLine? poLine = null;
                        if (billLine.PurchaseOrderLineId is > 0)
                            poLine = poLines.FirstOrDefault(x => x.Id == billLine.PurchaseOrderLineId.Value);
                        else
                            poLine = poLines.FirstOrDefault(x => x.ItemName.Equals(billLine.ItemName, StringComparison.OrdinalIgnoreCase));

                        if (poLine != null)
                        {
                            poLine.BilledQuantity = Math.Max(0m, poLine.BilledQuantity - billLine.Quantity);
                        }
                    }

                    // Write compensatory Debit Entry (DebitNote) in Vendor Ledger to offset AP Liability
                    var reverseEntry = new VendorLedgerEntry
                    {
                        VendorId = entity.VendorId,
                        VoucherNumber = $"VOID-{entity.BillNumber}",
                        EntryDate = now,
                        EntryType = "DebitNote",
                        ReferenceId = entity.Id,
                        ReferenceNumber = entity.InvoiceNumber,
                        DebitAmount = entity.GrandTotal,
                        CreditAmount = 0m,
                        Narration = $"Void Purchase Bill Reversal: {entity.BillNumber}. Reason: {remarks}",
                        CreatedAt = now
                    };

                    _dbContext.VendorLedgerEntries.Add(reverseEntry);
                }

                entity.Status = PurchaseBillStatus.Void;
                entity.UpdatedBy = currentUser;
                entity.UpdatedAt = now;

                entity.History.Add(new PurchaseBillHistory
                {
                    Status = PurchaseBillStatus.Void,
                    Date = now,
                    User = currentUser,
                    Remarks = string.IsNullOrWhiteSpace(remarks) ? "Purchase bill voided" : remarks
                });

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return MapBill(entity);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<PurchaseBillDto?> DuplicatePurchaseBillAsync(int id, string currentUser, CancellationToken cancellationToken = default)
        {
            var source = await _dbContext.PurchaseBills.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            if (source is null) return null;

            var req = new PurchaseBillCreateRequestDto
            {
                InvoiceNumber = $"{source.InvoiceNumber}-COPY",
                VendorId = source.VendorId,
                VendorName = source.VendorName,
                PurchaseOrderId = source.PurchaseOrderId,
                PurchaseOrderNumber = source.PurchaseOrderNumber,
                GRNId = source.GRNId,
                GRNNumber = source.GRNNumber,
                InvoiceDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(30),
                Currency = source.Currency,
                PaymentTerms = source.PaymentTerms,
                CgstAmount = source.CgstAmount,
                SgstAmount = source.SgstAmount,
                IgstAmount = source.IgstAmount,
                TdsSection = source.TdsSection,
                TdsPercentage = source.TdsPercentage,
                TdsAmount = source.TdsAmount,
                Remarks = $"Copy of {source.BillNumber}. {source.Remarks}".Trim(),
                Lines = source.Lines.Select(l => new PurchaseBillLineReqDto
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

            return await CreatePurchaseBillAsync(req, currentUser, cancellationToken);
        }

        public async Task<PurchaseBillDashboardDto> GetPurchaseBillDashboardAsync(CancellationToken cancellationToken = default)
        {
            var list = await _dbContext.PurchaseBills.Where(x => !x.IsDeleted).ToListAsync(cancellationToken);

            return new PurchaseBillDashboardDto
            {
                TotalBills = list.Count,
                DraftCount = list.Count(x => x.Status == PurchaseBillStatus.Draft),
                ApprovedCount = list.Count(x => x.Status == PurchaseBillStatus.Approved),
                PostedCount = list.Count(x => x.Status == PurchaseBillStatus.Posted),
                PaidCount = list.Count(x => x.Status == PurchaseBillStatus.Paid),
                TotalBilledAmount = list.Sum(x => x.GrandTotal),
                TotalOutstandingAmount = list.Sum(x => x.BalanceAmount)
            };
        }

        private static PurchaseBillDto MapBill(PurchaseBill entity) => new()
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
            InvoiceDate = entity.InvoiceDate,
            DueDate = entity.DueDate,
            Currency = entity.Currency,
            SubTotal = entity.SubTotal,
            DiscountTotal = entity.DiscountTotal,
            TaxTotal = entity.TaxTotal,
            CgstAmount = entity.CgstAmount,
            SgstAmount = entity.SgstAmount,
            IgstAmount = entity.IgstAmount,
            TdsSection = entity.TdsSection,
            TdsPercentage = entity.TdsPercentage,
            TdsAmount = entity.TdsAmount,
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
                GoodsReceiptItemId = l.GoodsReceiptItemId,
                ItemId = l.ItemId,
                ItemName = l.ItemName,
                Description = l.Description,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                DiscountAmount = l.DiscountAmount,
                TaxPercent = l.TaxPercent,
                TaxAmount = l.TaxAmount,
                TotalAmount = l.TotalAmount
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
