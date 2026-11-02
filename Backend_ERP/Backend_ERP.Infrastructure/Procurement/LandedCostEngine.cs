using System;
using ERP.Application.Procurement;

namespace ERP.Infrastructure.Procurement
{
    public class LandedCostEngine : ILandedCostEngine
    {
        public LandedCostCalculationResult CalculateLandedCost(
            decimal baseUnitRate,
            decimal quantity,
            decimal discountPct,
            decimal packaging,
            decimal freight,
            decimal insurance,
            decimal gstRatePct,
            bool isItcEligible,
            decimal exchangeRate = 1.0m,
            decimal uomConversionMultiplier = 1.0m)
        {
            if (quantity <= 0) quantity = 1m;
            if (exchangeRate <= 0) exchangeRate = 1.0m;
            if (uomConversionMultiplier <= 0) uomConversionMultiplier = 1.0m;

            // Normalize base rate with 6-decimal precision for foreign currency & UOM conversion
            decimal effectiveRateInBase = Math.Round(baseUnitRate * exchangeRate * uomConversionMultiplier, 6, MidpointRounding.AwayFromZero);

            // Discount calculation
            decimal discountMultiplier = 1.0m - (discountPct / 100.0m);
            decimal discountedBaseTotal = Math.Round(quantity * effectiveRateInBase * discountMultiplier, 4, MidpointRounding.AwayFromZero);

            // Taxable base includes packaging, freight, insurance
            decimal taxableBase = Math.Round(discountedBaseTotal + packaging + freight + insurance, 2, MidpointRounding.AwayFromZero);

            // GST amount
            decimal gstAmount = Math.Round(taxableBase * (gstRatePct / 100.0m), 2, MidpointRounding.AwayFromZero);

            // If ITC eligible, GST is an input tax credit offset and NOT capitalized into landed inventory cost
            decimal effectiveTaxCost = isItcEligible ? 0.00m : gstAmount;

            decimal totalNetLandedCost = Math.Round(taxableBase + effectiveTaxCost, 2, MidpointRounding.AwayFromZero);
            decimal netLandedCostPerUnit = Math.Round(totalNetLandedCost / quantity, 4, MidpointRounding.AwayFromZero);

            return new LandedCostCalculationResult(
                TaxableBase: taxableBase,
                GstAmount: gstAmount,
                EffectiveTaxCost: effectiveTaxCost,
                TotalNetLandedCost: totalNetLandedCost,
                NetLandedCostPerUnit: netLandedCostPerUnit
            );
        }
    }
}

