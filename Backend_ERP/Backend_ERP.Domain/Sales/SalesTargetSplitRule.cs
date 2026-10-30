using System;

namespace ERP.Domain.Sales
{
    public class SalesTargetSplitRule
    {
        public int Id { get; set; }

        public string TenantId { get; set; } = "default";

        public string RuleName { get; set; } = string.Empty;

        // Attribution Percentages (Sum must equal 100.00%)
        public decimal LeadGeneratorPercentage { get; set; } = 25.00m;

        public decimal ClosingRepPercentage { get; set; } = 65.00m;

        public decimal TechnicalConsultantPercentage { get; set; } = 10.00m;

        public bool IsActive { get; set; } = true;

        public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;
    }
}

