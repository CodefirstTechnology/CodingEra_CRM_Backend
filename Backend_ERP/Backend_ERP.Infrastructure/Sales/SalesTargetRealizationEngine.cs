using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Sales.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Infrastructure.Sales
{
    public class SalesTargetRealizationEngine : ISalesTargetRealizationEngine
    {
        private readonly ERPDbContext _context;
        private readonly ITargetRedisCacheManager _cacheManager;
        private readonly ILogger<SalesTargetRealizationEngine> _logger;

        public SalesTargetRealizationEngine(
            ERPDbContext context,
            ITargetRedisCacheManager cacheManager,
            ILogger<SalesTargetRealizationEngine> logger)
        {
            _context = context;
            _cacheManager = cacheManager;
            _logger = logger;
        }

        public async Task RecordOrderBookingAsync(int salesOrderId, CancellationToken ct = default)
        {
            var order = await _context.SalesOrders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == salesOrderId, ct);

            if (order == null)
            {
                _logger.LogWarning("Sales order #{OrderId} not found for realization", salesOrderId);
                return;
            }

            var orderDate = order.OrderDate != default
                ? order.OrderDate
                : DateOnly.FromDateTime(order.CreatedDate.UtcDateTime);

            var spUserId = order.SalesPersonUserId
                ?? (int.TryParse(order.SalesPerson, out var parsedId) ? parsedId : (int?)null);

            // Commercial booking value is strictly net of statutory GST
            var netBookingAmount = order.Subtotal - order.DiscountTotal;
            if (netBookingAmount <= 0)
            {
                netBookingAmount = Math.Max(0, order.GrandTotal - order.GstTotal);
            }

            var totalQuantity = order.Items?.Sum(i => i.Quantity) ?? 0m;

            // Find all active targets across dimensions covering this order date
            var candidateTargets = await _context.SalesTargets
                .Include(t => t.Realizations)
                .Include(t => t.ProgressHistory)
                .Where(t => !t.IsDeleted && !t.IsLocked)
                .Where(t => t.Status == SalesTargetStatuses.Active)
                .Where(t => t.StartDate <= orderDate && t.EndDate >= orderDate)
                .ToListAsync(ct);

            var matchedTargets = candidateTargets.Where(t =>
                (spUserId.HasValue && t.SalesPersonUserId == spUserId.Value) ||
                (!string.IsNullOrEmpty(t.SalesTeam) && t.SalesTeam.Equals(order.SalesPerson, StringComparison.OrdinalIgnoreCase)) ||
                (t.AssignmentType == SalesTargetAssignmentTypes.CompanyWide)
            ).ToList();

            if (!matchedTargets.Any())
            {
                _logger.LogInformation("No active sales targets matched for Sales Order #{OrderNumber}", order.SalesOrderNumber);
                return;
            }

            foreach (var target in matchedTargets)
            {
                // Prevent duplicate realization of the same order booking
                bool alreadyRealized = target.Realizations.Any(r =>
                    r.SalesOrderId == order.Id &&
                    r.RealizationType == SalesTargetRealizationTypes.BookingNet &&
                    !r.IsReversal);

                if (alreadyRealized) continue;

                // Normalize currency if order was placed in FCY
                decimal fxRate = target.LockedExchangeRate > 0 ? target.LockedExchangeRate : 1.000000m;
                decimal inrAmount = netBookingAmount * fxRate;

                var realization = new SalesTargetRealization
                {
                    TenantId = target.TenantId,
                    SalesTargetId = target.Id,
                    SalesOrderId = order.Id,
                    QuotationId = order.QuotationId,
                    RealizationType = SalesTargetRealizationTypes.BookingNet,
                    RealizedAmount = inrAmount,
                    RealizedQuantity = totalQuantity,
                    MarginContributionAmount = Math.Round(inrAmount * 0.20m, 2), // Standard baseline contribution
                    SourceCurrency = "INR",
                    SourceAmount = netBookingAmount,
                    LockedFXRate = fxRate,
                    IsReversal = false,
                    TransactionDate = DateTimeOffset.UtcNow,
                    AppliedOn = DateTimeOffset.UtcNow,
                    AppliedBy = "SYSTEM_SO_CONFIRMATION"
                };

                target.Realizations.Add(realization);

                // Synchronize in-memory model (database trigger also recalculates atomically on commit)
                target.RealizedRevenue += inrAmount;
                target.AchievedValue = target.RealizedRevenue;
                target.RemainingValue = Math.Max(0m, target.RevenueTarget - target.AchievedValue);
                target.OverAchievementValue = Math.Max(0m, target.AchievedValue - target.RevenueTarget);
                target.AchievementPercentage = target.RevenueTarget > 0
                    ? Math.Round((target.AchievedValue / target.RevenueTarget) * 100m, 2)
                    : 0m;

                target.ProgressHistory.Add(new SalesTargetProgressHistory
                {
                    SalesTargetId = target.Id,
                    OldAchievedValue = target.AchievedValue - inrAmount,
                    NewAchievedValue = target.AchievedValue,
                    AchievementPercentage = target.AchievementPercentage,
                    Action = SalesTargetProgressActions.Realization,
                    Remarks = $"Booking credited from SO: {order.SalesOrderNumber}",
                    UpdatedBy = "SYSTEM",
                    UpdatedOn = DateTimeOffset.UtcNow
                });
            }

            await _context.SaveChangesAsync(ct);
            await _cacheManager.InvalidateTenantAsync("default");
        }

        public async Task ProcessOrderClawbackAsync(int salesOrderId, string reason, CancellationToken ct = default)
        {
            var realizations = await _context.SalesTargetRealizations
                .Include(r => r.Target)
                .Where(r => r.SalesOrderId == salesOrderId && !r.IsReversal)
                .ToListAsync(ct);

            if (!realizations.Any()) return;

            foreach (var orig in realizations)
            {
                var target = orig.Target;
                if (target == null) continue;

                var reversal = new SalesTargetRealization
                {
                    TenantId = orig.TenantId,
                    SalesTargetId = orig.SalesTargetId,
                    SalesOrderId = orig.SalesOrderId,
                    QuotationId = orig.QuotationId,
                    RealizationType = orig.RealizationType,
                    RealizedAmount = orig.RealizedAmount,
                    RealizedQuantity = orig.RealizedQuantity,
                    MarginContributionAmount = orig.MarginContributionAmount,
                    SourceCurrency = orig.SourceCurrency,
                    SourceAmount = orig.SourceAmount,
                    LockedFXRate = orig.LockedFXRate,
                    IsReversal = true,
                    ReversalOfRealizationId = orig.Id,
                    ReversalReason = reason,
                    TransactionDate = DateTimeOffset.UtcNow,
                    AppliedOn = DateTimeOffset.UtcNow,
                    AppliedBy = "SYSTEM_SO_CLAWBACK"
                };

                _context.SalesTargetRealizations.Add(reversal);

                // Synchronize in-memory model
                target.RealizedRevenue = Math.Max(0m, target.RealizedRevenue - orig.RealizedAmount);
                target.AchievedValue = target.RealizedRevenue;
                target.RemainingValue = Math.Max(0m, target.RevenueTarget - target.AchievedValue);
                target.OverAchievementValue = Math.Max(0m, target.AchievedValue - target.RevenueTarget);
                target.AchievementPercentage = target.RevenueTarget > 0
                    ? Math.Round((target.AchievedValue / target.RevenueTarget) * 100m, 2)
                    : 0m;

                target.ProgressHistory.Add(new SalesTargetProgressHistory
                {
                    SalesTargetId = target.Id,
                    OldAchievedValue = target.AchievedValue + orig.RealizedAmount,
                    NewAchievedValue = target.AchievedValue,
                    AchievementPercentage = target.AchievementPercentage,
                    Action = SalesTargetProgressActions.Reversal,
                    Remarks = $"Clawback from SO #{salesOrderId}: {reason}",
                    UpdatedBy = "SYSTEM",
                    UpdatedOn = DateTimeOffset.UtcNow
                });
            }

            await _context.SaveChangesAsync(ct);
            await _cacheManager.InvalidateTenantAsync("default");
        }

        public async Task RecordAdvanceCashAsync(int advancePaymentId, CancellationToken ct = default)
        {
            var payment = await _context.AdvancePayments
                .Include(p => p.SalesOrder)
                .FirstOrDefaultAsync(p => p.Id == advancePaymentId, ct);

            if (payment == null || payment.AdvanceAmount <= 0) return;

            var paymentDate = payment.PaymentDate;
            int? spUserId = payment.SalesOrder?.SalesPersonUserId;

            var candidateTargets = await _context.SalesTargets
                .Include(t => t.Realizations)
                .Where(t => !t.IsDeleted && !t.IsLocked)
                .Where(t => t.Status == SalesTargetStatuses.Active)
                .Where(t => t.StartDate <= paymentDate && t.EndDate >= paymentDate)
                .ToListAsync(ct);

            var matched = candidateTargets.Where(t =>
                (spUserId.HasValue && t.SalesPersonUserId == spUserId.Value) ||
                t.AssignmentType == SalesTargetAssignmentTypes.CompanyWide
            ).ToList();

            foreach (var target in matched)
            {
                bool alreadyRecorded = target.Realizations.Any(r =>
                    r.AdvancePaymentId == payment.Id &&
                    r.RealizationType == SalesTargetRealizationTypes.CashCollected &&
                    !r.IsReversal);

                if (alreadyRecorded) continue;

                decimal fx = target.LockedExchangeRate > 0 ? target.LockedExchangeRate : (payment.ExchangeRate > 0 ? payment.ExchangeRate : 1.0m);
                decimal inrCash = payment.AdvanceAmount * fx;

                var realization = new SalesTargetRealization
                {
                    TenantId = target.TenantId,
                    SalesTargetId = target.Id,
                    AdvancePaymentId = payment.Id,
                    SalesOrderId = payment.SalesOrderId,
                    RealizationType = SalesTargetRealizationTypes.CashCollected,
                    RealizedAmount = inrCash,
                    SourceCurrency = payment.Currency,
                    SourceAmount = payment.AdvanceAmount,
                    LockedFXRate = fx,
                    IsReversal = false,
                    TransactionDate = DateTimeOffset.UtcNow,
                    AppliedOn = DateTimeOffset.UtcNow,
                    AppliedBy = "SYSTEM_ADVANCE_CLEARANCE"
                };

                target.Realizations.Add(realization);
                target.RealizedCash += inrCash;
            }

            await _context.SaveChangesAsync(ct);
            await _cacheManager.InvalidateTenantAsync("default");
        }

        public async Task ProcessAdvanceClawbackAsync(int advancePaymentId, string reason, CancellationToken ct = default)
        {
            var realizations = await _context.SalesTargetRealizations
                .Include(r => r.Target)
                .Where(r => r.AdvancePaymentId == advancePaymentId && !r.IsReversal)
                .ToListAsync(ct);

            foreach (var orig in realizations)
            {
                var reversal = new SalesTargetRealization
                {
                    TenantId = orig.TenantId,
                    SalesTargetId = orig.SalesTargetId,
                    AdvancePaymentId = orig.AdvancePaymentId,
                    SalesOrderId = orig.SalesOrderId,
                    RealizationType = orig.RealizationType,
                    RealizedAmount = orig.RealizedAmount,
                    SourceCurrency = orig.SourceCurrency,
                    SourceAmount = orig.SourceAmount,
                    LockedFXRate = orig.LockedFXRate,
                    IsReversal = true,
                    ReversalOfRealizationId = orig.Id,
                    ReversalReason = reason,
                    TransactionDate = DateTimeOffset.UtcNow,
                    AppliedOn = DateTimeOffset.UtcNow,
                    AppliedBy = "SYSTEM_ADVANCE_REFUND"
                };

                _context.SalesTargetRealizations.Add(reversal);
                if (orig.Target != null)
                {
                    orig.Target.RealizedCash = Math.Max(0m, orig.Target.RealizedCash - orig.RealizedAmount);
                }
            }

            await _context.SaveChangesAsync(ct);
            await _cacheManager.InvalidateTenantAsync("default");
        }

        public async Task RecordInvoiceDispatchedAsync(int salesInvoiceId, CancellationToken ct = default)
        {
            // Billed realization on physical goods leaving the warehouse
            var realization = await _context.SalesTargetRealizations
                .FirstOrDefaultAsync(r => r.SalesInvoiceId == salesInvoiceId && !r.IsReversal, ct);

            if (realization != null) return; // Prevent duplicate

            // Look up corresponding sales target via active invoice mapping
            var target = await _context.SalesTargets
                .Where(t => !t.IsDeleted && t.Status == SalesTargetStatuses.Active && t.AssignmentType == SalesTargetAssignmentTypes.CompanyWide)
                .FirstOrDefaultAsync(ct);

            if (target != null)
            {
                _context.SalesTargetRealizations.Add(new SalesTargetRealization
                {
                    TenantId = target.TenantId,
                    SalesTargetId = target.Id,
                    SalesInvoiceId = salesInvoiceId,
                    RealizationType = SalesTargetRealizationTypes.BilledDispatched,
                    RealizedAmount = 0m,
                    TransactionDate = DateTimeOffset.UtcNow,
                    AppliedOn = DateTimeOffset.UtcNow,
                    AppliedBy = "SYSTEM_DISPATCH_INVOICE"
                });

                await _context.SaveChangesAsync(ct);
                await _cacheManager.InvalidateTenantAsync(target.TenantId);
            }
        }
    }
}

