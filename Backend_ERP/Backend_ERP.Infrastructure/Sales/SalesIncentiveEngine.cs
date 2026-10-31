using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
    public class SalesIncentiveEngine : ISalesIncentiveEngine
    {
        private readonly ERPDbContext _context;
        private readonly ILogger<SalesIncentiveEngine> _logger;

        public SalesIncentiveEngine(
            ERPDbContext context,
            ILogger<SalesIncentiveEngine>? logger = null)
        {
            _context = context;
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SalesIncentiveEngine>.Instance;
        }

        public async Task<SalesIncentiveScheme?> GetActiveSchemeAsync(int financialYear, CancellationToken ct = default)
        {
            return await _context.SalesIncentiveSchemes
                .Where(s => s.FiscalYear == financialYear && s.Status == "Active")
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<SalesCommissionCalculationResult> CalculateCommissionForOrderAsync(
            int salesPersonUserId,
            int financialYear,
            decimal orderAmount,
            decimal grossMarginPercentage,
            string orderNumber,
            int orderId,
            decimal advancePaymentClearedPercentage,
            decimal splitPercentage = 100.0m,
            CancellationToken ct = default)
        {
            var scheme = await GetActiveSchemeAsync(financialYear, ct);
            decimal baseRate = scheme?.BaseCommissionRate ?? 3.00m;
            decimal marginHurdle = scheme?.MarginHurdlePercentage ?? 22.00m;
            decimal minAttainment = scheme?.MinimumAttainmentThreshold ?? 80.00m;
            bool requiresCash = scheme?.RequiresCashClearance ?? true;
            decimal minCashClearance = scheme?.MinCashClearancePercentage ?? 50.00m;

            // Fetch current rep performance attainment
            var perf = await _context.SalespersonPerformances
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.SalesPersonUserId == salesPersonUserId && p.FinancialYear == financialYear, ct);

            decimal currentAttainment = perf?.AttainmentPercentage ?? 0m;

            // 1. Determine base multiplier from accelerator tiers
            decimal multiplier = 1.0m;
            List<IncentiveAcceleratorTier> tiers = new();
            if (scheme != null && !string.IsNullOrWhiteSpace(scheme.AcceleratorTiersJson))
            {
                try
                {
                    tiers = JsonSerializer.Deserialize<List<IncentiveAcceleratorTier>>(scheme.AcceleratorTiersJson) ?? new();
                }
                catch
                {
                    tiers = new();
                }
            }

            if (!tiers.Any())
            {
                // Default Tier Curve
                tiers = new List<IncentiveAcceleratorTier>
                {
                    new() { MinAttainment = 0m, MaxAttainment = 100.00m, Multiplier = 1.0m, TierName = "Standard" },
                    new() { MinAttainment = 100.01m, MaxAttainment = 125.00m, Multiplier = 1.5m, TierName = "Accelerator Tier 1" },
                    new() { MinAttainment = 125.01m, MaxAttainment = 9999.00m, Multiplier = 2.0m, TierName = "Super Achiever Tier 2" }
                };
            }

            var matchingTier = tiers.FirstOrDefault(t => currentAttainment >= t.MinAttainment && currentAttainment <= t.MaxAttainment)
                ?? tiers.LastOrDefault(t => currentAttainment >= t.MinAttainment)
                ?? tiers.First();

            multiplier = matchingTier.Multiplier;

            // 2. DUAL-HURDLE MARGIN CAP ENFORCEMENT:
            // Multipliers > 1.0x are unlocked ONLY IF GrossMarginPercentage >= marginHurdle (default 22.0%)
            bool marginHurdlePassed = grossMarginPercentage >= marginHurdle;
            string notes = "";

            if (multiplier > 1.0m && !marginHurdlePassed)
            {
                _logger.LogInformation(
                    "Dual-Hurdle Cap Enforced: Rep {UserId} attained {Attainment}% (Tier {Multiplier}x) but order gross margin {GM}% is below hurdle {Hurdle}%. Capping at 1.0x.",
                    salesPersonUserId, currentAttainment, multiplier, grossMarginPercentage, marginHurdle);

                multiplier = 1.0m;
                notes = $"Accelerator multiplier capped at 1.0x due to Gross Margin {grossMarginPercentage:F1}% falling below {marginHurdle:F1}% hurdle.";
            }

            // 3. Shared Deal Attribution Proration
            decimal splitFactor = Math.Clamp(splitPercentage, 0m, 100m) / 100m;
            decimal attributedOrderAmount = orderAmount * splitFactor;

            // 4. Calculate Net Accrued Commission
            decimal rawCommission = attributedOrderAmount * (baseRate / 100m) * multiplier;
            decimal finalCommission = Math.Round(rawCommission, 2, MidpointRounding.AwayFromZero);

            // 5. Cash Gating State Machine
            bool cashGatePassed = !requiresCash || advancePaymentClearedPercentage >= minCashClearance;
            string status = cashGatePassed ? "Accrued" : "GatedPendingCash";

            if (!cashGatePassed)
            {
                notes = string.IsNullOrWhiteSpace(notes)
                    ? $"Commission gated pending cleared advance payment ({advancePaymentClearedPercentage:F1}% cleared < {minCashClearance:F1}% required)."
                    : $"{notes} | Commission gated pending cleared advance payment.";
            }

            return new SalesCommissionCalculationResult(
                BaseRateApplied: baseRate,
                AcceleratorMultiplier: multiplier,
                CalculatedCommission: finalCommission,
                MarginHurdlePassed: marginHurdlePassed,
                CashGatePassed: cashGatePassed,
                Status: status,
                Notes: notes
            );
        }

        public async Task<SalesCommissionLedger> AccrueOrderCommissionAsync(
            int salesPersonUserId,
            int financialYear,
            int orderId,
            string orderNumber,
            decimal orderAmount,
            decimal grossMarginPercentage,
            decimal advancePaymentClearedPercentage,
            decimal splitPercentage = 100.0m,
            CancellationToken ct = default)
        {
            // 1. Check idempotency
            var existing = await _context.SalesCommissionLedgers
                .FirstOrDefaultAsync(l => l.SalesOrderId == orderId && l.SalesPersonUserId == salesPersonUserId && !l.IsClawback, ct);

            if (existing != null)
            {
                _logger.LogInformation("Commission ledger entry already exists for Order {OrderId} Rep {UserId}", orderId, salesPersonUserId);
                return existing;
            }

            // 2. Fetch or create rep performance record
            var perf = await _context.SalespersonPerformances
                .FirstOrDefaultAsync(p => p.SalesPersonUserId == salesPersonUserId && p.FinancialYear == financialYear, ct);

            if (perf == null)
            {
                perf = new SalespersonPerformance
                {
                    SalesPersonUserId = salesPersonUserId,
                    SalesPersonName = $"Salesperson {salesPersonUserId}",
                    FinancialYear = financialYear,
                    Status = "Active",
                    CreatedDate = DateTimeOffset.UtcNow,
                    UpdatedDate = DateTimeOffset.UtcNow
                };
                _context.SalespersonPerformances.Add(perf);
                await _context.SaveChangesAsync(ct);
            }

            // 3. Compute commission
            var calc = await CalculateCommissionForOrderAsync(
                salesPersonUserId,
                financialYear,
                orderAmount,
                grossMarginPercentage,
                orderNumber,
                orderId,
                advancePaymentClearedPercentage,
                splitPercentage,
                ct);

            var scheme = await GetActiveSchemeAsync(financialYear, ct);

            // Generate HMAC approval token placeholder
            string rawTokenData = $"{perf.TenantId}:{perf.Id}:{orderId}:{calc.CalculatedCommission}:{DateTimeOffset.UtcNow.Ticks}";
            string approvalToken = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawTokenData))).ToLowerInvariant();

            // 4. Insert Ledger Entry
            var ledger = new SalesCommissionLedger
            {
                TenantId = perf.TenantId,
                ProfileId = perf.Id,
                SalesPersonUserId = salesPersonUserId,
                IncentiveSchemeId = scheme?.Id,
                SalesOrderId = orderId,
                TransactionType = "BookingAccrual",
                TransactionAmount = orderAmount * (splitPercentage / 100m),
                CommissionRate = calc.BaseRateApplied,
                AcceleratorMultiplier = calc.AcceleratorMultiplier,
                AccruedCommission = calc.CalculatedCommission,
                IsClawback = false,
                PayoutStatus = calc.Status,
                ApprovalToken = approvalToken,
                PaymentVoucherReference = orderNumber,
                AccruedDate = DateTimeOffset.UtcNow
            };

            _context.SalesCommissionLedgers.Add(ledger);
            await _context.SaveChangesAsync(ct); // Trigger fn_trg_sync_rep_commission_totals will update profile totals!

            return ledger;
        }

        public async Task<SalesCommissionLedger?> RecordClawbackAsync(
            int salesPersonUserId,
            int financialYear,
            int orderId,
            string orderNumber,
            decimal originalCommissionAmount,
            string reason,
            CancellationToken ct = default)
        {
            var perf = await _context.SalespersonPerformances
                .FirstOrDefaultAsync(p => p.SalesPersonUserId == salesPersonUserId && p.FinancialYear == financialYear, ct);

            if (perf == null) return null;

            var origLedger = await _context.SalesCommissionLedgers
                .FirstOrDefaultAsync(l => l.SalesOrderId == orderId && l.SalesPersonUserId == salesPersonUserId && !l.IsClawback, ct);

            var clawback = new SalesCommissionLedger
            {
                TenantId = perf.TenantId,
                ProfileId = perf.Id,
                SalesPersonUserId = salesPersonUserId,
                SalesOrderId = orderId,
                IncentiveSchemeId = origLedger?.IncentiveSchemeId,
                ReversalOfLedgerId = origLedger?.Id,
                TransactionType = "Clawback",
                TransactionAmount = 0m,
                CommissionRate = 0m,
                AcceleratorMultiplier = 1.0m,
                AccruedCommission = Math.Abs(originalCommissionAmount),
                IsClawback = true,
                ClawbackReason = reason,
                PayoutStatus = "ClawedBack",
                PaymentVoucherReference = $"REV-{orderNumber}",
                AccruedDate = DateTimeOffset.UtcNow
            };

            _context.SalesCommissionLedgers.Add(clawback);
            await _context.SaveChangesAsync(ct); // Trigger fn_trg_sync_rep_commission_totals will sync TotalClawbackCommission

            return clawback;
        }

        public async Task<int> ReleaseCashGatedCommissionsAsync(
            int orderId,
            decimal totalClearedAdvancePercentage,
            CancellationToken ct = default)
        {
            var gatedEntries = await _context.SalesCommissionLedgers
                .Where(l => l.SalesOrderId == orderId && l.PayoutStatus == "GatedPendingCash")
                .ToListAsync(ct);

            if (!gatedEntries.Any()) return 0;

            int releasedCount = 0;
            foreach (var entry in gatedEntries)
            {
                var scheme = await GetActiveSchemeAsync(2026, ct);
                decimal minCash = scheme?.MinCashClearancePercentage ?? 50.0m;

                if (totalClearedAdvancePercentage >= minCash)
                {
                    entry.PayoutStatus = "ApprovedForPayment";
                    releasedCount++;
                }
            }

            if (releasedCount > 0)
            {
                await _context.SaveChangesAsync(ct);
            }

            return releasedCount;
        }
    }
}
