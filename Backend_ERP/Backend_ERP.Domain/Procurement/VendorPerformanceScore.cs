using System;

namespace ERP.Domain.Procurement
{
    public class VendorPerformanceScore
    {
        public int Id { get; set; }

        public string TenantId { get; set; } = "default";

        public int VendorId { get; set; }

        public Vendor? Vendor { get; set; }

        public string PeriodKey { get; set; } = string.Empty; // e.g. "2026-M10"

        public int FiscalYear { get; set; } = 2026;

        public int Month { get; set; } = 10;

        public decimal OtifPercentage { get; set; }

        public decimal QualityPassRate { get; set; }

        public decimal PriceVarianceScore { get; set; }

        public decimal ComplianceScore { get; set; }

        public decimal BlendedRatingScore { get; set; }

        public int TotalLotsReceived { get; set; }

        public int TotalLotsRejected { get; set; }

        public decimal DefectPartsPerMillion { get; set; }

        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    }
}

