using System;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    public class SalesOrderAdvanceGateService : ISalesOrderAdvanceGateService
    {
        private readonly ERPDbContext _db;

        public SalesOrderAdvanceGateService(ERPDbContext db)
        {
            _db = db;
        }

        public async Task<AdvanceGateCheckResultDto> CanReleaseToProductionAsync(
            int salesOrderId,
            CancellationToken ct = default)
        {
            var order = await _db.SalesOrders
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == salesOrderId, ct);

            if (order == null)
            {
                return AdvanceGateCheckResultDto.Locked($"Sales Order #{salesOrderId} not found.");
            }

            // Enterprise / VIP credit customers have production advance requirement waived
            if (string.Equals(order.CustomerCategory, "Enterprise", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(order.CustomerCategory, "VIP", StringComparison.OrdinalIgnoreCase))
            {
                return AdvanceGateCheckResultDto.Released();
            }

            // Tier 1 Gate: 30% token advance required before engineering / shop-floor release
            const decimal tokenPct = 0.30m;
            var requiredToken = Math.Round(order.GrandTotal * tokenPct, 2);

            if (order.AdvanceAllocatedAmount < requiredToken)
            {
                return AdvanceGateCheckResultDto.Locked(
                    $"Tier 1 Production Lock: Minimum token advance of ₹{requiredToken:N2} (30% of ₹{order.GrandTotal:N2}) is required prior to shop-floor release. Allocated advance: ₹{order.AdvanceAllocatedAmount:N2}.",
                    requiredToken,
                    order.AdvanceAllocatedAmount);
            }

            return AdvanceGateCheckResultDto.Released();
        }

        public async Task<AdvanceGateCheckResultDto> CanReleaseToDispatchAsync(
            int salesOrderId,
            CancellationToken ct = default)
        {
            var order = await _db.SalesOrders
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == salesOrderId, ct);

            if (order == null)
            {
                return AdvanceGateCheckResultDto.Locked($"Sales Order #{salesOrderId} not found.");
            }

            // Credit accounts (Enterprise / Net30) are cleared for dispatch based on credit limit
            if (string.Equals(order.CustomerCategory, "Enterprise", StringComparison.OrdinalIgnoreCase) ||
                order.PaymentTerms.Contains("Net", StringComparison.OrdinalIgnoreCase))
            {
                return AdvanceGateCheckResultDto.Released();
            }

            // Tier 2 Gate: 100% advance clearance required for standard commercial accounts
            if (order.AdvanceAllocatedAmount < order.GrandTotal)
            {
                var balance = order.GrandTotal - order.AdvanceAllocatedAmount;
                return AdvanceGateCheckResultDto.Locked(
                    $"Tier 2 Dispatch Lock: Balance payment of ₹{balance:N2} required prior to dispatch release and gate pass generation. Allocated: ₹{order.AdvanceAllocatedAmount:N2} of ₹{order.GrandTotal:N2}.",
                    order.GrandTotal,
                    order.AdvanceAllocatedAmount);
            }

            return AdvanceGateCheckResultDto.Released();
        }
    }
}
