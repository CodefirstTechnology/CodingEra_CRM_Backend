namespace ERP.Domain.Sales
{
    public class SalespersonPerformance
    {
        public int Id { get; set; }
        public int SalesPersonUserId { get; set; }
        public string SalesPersonName { get; set; } = string.Empty;
        public string? SalesTeam { get; set; }
        public string? Branch { get; set; }
        public string? RegionalManager { get; set; }
        public int FinancialYear { get; set; }
        public decimal TotalTargetValue { get; set; }
        public decimal TotalAchievedValue { get; set; }
        public decimal AttainmentPercentage { get; set; }
        public int ConfirmedOrderCount { get; set; }
        public int TotalQuotationCount { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal CalculatedCommission { get; set; }
        public string Status { get; set; } = "Active";
        public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedDate { get; set; } = DateTimeOffset.UtcNow;

        // Phase 1 Extended Fields
        public decimal WeightedScore { get; set; } = 0m;
        public decimal GrossMarginPercentage { get; set; } = 0m;
        public decimal AvgDealVelocityDays { get; set; } = 0m;
        public string ActiveBadgesJson { get; set; } = "[]";
        public string PeriodType { get; set; } = "FiscalYear";
    }
}
