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

        public static CommercialComplianceResult Compliant() => new() { IsBlocked = false };
    }

    public interface ICommercialComplianceService
    {
        CommercialComplianceResult EvaluateQuotation(Quotation quotation);
    }
}
