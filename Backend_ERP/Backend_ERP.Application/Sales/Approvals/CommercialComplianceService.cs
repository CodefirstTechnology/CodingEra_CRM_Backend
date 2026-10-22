using ERP.Domain.Sales;

namespace ERP.Application.Sales.Approvals
{
    public class CommercialComplianceService : ICommercialComplianceService
    {
        public CommercialComplianceResult EvaluateQuotation(Quotation quotation)
        {
            if (quotation == null || quotation.Items == null)
            {
                return CommercialComplianceResult.Compliant();
            }

            foreach (var item in quotation.Items)
            {
                // 1. Absolute Floor Price Breach Check
                if (item.FloorPriceSnapshot > 0 && item.UnitPrice < item.FloorPriceSnapshot)
                {
                    var delta = (item.FloorPriceSnapshot - item.UnitPrice) * item.Quantity;
                    return new CommercialComplianceResult
                    {
                        IsBlocked = true,
                        Severity = CommercialDeviationSeverity.ExecutiveApproval,
                        DeviationReason = $"Item '{item.ItemCode}' quoted rate ({item.UnitPrice:N2}) breaches absolute floor price ({item.FloorPriceSnapshot:N2}).",
                        ViolationAmount = delta
                    };
                }

                // 2. Maximum Discount Ceiling Breach Check
                if (item.MaxDiscountPctSnapshot > 0 && item.DiscountPercent > item.MaxDiscountPctSnapshot)
                {
                    return new CommercialComplianceResult
                    {
                        IsBlocked = true,
                        Severity = CommercialDeviationSeverity.ManagerialApproval,
                        DeviationReason = $"Item '{item.ItemCode}' discount ({item.DiscountPercent:N1}%) exceeds allowed ceiling ({item.MaxDiscountPctSnapshot:N1}%).",
                        ViolationAmount = 0
                    };
                }
            }

            return CommercialComplianceResult.Compliant();
        }
    }
}
