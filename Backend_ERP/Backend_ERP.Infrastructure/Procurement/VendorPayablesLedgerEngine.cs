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
using Microsoft.Extensions.Logging;

namespace ERP.Infrastructure.Procurement
{
    public class VendorPayablesLedgerEngine : IVendorPayablesLedgerEngine
    {
        private readonly ERPDbContext _dbContext;
        private readonly ILogger<VendorPayablesLedgerEngine> _logger;
        private readonly IProcurementSpendAnalyticsService _spendAnalytics;

        public VendorPayablesLedgerEngine(
            ERPDbContext dbContext,
            ILogger<VendorPayablesLedgerEngine> logger,
            IProcurementSpendAnalyticsService spendAnalytics)
        {
            _dbContext = dbContext;
            _logger = logger;
            _spendAnalytics = spendAnalytics;
        }

        public async Task<VendorLedgerEntry> PostBillPayableAsync(int purchaseBillId, int? userId = null, CancellationToken ct = default)
        {
            var bill = await _dbContext.PurchaseBills
                .FirstOrDefaultAsync(b => b.Id == purchaseBillId, ct)
                ?? throw new InvalidOperationException($"Purchase Bill #{purchaseBillId} not found.");

            // Check if already posted to ledger
            var existing = await _dbContext.VendorLedgerEntries
                .FirstOrDefaultAsync(e => e.PurchaseBillId == purchaseBillId && e.VoucherType == "Bill", ct);

            if (existing != null)
                return existing;

            var entryNumber = $"VLE-BIL-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

            var entry = new VendorLedgerEntry
            {
                TenantId = "TENANT-DEFAULT",
                EntryNumber = entryNumber,
                VendorId = bill.VendorId,
                PurchaseBillId = bill.Id,
                PostingDate = bill.InvoiceDate,
                VoucherType = "Bill",
                VoucherNumber = bill.BillNumber,
                ReferenceDocType = "PurchaseBill",
                ReferenceDocId = bill.Id,
                ReferenceDocNumber = bill.BillNumber,
                Currency = "INR",
                ExchangeRate = 1.0m,
                DebitAmount = 0m,
                CreditAmount = bill.TotalAmount,
                RunningBalance = 0m, // Evaluated by DB trigger
                SettledAmount = bill.PaidAmount,
                OutstandingAmount = bill.OutstandingAmount,
                SettlementStatus = bill.OutstandingAmount <= 0 ? "FullySettled" : (bill.PaidAmount > 0 ? "PartiallySettled" : "Unsettled"),
                Narration = $"Purchase Bill liability booked for invoice {bill.VendorInvoiceNumber} (Bill {bill.BillNumber})",
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.VendorLedgerEntries.Add(entry);
            await _dbContext.SaveChangesAsync(ct);

            await _spendAnalytics.InvalidateAnalyticsCacheAsync(ct);
            return entry;
        }

        public async Task<VendorLedgerEntry> PostPaymentDisbursementAsync(int vendorPaymentId, int? userId = null, CancellationToken ct = default)
        {
            var payment = await _dbContext.VendorPayments
                .FirstOrDefaultAsync(p => p.Id == vendorPaymentId, ct)
                ?? throw new InvalidOperationException($"Vendor Payment #{vendorPaymentId} not found.");

            var entryNumber = $"VLE-PAY-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

            var entry = new VendorLedgerEntry
            {
                TenantId = "TENANT-DEFAULT",
                EntryNumber = entryNumber,
                VendorId = payment.VendorId,
                PostingDate = payment.PaymentDate,
                VoucherType = "Payment",
                VoucherNumber = payment.PaymentNumber,
                ReferenceDocType = "VendorPayment",
                ReferenceDocId = payment.Id,
                ReferenceDocNumber = payment.PaymentNumber,
                Currency = "INR",
                ExchangeRate = 1.0m,
                DebitAmount = payment.TotalAmount,
                CreditAmount = 0m,
                RunningBalance = 0m, // Evaluated by DB trigger
                SettledAmount = payment.TotalAmount - payment.UnallocatedAmount,
                OutstandingAmount = payment.UnallocatedAmount,
                SettlementStatus = payment.UnallocatedAmount <= 0 ? "FullySettled" : "PartiallySettled",
                Narration = $"Disbursement payment {payment.PaymentNumber} via {payment.PaymentMethod} (Ref: {payment.ReferenceNumber})",
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.VendorLedgerEntries.Add(entry);
            await _dbContext.SaveChangesAsync(ct);

            await _spendAnalytics.InvalidateAnalyticsCacheAsync(ct);
            return entry;
        }

        public async Task<VendorLedgerEntry> PostDebitNoteAsync(int debitNoteId, int vendorId, decimal amount, string referenceNumber, string narration, int? userId = null, CancellationToken ct = default)
        {
            var entryNumber = $"VLE-DBN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

            var entry = new VendorLedgerEntry
            {
                TenantId = "TENANT-DEFAULT",
                EntryNumber = entryNumber,
                VendorId = vendorId,
                PostingDate = DateTime.UtcNow,
                VoucherType = "DebitNote",
                VoucherNumber = referenceNumber,
                ReferenceDocType = "DebitNote",
                ReferenceDocId = debitNoteId,
                ReferenceDocNumber = referenceNumber,
                Currency = "INR",
                ExchangeRate = 1.0m,
                DebitAmount = amount,
                CreditAmount = 0m,
                RunningBalance = 0m,
                SettledAmount = 0m,
                OutstandingAmount = amount,
                SettlementStatus = "Unsettled",
                Narration = narration,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.VendorLedgerEntries.Add(entry);
            await _dbContext.SaveChangesAsync(ct);

            await _spendAnalytics.InvalidateAnalyticsCacheAsync(ct);
            return entry;
        }

        public async Task<VendorBillSettlementResultDto> SettleBillAsync(SettleBillRequestDto request, int? userId = null, CancellationToken ct = default)
        {
            if (request.Allocations == null || request.Allocations.Count == 0)
                throw new ArgumentException("At least one bill allocation is required for settlement.", nameof(request));

            var vendor = await _dbContext.Vendors.FirstOrDefaultAsync(v => v.Id == request.VendorId, ct)
                ?? throw new InvalidOperationException($"Vendor #{request.VendorId} not found.");

            using var tx = await _dbContext.Database.BeginTransactionAsync(ct);
            try
            {
                decimal totalSettled = 0m;
                decimal totalRealizedFx = 0m;
                var auditEntries = new List<SettlementAuditEntryDto>();

                foreach (var alloc in request.Allocations)
                {
                    if (alloc.AllocatedAmount <= 0) continue;

                    var bill = await _dbContext.PurchaseBills
                        .FirstOrDefaultAsync(b => b.Id == alloc.PurchaseBillId, ct)
                        ?? throw new InvalidOperationException($"Bill #{alloc.PurchaseBillId} not found.");

                    if (bill.VendorId != request.VendorId)
                        throw new InvalidOperationException($"Bill #{bill.BillNumber} does not belong to Vendor #{request.VendorId}.");

                    decimal allocQty = Math.Min(alloc.AllocatedAmount, bill.OutstandingAmount);
                    if (allocQty <= 0) continue;

                    // Realized Foreign Exchange Gain/Loss:
                    // If bill was booked at rate R1 and payment executed at rate R2
                    // Difference = allocQty * (billRate - paymentRate)
                    decimal realizedFx = 0m;
                    if (alloc.PaymentExchangeRate > 0 && bill.ExchangeRate > 0)
                    {
                        realizedFx = Math.Round(allocQty * (bill.ExchangeRate - alloc.PaymentExchangeRate), 4);
                    }

                    var settlement = new VendorBillSettlement
                    {
                        TenantId = "TENANT-DEFAULT",
                        PurchaseBillId = bill.Id,
                        PaymentVoucherId = request.PaymentVoucherId,
                        DebitNoteId = request.DebitNoteId,
                        AllocatedAmount = allocQty,
                        SettlementDate = request.SettlementDate,
                        RealizedFxGainLoss = realizedFx,
                        AllocationNotes = request.AllocationNotes,
                        CreatedByUserId = userId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _dbContext.VendorBillSettlements.Add(settlement);

                    decimal prevOutstanding = bill.OutstandingAmount;
                    bill.OutstandingAmount -= allocQty;
                    bill.PaidAmount += allocQty;

                    if (bill.OutstandingAmount <= 0.001m)
                    {
                        bill.OutstandingAmount = 0m;
                        bill.Status = PurchaseBillStatus.Paid;
                        bill.PaymentStatus = PurchaseBillPaymentStatus.Paid;
                    }
                    else
                    {
                        bill.PaymentStatus = PurchaseBillPaymentStatus.PartiallyPaid;
                    }

                    // Update corresponding VendorLedgerEntry
                    var ledgerEntry = await _dbContext.VendorLedgerEntries
                        .FirstOrDefaultAsync(e => e.PurchaseBillId == bill.Id && e.VoucherType == "Bill", ct);

                    if (ledgerEntry != null)
                    {
                        ledgerEntry.SettledAmount += allocQty;
                        ledgerEntry.OutstandingAmount = bill.OutstandingAmount;
                        ledgerEntry.SettlementStatus = bill.OutstandingAmount <= 0 ? "FullySettled" : "PartiallySettled";
                    }

                    totalSettled += allocQty;
                    totalRealizedFx += realizedFx;

                    auditEntries.Add(new SettlementAuditEntryDto
                    {
                        SettlementId = settlement.Id,
                        PurchaseBillId = bill.Id,
                        BillNumber = bill.BillNumber,
                        AllocatedAmount = allocQty,
                        OutstandingBefore = prevOutstanding,
                        OutstandingAfter = bill.OutstandingAmount,
                        NewBillStatus = bill.Status.ToString(),
                        RealizedFxGainLoss = realizedFx
                    });
                }

                await _dbContext.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                await _spendAnalytics.InvalidateAnalyticsCacheAsync(ct);

                return new VendorBillSettlementResultDto
                {
                    Success = true,
                    VendorId = vendor.Id,
                    VendorName = vendor.Name,
                    TotalSettledAmount = totalSettled,
                    TotalRealizedFxGainLoss = totalRealizedFx,
                    BillsSettledCount = auditEntries.Count,
                    ClearedBills = auditEntries,
                    Message = $"Successfully settled {auditEntries.Count} bills totaling ₹{totalSettled:N2} for {vendor.Name}."
                };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                _logger.LogError(ex, "Failed to settle bills for vendor {VendorId}.", request.VendorId);
                throw;
            }
        }

        public async Task<VendorBillSettlementResultDto> AutoKnockOffFifoAsync(FifoKnockOffRequestDto request, int? userId = null, CancellationToken ct = default)
        {
            if (request.AvailablePaymentAmount <= 0)
                throw new ArgumentException("Available payment amount must be greater than zero.", nameof(request));

            // Load all open bills for the vendor sorted by InvoiceDate ASC, Id ASC
            var openBills = await _dbContext.PurchaseBills
                .Where(b => b.VendorId == request.VendorId && b.BalanceAmount > 0 && b.Status == PurchaseBillStatus.Posted)
                .OrderBy(b => b.InvoiceDate)
                .ThenBy(b => b.Id)
                .ToListAsync(ct);

            if (openBills.Count == 0)
            {
                return new VendorBillSettlementResultDto
                {
                    Success = true,
                    VendorId = request.VendorId,
                    TotalSettledAmount = 0m,
                    Message = "No open unpaid bills found for this vendor."
                };
            }

            decimal remainingPayment = request.AvailablePaymentAmount;
            var allocations = new List<SettleBillAllocationItemDto>();

            foreach (var bill in openBills)
            {
                if (remainingPayment <= 0) break;

                decimal allocateThis = Math.Min(bill.OutstandingAmount, remainingPayment);
                allocations.Add(new SettleBillAllocationItemDto
                {
                    PurchaseBillId = bill.Id,
                    AllocatedAmount = allocateThis,
                    PaymentExchangeRate = request.PaymentExchangeRate
                });

                remainingPayment -= allocateThis;
            }

            var settleReq = new SettleBillRequestDto
            {
                VendorId = request.VendorId,
                PaymentVoucherId = request.PaymentVoucherId,
                TotalSettlementAmount = request.AvailablePaymentAmount - remainingPayment,
                SettlementDate = DateTime.UtcNow,
                AllocationNotes = request.AllocationNotes ?? "Automated FIFO clearing",
                Allocations = allocations
            };

            return await SettleBillAsync(settleReq, userId, ct);
        }
    }
}
