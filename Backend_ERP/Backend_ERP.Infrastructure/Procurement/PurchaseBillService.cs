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
            // 3-Way Matching Validation: duplicate active bill prevention for same GRN
            if (request.GRNId is > 0)
            {
                var existingActiveBill = await _dbContext.PurchaseBills
                    .FirstOrDefaultAsync(x => x.GRNId == request.GRNId.Value && x.Status != PurchaseBillStatus.Void && !x.IsDeleted, cancellationToken);

                if (existingActiveBill is not null)
                {
                    throw new InvalidOperationException($"Purchase Bill {existingActiveBill.BillNumber} already exists for GRN {request.GRNNumber ?? request.GRNId.Value.ToString()}.");
                }

                // Verify GRN status is Completed
                var grn = await _dbContext.GoodsReceipts
                    .FirstOrDefaultAsync(x => x.Id == request.GRNId.Value && !x.IsDeleted, cancellationToken);

                if (grn is not null && grn.Status != GoodsReceiptStatus.Completed)
                {
                    throw new InvalidOperationException("Only Completed GRNs can be billed.");
                }
            }

            var billNumber = await _numberingService.GenerateNumberAsync(cancellationToken);
            var now = DateTime.UtcNow;

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
                PaymentStatus = PurchaseBillPaymentStatus.Unpaid,
                Status = PurchaseBillStatus.Draft,
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
            entity.GrandTotal = totals.GrandTotal;
            entity.PaidAmount = 0m;
            entity.BalanceAmount = totals.GrandTotal;

            entity.History.Add(new PurchaseBillHistory
            {
                Status = PurchaseBillStatus.Draft,
                Date = now,
                User = currentUser,
                Remarks = "Bill created"
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

            if (!PurchaseBillRules.CanEdit(entity.Status))
            {
                throw new InvalidOperationException($"Only Draft purchase bills can be edited. Current status: {entity.Status}.");
            }

            var now = DateTime.UtcNow;
            entity.InvoiceNumber = request.InvoiceNumber;
            entity.VendorId = request.VendorId;
            entity.VendorName = request.VendorName;
            entity.InvoiceDate = request.InvoiceDate.ToUniversalTime();
            entity.DueDate = request.DueDate.ToUniversalTime();
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
            entity.GrandTotal = totals.GrandTotal;
            entity.BalanceAmount = Math.Max(0m, totals.GrandTotal - entity.PaidAmount);

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
            return await TransitionStatusAsync(id, PurchaseBillStatus.Approved, remarks, currentUser, cancellationToken);
        }

        public async Task<PurchaseBillDto?> PostPurchaseBillAsync(int id, string remarks, string currentUser, CancellationToken cancellationToken = default)
        {
            return await TransitionStatusAsync(id, PurchaseBillStatus.Posted, remarks, currentUser, cancellationToken);
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
            return await TransitionStatusAsync(id, PurchaseBillStatus.Void, remarks, currentUser, cancellationToken);
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
                Remarks = $"Copy of {source.BillNumber}. {source.Remarks}".Trim(),
                Lines = source.Lines.Select(l => new PurchaseBillLineReqDto
                {
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

        private async Task<PurchaseBillDto?> TransitionStatusAsync(int id, PurchaseBillStatus targetStatus, string remarks, string currentUser, CancellationToken cancellationToken)
        {
            var entity = await _dbContext.PurchaseBills
                .Include(x => x.Lines)
                .Include(x => x.History)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (entity is null) return null;

            if (!PurchaseBillRules.CanTransition(entity.Status, targetStatus))
            {
                throw new InvalidOperationException($"Cannot transition purchase bill from {entity.Status} to {targetStatus}.");
            }

            var now = DateTime.UtcNow;
            entity.Status = targetStatus;
            entity.UpdatedBy = currentUser;
            entity.UpdatedAt = now;

            entity.History.Add(new PurchaseBillHistory
            {
                Status = targetStatus,
                Date = now,
                User = currentUser,
                Remarks = string.IsNullOrWhiteSpace(remarks) ? $"Status changed to {targetStatus}" : remarks
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapBill(entity);
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
