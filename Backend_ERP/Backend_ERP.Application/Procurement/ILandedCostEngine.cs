namespace ERP.Application.Procurement
{
    public record LandedCostCalculationResult(
        decimal TaxableBase,
        decimal GstAmount,
        decimal EffectiveTaxCost,
        decimal TotalNetLandedCost,
        decimal NetLandedCostPerUnit
    );

    public interface ILandedCostEngine
    {
        LandedCostCalculationResult CalculateLandedCost(
            decimal baseUnitRate,
            decimal quantity,
            decimal discountPct,
            decimal packaging,
            decimal freight,
            decimal insurance,
            decimal gstRatePct,
            bool isItcEligible,
            decimal exchangeRate = 1.0m,
            decimal uomConversionMultiplier = 1.0m);
    }
}

