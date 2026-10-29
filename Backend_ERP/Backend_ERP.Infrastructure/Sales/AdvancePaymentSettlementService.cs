using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Infrastructure.Sales
{
    public class AdvancePaymentSettlementService : IAdvancePaymentSettlementService
    {
        private readonly ERPDbContext _db;
        private readonly IAdvancePaymentNumberingService _numbering;
        private readonly IMtoProductionTriggerService _mtoTrigger;
        private readonly ILogger<AdvancePaymentSettlementService> _logger;

        public AdvancePaymentSettlementService(
            ERPDbContext db,
            IAdvancePaymentNumberingService numbering,
            IMtoProductionTriggerService mtoTrigger,
            ILogger<AdvancePaymentSettlementService> logger)
        {
            _db = db;
            _numbering = numbering;
            _mtoTrigger = mtoTrigger;
            _logger = logger;
        }

        public async Task<AdvancePaymentDto> ClearPaymentAsync(
            int paymentId,
            AdvancePaymentClearRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var isRelational = _db.Database.IsRelational();
            await using var tx = isRelational ? await _db.Database.BeginTransactionAsync(cancellationToken) : null;

            var entity = await LockAdvancePaymentAsync(paymentId, cancellationToken);
            if (entity is null)
            {
                throw new KeyNotFoundException($"Advance payment #{paymentId} not found.");
            }

            if (entity.Status == AdvancePaymentStatuses.Cancelled || entity.Status == AdvancePaymentStatuses.Rejected)
            {
                throw new InvalidOperationException($"Cannot clear an advance payment in status '{entity.Status}'.");
            }

            var now = DateTimeOffset.UtcNow;
            var oldStatus = entity.Status;

            // Transition to Cleared
            entity.Status = AdvancePaymentStatuses.Cleared;
            entity.ReconciliationStatus = "Reconciled";
            entity.BankReconciledOn = now;
            entity.BankStatementReference = request.BankStatementReference?.Trim();
            entity.ReceivedBy = actingUser;
            entity.ReceivedOn = now;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;

            if (!string.IsNullOrWhiteSpace(request.Remarks))
            {
                entity.Remarks = string.IsNullOrWhiteSpace(entity.Remarks)
                    ? request.Remarks.Trim()
                    : $"{entity.Remarks} | BRS Clear: {request.Remarks.Trim()}";
            }

            // Ensure Statutory GST Receipt Voucher is generated (CGST Act Section 31(3)(d))
            var hasReceiptVoucher = await _db.AdvancePaymentReceiptVouchers
                .AnyAsync(v => v.AdvancePaymentId == entity.Id, cancellationToken);

            if (!hasReceiptVoucher)
            {
                var pos = string.IsNullOrWhiteSpace(entity.PlaceOfSupply) ? "Maharashtra" : entity.PlaceOfSupply;
                var isInterState = !string.Equals(pos, "Maharashtra", StringComparison.OrdinalIgnoreCase);

                decimal cgstRate = isInterState ? 0m : 9.00m;
                decimal sgstRate = isInterState ? 0m : 9.00m;
                decimal igstRate = isInterState ? 18.00m : 0m;
                decimal taxRate = isInterState ? igstRate : (cgstRate + sgstRate);

                decimal taxableAmount = Math.Round(entity.AdvanceAmount / (1m + (taxRate / 100m)), 2, MidpointRounding.AwayFromZero);
                decimal totalTax = entity.AdvanceAmount - taxableAmount;
                decimal cgstAmount = isInterState ? 0m : Math.Round(totalTax / 2m, 2, MidpointRounding.AwayFromZero);
                decimal sgstAmount = isInterState ? 0m : (totalTax - cgstAmount);
                decimal igstAmount = isInterState ? totalTax : 0m;

                var voucherNumber = await _numbering.GenerateNextReceiptVoucherNumberAsync(cancellationToken);
                int customerIdInt = int.TryParse(entity.CustomerId, out var parsedCid) ? parsedCid : 1;

                var voucher = new AdvancePaymentReceiptVoucher
                {
                    TenantId = entity.TenantId,
                    VoucherNumber = voucherNumber,
                    AdvancePaymentId = entity.Id,
                    CustomerId = customerIdInt,
                    CustomerName = entity.CustomerName,
                    PlaceOfSupply = pos,
                    PlaceOfSupplyStateCode = entity.PlaceOfSupplyStateCode,
                    IsInterState = isInterState,
                    TaxableAmount = taxableAmount,
                    CgstRate = cgstRate,
                    CgstAmount = cgstAmount,
                    SgstRate = sgstRate,
                    SgstAmount = sgstAmount,
                    IgstRate = igstRate,
                    IgstAmount = igstAmount,
                    TotalVoucherAmount = entity.AdvanceAmount,
                    IsAdjusted = false,
                    VoucherDate = entity.PaymentDate,
                    CreatedDate = now,
                    CreatedBy = actingUser
                };

                _db.AdvancePaymentReceiptVouchers.Add(voucher);
                entity.Timeline.Add(new AdvancePaymentTimeline
                {
                    AdvancePaymentId = entity.Id,
                    Action = AdvancePaymentTimelineActions.Created,
                    Remarks = $"Section 31(3)(d) GST Receipt Voucher {voucherNumber} generated upon BRS clearance.",
                    PerformedBy = actingUser,
                    PerformedOn = now
                });
            }

            entity.Timeline.Add(new AdvancePaymentTimeline
            {
                AdvancePaymentId = entity.Id,
                Action = AdvancePaymentTimelineActions.Cleared,
                Remarks = $"BRS Clearance confirmed ({oldStatus} â†’ Cleared). Ref: {request.BankStatementReference ?? "N/A"}",
                PerformedBy = actingUser,
                PerformedOn = now
            });

            await _db.SaveChangesAsync(cancellationToken);

            // Synchronize Two-Tier Operational Release Gates for all linked Sales Orders
            var linkedSoIds = entity.Applications
                .Where(a => !a.IsReversal)
                .Select(a => a.SalesOrderId)
                .Distinct()
                .ToList();

            if (entity.SalesOrderId.HasValue && !linkedSoIds.Contains(entity.SalesOrderId.Value))
            {
                linkedSoIds.Add(entity.SalesOrderId.Value);
            }

            foreach (var soId in linkedSoIds)
            {
                await SynchronizeOperationalGatesAsync(soId, cancellationToken);
            }

            if (tx is not null)
            {
                await tx.CommitAsync(cancellationToken);
            }

            return AdvancePaymentMapper.ToDto(entity);
        }

        public async Task<AdvancePaymentDto> DishonorPaymentAsync(
            int paymentId,
            AdvancePaymentDishonorRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.DishonorReason))
            {
                throw new InvalidOperationException("Dishonor reason is mandatory for BRS instrument bounces.");
            }

            var isRelational = _db.Database.IsRelational();
            await using var tx = isRelational ? await _db.Database.BeginTransactionAsync(cancellationToken) : null;

            // Deterministic row locking: find all linked sales orders first
            var linkedSoIds = await _db.AdvancePaymentApplications
                .Where(a => a.AdvancePaymentId == paymentId && !a.IsReversal)
                .Select(a => a.SalesOrderId)
                .Distinct()
                .OrderBy(id => id)
                .ToListAsync(cancellationToken);

            // 1. Lock Sales Orders FIRST
            var lockedOrders = new List<SalesOrder>();
            foreach (var soId in linkedSoIds)
            {
                var so = await LockSalesOrderAsync(soId, cancellationToken);
                if (so != null)
                {
                    lockedOrders.Add(so);
                }
            }

            // 2. Lock Advance Payment SECOND
            var entity = await LockAdvancePaymentAsync(paymentId, cancellationToken);
            if (entity is null)
            {
                throw new KeyNotFoundException($"Advance payment #{paymentId} not found.");
            }

            var now = DateTimeOffset.UtcNow;
            var oldStatus = entity.Status;

            entity.Status = AdvancePaymentStatuses.Dishonored;
            entity.ReconciliationStatus = "Dishonored";
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;
            entity.Remarks = string.IsNullOrWhiteSpace(entity.Remarks)
                ? $"DISHONORED: {request.DishonorReason}"
                : $"{entity.Remarks} | DISHONORED: {request.DishonorReason}";

            // Atomic Compensating Reversals on all active child applications
            var activeApps = entity.Applications.Where(a => !a.IsReversal).ToList();
            foreach (var app in activeApps)
            {
                var reversal = new AdvancePaymentApplication
                {
                    TenantId = entity.TenantId,
                    AdvancePaymentId = entity.Id,
                    SalesOrderId = app.SalesOrderId,
                    SalesOrderNumber = app.SalesOrderNumber,
                    ApplyAmount = app.ApplyAmount,
                    ExchangeRateAtAllocation = app.ExchangeRateAtAllocation,
                    RealizedFxGainLoss = -app.RealizedFxGainLoss,
                    IsReversal = true,
                    ReversalReason = $"Compensating Reversal due to Dishonored Instrument: {request.DishonorReason}",
                    OriginalApplicationId = app.Id,
                    Remarks = $"Dishonor bounce reversal of â‚¹{app.ApplyAmount:N2}",
                    AppliedBy = actingUser,
                    AppliedOn = now
                };
                entity.Applications.Add(reversal);
            }

            // Reset balances since the financial funds never materialized
            entity.AppliedAmount = 0m;
            entity.RemainingAmount = 0m;

            // Rollback and freeze operational gates on locked Sales Orders
            foreach (var so in lockedOrders)
            {
                // Calculate actual valid active allocated advances
                var validTotal = await _db.AdvancePaymentApplications
                    .Where(a => a.SalesOrderId == so.Id && a.AdvancePaymentId != entity.Id)
                    .Join(_db.AdvancePayments,
                        a => a.AdvancePaymentId,
                        ap => ap.Id,
                        (a, ap) => new { a, ap })
                    .Where(x => !x.ap.IsDeleted && x.ap.Status != AdvancePaymentStatuses.Dishonored &&
                                x.ap.Status != AdvancePaymentStatuses.Cancelled &&
                                x.ap.Status != AdvancePaymentStatuses.Refunded)
                    .SumAsync(x => x.a.IsReversal ? -x.a.ApplyAmount : x.a.ApplyAmount, cancellationToken);

                if (validTotal < 0) validTotal = 0;
                so.AdvanceAllocatedAmount = validTotal;
                so.CreditHoldReason = $"Operational Freeze: Instrument for payment {entity.PaymentNumber} dishonored. {request.DishonorReason}";
                so.UpdatedBy = actingUser;
                so.UpdatedDate = now;

                // Trip Tier 2 Dispatch Gate
                if (so.GrandTotal > 0 && so.AdvanceAllocatedAmount < so.GrandTotal)
                {
                    so.IsDispatchReleased = false;
                    so.DispatchReleasedDate = null;
                    so.DispatchReleasedBy = null;
                }

                // Trip Tier 1 Production Gate if below 30%
                if (so.GrandTotal > 0 && (so.AdvanceAllocatedAmount / so.GrandTotal) < 0.30m)
                {
                    so.IsProductionReleased = false;
                    so.ProductionReleasedDate = null;
                    so.ProductionReleasedBy = null;
                }
            }

            entity.Timeline.Add(new AdvancePaymentTimeline
            {
                AdvancePaymentId = entity.Id,
                Action = AdvancePaymentTimelineActions.Dishonored,
                Remarks = $"Instrument DISHONORED. Allocations reversed and linked orders frozen on credit hold. Reason: {request.DishonorReason}",
                PerformedBy = actingUser,
                PerformedOn = now
            });

            await _db.SaveChangesAsync(cancellationToken);

            if (tx is not null)
            {
                await tx.CommitAsync(cancellationToken);
            }

            _logger.LogWarning("Advance payment {PaymentNumber} dishonored. Reversed {AppCount} allocations across {OrderCount} sales orders.",
                entity.PaymentNumber, activeApps.Count, lockedOrders.Count);

            return AdvancePaymentMapper.ToDto(entity);
        }

        public async Task<AdvancePaymentAdjustmentDto> AdjustAgainstTaxInvoiceAsync(
            AdvancePaymentAdjustmentRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            if (request.AdjustedAmount <= 0)
            {
                throw new InvalidOperationException("Adjustment amount must be greater than zero.");
            }

            var isRelational = _db.Database.IsRelational();
            await using var tx = isRelational ? await _db.Database.BeginTransactionAsync(cancellationToken) : null;

            var entity = await LockAdvancePaymentAsync(request.AdvancePaymentId, cancellationToken);
            if (entity is null)
            {
                throw new KeyNotFoundException($"Advance payment #{request.AdvancePaymentId} not found.");
            }

            var adjustedAmount = AdvancePaymentCalculator.Round2(request.AdjustedAmount);
            if (adjustedAmount > entity.RemainingAmount)
            {
                throw new InvalidOperationException(
                    $"Adjustment amount {adjustedAmount:N2} exceeds available float {entity.RemainingAmount:N2}.");
            }

            var now = DateTimeOffset.UtcNow;

            // Multi-Currency Realized FX Gain/Loss Calculation
            var advanceRate = entity.ExchangeRate > 0 ? entity.ExchangeRate : 1.0000m;
            var adjRate = request.ExchangeRateAtAdjustment > 0 ? request.ExchangeRateAtAdjustment : advanceRate;
            var realizedFx = Math.Round(adjustedAmount * (adjRate - advanceRate), 2, MidpointRounding.AwayFromZero);

            // Reconcile GSTR-1 Table 11B Tax Offset
            var unadjustedVoucher = await _db.AdvancePaymentReceiptVouchers
                .FirstOrDefaultAsync(v => v.AdvancePaymentId == entity.Id && !v.IsAdjusted, cancellationToken);

            string? gstr1Ref = null;
            if (unadjustedVoucher != null)
            {
                unadjustedVoucher.IsAdjusted = true;
                gstr1Ref = unadjustedVoucher.VoucherNumber;
            }

            // Update Advance Payment Balances
            entity.AppliedAmount = AdvancePaymentCalculator.Round2(entity.AppliedAmount + adjustedAmount);
            entity.RemainingAmount = AdvancePaymentCalculator.CalcRemaining(
                entity.AdvanceAmount,
                entity.AppliedAmount,
                entity.RefundedAmount,
                entity.ForfeitedAmount);

            entity.Status = AdvancePaymentCalculator.ResolveStatusAfterApply(entity.RemainingAmount);
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;

            var adjustmentDate = AdvancePaymentMapper.ParseOptionalDate(request.AdjustmentDate)
                ?? DateOnly.FromDateTime(DateTime.UtcNow);

            var adjustment = new AdvancePaymentAdjustment
            {
                TenantId = entity.TenantId,
                AdvancePaymentId = entity.Id,
                SalesInvoiceId = request.SalesInvoiceId,
                InvoiceNumber = request.InvoiceNumber.Trim(),
                AdjustedAmount = adjustedAmount,
                AdjustmentDate = adjustmentDate,
                ExchangeRateAtAdjustment = adjRate,
                RealizedFxGainLoss = realizedFx,
                Gstr1VoucherRef = gstr1Ref,
                Remarks = request.Remarks?.Trim() ?? string.Empty,
                CreatedDate = now,
                CreatedBy = actingUser
            };

            _db.AdvancePaymentAdjustments.Add(adjustment);

            entity.Timeline.Add(new AdvancePaymentTimeline
            {
                AdvancePaymentId = entity.Id,
                Action = AdvancePaymentTimelineActions.Adjusted,
                Remarks = $"Adjusted â‚¹{adjustedAmount:N2} against Tax Invoice '{request.InvoiceNumber}'. FX Drift: â‚¹{realizedFx:N2} at {adjRate:F4}. GSTR-1 Offset: {gstr1Ref ?? "None"}",
                PerformedBy = actingUser,
                PerformedOn = now
            });

            await _db.SaveChangesAsync(cancellationToken);

            if (tx is not null)
            {
                await tx.CommitAsync(cancellationToken);
            }

            return AdvancePaymentMapper.ToAdjustmentDto(adjustment);
        }

        public async Task SynchronizeOperationalGatesAsync(int salesOrderId, CancellationToken cancellationToken = default)
        {
            var so = await _db.SalesOrders
                .FirstOrDefaultAsync(x => x.Id == salesOrderId, cancellationToken);

            if (so == null) return;

            var hasApps = await _db.AdvancePaymentApplications
                .AnyAsync(a => a.SalesOrderId == salesOrderId, cancellationToken);

            decimal totalCleared;
            if (hasApps)
            {
                totalCleared = await _db.AdvancePaymentApplications
                    .Where(a => a.SalesOrderId == salesOrderId)
                    .Join(_db.AdvancePayments,
                        a => a.AdvancePaymentId,
                        ap => ap.Id,
                        (a, ap) => new { a, ap })
                    .Where(x => !x.ap.IsDeleted &&
                                x.ap.Status != AdvancePaymentStatuses.Dishonored &&
                                x.ap.Status != AdvancePaymentStatuses.Cancelled &&
                                x.ap.Status != AdvancePaymentStatuses.Refunded)
                    .SumAsync(x => x.a.IsReversal ? -x.a.ApplyAmount : x.a.ApplyAmount, cancellationToken);

                if (totalCleared < 0) totalCleared = 0;
                so.AdvanceAllocatedAmount = totalCleared;
            }
            else
            {
                totalCleared = so.AdvanceAllocatedAmount;
            }

            var grandTotal = so.GrandTotal;
            var now = DateTimeOffset.UtcNow;

            // Tier 1 Production Release (>= 30%)
            var reachedTier1 = grandTotal > 0 && (totalCleared / grandTotal) >= 0.30m;
            if (reachedTier1 && !so.IsProductionReleased)
            {
                so.IsProductionReleased = true;
                so.ProductionReleasedDate = now;
                so.ProductionReleasedBy = "SystemSync";

                // Evaluate MTO and elevate shop-floor work orders
                try
                {
                    await _mtoTrigger.EvaluateAndSpawnProductionAsync(so.Id, "SystemSync", cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to spawn MTO production for sales order {SalesOrderId} after Tier 1 release.", so.Id);
                }
            }
            else if (!reachedTier1 && so.IsProductionReleased)
            {
                so.IsProductionReleased = false;
                so.ProductionReleasedDate = null;
                so.ProductionReleasedBy = null;
            }

            // Tier 2 Dispatch Release (>= 100%)
            var reachedTier2 = grandTotal > 0 && totalCleared >= grandTotal;
            if (reachedTier2 && !so.IsDispatchReleased)
            {
                so.IsDispatchReleased = true;
                so.DispatchReleasedDate = now;
                so.DispatchReleasedBy = "SystemSync";
            }
            else if (!reachedTier2 && so.IsDispatchReleased)
            {
                // Check if credit customer has waiver
                var isEnterprise = string.Equals(so.CustomerCategory, "Enterprise", StringComparison.OrdinalIgnoreCase) ||
                                  so.PaymentTerms.Contains("Net", StringComparison.OrdinalIgnoreCase);
                if (!isEnterprise)
                {
                    so.IsDispatchReleased = false;
                    so.DispatchReleasedDate = null;
                    so.DispatchReleasedBy = null;
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task<AdvancePayment?> LockAdvancePaymentAsync(int id, CancellationToken cancellationToken)
        {
            if (_db.Database.IsRelational())
            {
                return await _db.AdvancePayments
                    .FromSqlRaw(@"SELECT * FROM advance_payments WHERE ""Id"" = {0} AND ""IsDeleted"" = false FOR UPDATE", id)
                    .Include(x => x.Applications)
                    .Include(x => x.Adjustments)
                    .Include(x => x.Timeline)
                    .Include(x => x.ReceiptVouchers)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            return await _db.AdvancePayments
                .Include(x => x.Applications)
                .Include(x => x.Adjustments)
                .Include(x => x.Timeline)
                .Include(x => x.ReceiptVouchers)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        }

        private async Task<SalesOrder?> LockSalesOrderAsync(int salesOrderId, CancellationToken cancellationToken)
        {
            if (_db.Database.IsRelational())
            {
                return await _db.SalesOrders
                    .FromSqlRaw(@"SELECT *, xmin FROM sales_orders WHERE ""Id"" = {0} FOR UPDATE", salesOrderId)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            return await _db.SalesOrders
                .FirstOrDefaultAsync(x => x.Id == salesOrderId, cancellationToken);
        }
    }
}
