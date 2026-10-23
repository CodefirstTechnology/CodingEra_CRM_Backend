using System;
using System.Collections.Generic;
using ERP.Domain.Sales;

namespace ERP.Application.Sales.Approvals
{
    public enum CommercialDeviationSeverity
    {
        Compliant = 0,
        ManagerialApproval = 1,
        ExecutiveApproval = 2
    }

    public class CommercialComplianceResult
    {
        public bool IsBlocked { get; set; }
        public CommercialDeviationSeverity Severity { get; set; } = CommercialDeviationSeverity.Compliant;
        public string DeviationReason { get; set; } = string.Empty;
        public decimal ViolationAmount { get; set; }
        public int BreachedItemsCount { get; set; }
        public decimal MaxDiscountBreachPercentage { get; set; }
        public decimal MaxFloorPriceBreachAmount { get; set; }
        public string RequiredApprovalLevel { get; set; } = "None";
        public List<string> BreachReasons { get; set; } = new();

        public static CommercialComplianceResult Compliant() => new() { IsBlocked = false };
    }

    public interface ICommercialComplianceService
    {
        CommercialComplianceResult EvaluateQuotation(Quotation quotation);
        string GenerateApprovalToken(Quotation quotation);
        bool VerifyApprovalToken(Quotation quotation, string token);
    }
}
