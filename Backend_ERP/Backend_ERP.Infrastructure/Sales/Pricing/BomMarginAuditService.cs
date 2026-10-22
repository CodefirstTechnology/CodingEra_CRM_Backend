using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Dtos;
using ERP.Application.Sales.Pricing;
using ERP.Domain.Production;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales.Pricing
{
    public class BomMarginAuditService : IBomMarginAuditService
    {
        private readonly ERPDbContext _db;

        public BomMarginAuditService(ERPDbContext db)
        {
            _db = db;
        }

        public async Task<BomMarginAuditDto> AuditItemMarginAsync(
            string itemCode,
            decimal proposedSellingRate,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(itemCode))
            {
                throw new ArgumentException("Item code is required.", nameof(itemCode));
            }

            var trimmedCode = itemCode.Trim();

            // Query active engineering BOM and its material lines with raw material unit costs
            var activeBom = await _db.BillOfMaterials.AsNoTracking()
                .Include(b => b.Materials)
                    .ThenInclude(m => m.Material)
                .Where(b => b.ProductCode == trimmedCode && b.Status == BomStatus.Active && !b.IsDeleted)
                .OrderByDescending(b => b.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (activeBom == null)
            {
                // Fallback to FinishedGood inventory unit cost
                var fg = await _db.FinishedGoods.AsNoTracking()
                    .Where(f => f.ProductCode == trimmedCode && !f.IsDeleted)
                    .FirstOrDefaultAsync(cancellationToken);

                var inventoryCost = fg?.UnitCost ?? 0m;
                var fallbackMargin = proposedSellingRate > 0
                    ? Math.Round(((proposedSellingRate - inventoryCost) / proposedSellingRate) * 100m, 2)
                    : 0m;

                return new BomMarginAuditDto
                {
                    ItemCode = trimmedCode,
                    HasActiveBom = false,
                    BomNumber = "N/A",
                    StandardBomUnitCost = inventoryCost,
                    ProposedSellingRate = proposedSellingRate,
                    GrossMarginPercentage = fallbackMargin,
                    IsBelowHurdle = fallbackMargin < 20.00m,
                    IsNegativeMargin = fallbackMargin <= 0.00m,
                    HurdleWarning = fallbackMargin < 20.00m
                        ? $"Margin {fallbackMargin:N1}% is below the standard hurdle rate of 20% (Inventory Cost Fallback)."
                        : "Compliant"
                };
            }

            // Material rollup: Quantity * (1 + Wastage/100) * UnitCost
            decimal materialRollup = 0m;
            foreach (var line in activeBom.Materials)
            {
                var cost = line.Material?.UnitCost ?? 0m;
                var wastageMultiplier = 1.00m + (line.WastagePercent / 100.00m);
                materialRollup += line.Quantity * wastageMultiplier * cost;
            }

            // Standard operational overhead allocation burden: 12%
            var totalStandardCost = Math.Round(materialRollup * 1.12m, 4);
            var grossMarginPct = proposedSellingRate > 0
                ? Math.Round(((proposedSellingRate - totalStandardCost) / proposedSellingRate) * 100m, 2)
                : -100m;

            return new BomMarginAuditDto
            {
                ItemCode = trimmedCode,
                HasActiveBom = true,
                BomNumber = activeBom.BomNumber,
                StandardBomUnitCost = totalStandardCost,
                ProposedSellingRate = proposedSellingRate,
                GrossMarginPercentage = grossMarginPct,
                IsBelowHurdle = grossMarginPct < 20.00m,
                IsNegativeMargin = grossMarginPct <= 0.00m,
                HurdleWarning = grossMarginPct < 20.00m
                    ? $"Margin {grossMarginPct:N1}% is below the target hurdle rate of 20%."
                    : "Compliant"
            };
        }
    }
}
