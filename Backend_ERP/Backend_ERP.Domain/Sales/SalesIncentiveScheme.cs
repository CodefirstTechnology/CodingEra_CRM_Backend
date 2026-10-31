using System;

namespace ERP.Domain.Sales
{
    public class SalesIncentiveScheme
    {
        public int Id { get; set; }
        public string TenantId { get; set; } = "default";
        public string SchemeCode { get; set; } = string.Empty; // e.g. "INC-FY26-STANDARD"
        public string SchemeName { get; set; } = string.Empty;
        public int FiscalYear { get; set; } = 2026;
        public string PeriodType { get; set; } = "Monthly"; // Monthly, Quarterly, Annual

        public decimal BaseCommissionRate { get; set; } = 3.00m; // 3.0%
        public decimal MarginHurdlePercentage { get; set; } = 22.00m; // Minimum 22% GM to unlock Tier 2/3
        public decimal MinimumAttainmentThreshold { get; set; } = 80.00m; // Min 80% quota attainment to qualify

        // JSONB Array: [{ "MinAttainment": 100.1, "MaxAttainment": 120.0, "Multiplier": 1.5 }, { "MinAttainment": 120.1, "MaxAttainment": 999.0, "Multiplier": 2.0 }]
        public string AcceleratorTiersJson { get; set; } = "[]";

        // Cash flow gating
        public bool RequiresCashClearance { get; set; } = true;
        public decimal MinCashClearancePercentage { get; set; } = 50.00m; // Min 50% cleared advance to authorize payout

        public string Status { get; set; } = "Active";
        public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;
        public uint Version { get; set; }
    }

    public class IncentiveAcceleratorTier
    {
        public decimal MinAttainment { get; set; }
        public decimal MaxAttainment { get; set; }
        public decimal Multiplier { get; set; }
        public string TierName { get; set; } = string.Empty;
    }
}

