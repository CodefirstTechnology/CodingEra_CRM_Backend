using System;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Dtos;
using ERP.Application.Sales.Pricing;
using ERP.Domain.Sales;

namespace ERP.Application.Sales.Quotations
{
    public interface IQuotationLineSnapshotService
    {
        Task ApplyResolvedSnapshotToLineItemAsync(
            Quotation quotation,
            QuotationItem lineItem,
            decimal quantity,
            CancellationToken cancellationToken = default);
    }

    public class QuotationLineSnapshotService : IQuotationLineSnapshotService
    {
        private readonly IPriceResolutionEngine _pricingEngine;

        public QuotationLineSnapshotService(IPriceResolutionEngine pricingEngine)
        {
            _pricingEngine = pricingEngine;
        }

        public async Task ApplyResolvedSnapshotToLineItemAsync(
            Quotation quotation,
            QuotationItem lineItem,
            decimal quantity,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(quotation);
            ArgumentNullException.ThrowIfNull(lineItem);

            var requestedQty = quantity <= 0 ? 1.0m : quantity;

            var resolution = await _pricingEngine.ResolveRateAsync(new PriceListResolveRequestDto
            {
                CustomerId = quotation.CustomerId,
                CustomerCategory = string.IsNullOrWhiteSpace(quotation.CustomerCategory) ? "Standard" : quotation.CustomerCategory,
                ItemCode = lineItem.ItemCode,
                Quantity = requestedQty,
                RequestedUom = lineItem.Unit,
                Currency = quotation.Currency,
                EffectiveDateUtc = quotation.QuotationDate.ToDateTime(TimeOnly.MinValue)
            }, cancellationToken);

            lineItem.ResolvedPriceListId = resolution.PriceListId;
            lineItem.PriceListNumberSnapshot = resolution.PriceListNumber;
            lineItem.ResolutionStageSnapshot = resolution.ResolutionStage;
            lineItem.OriginalSellingRate = resolution.EffectiveRatePerQuotedUom;
            lineItem.FloorPriceSnapshot = resolution.FloorPrice;
            lineItem.MaxDiscountPctSnapshot = resolution.MaximumDiscount;
            lineItem.TaxSlabPercentSnapshot = resolution.TaxPercentage;
            lineItem.SnapshotTimestampUtc = DateTimeOffset.UtcNow;

            lineItem.UnitPrice = resolution.EffectiveRatePerQuotedUom;
            lineItem.TaxPercent = resolution.TaxPercentage;

            var unitPrice = lineItem.UnitPrice;
            var discountPct = lineItem.DiscountPercent;
            var discountedUnit = unitPrice * (1.00m - (discountPct / 100.00m));

            lineItem.DiscountAmount = Math.Round((unitPrice - discountedUnit) * requestedQty, 2);
            lineItem.TaxAmount = Math.Round((discountedUnit * requestedQty) * (lineItem.TaxPercent / 100.00m), 2);
            lineItem.LineTotal = Math.Round((discountedUnit * requestedQty) + lineItem.TaxAmount, 2);

            lineItem.IsCommercialDeviation = lineItem.UnitPrice < lineItem.FloorPriceSnapshot 
                || lineItem.DiscountPercent > lineItem.MaxDiscountPctSnapshot;
        }
    }
}
