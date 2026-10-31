using System;

namespace ERP.Domain.Sales
{
    public class SalesRepBadge
    {
        public int Id { get; set; }
        public string TenantId { get; set; } = "default";
        public int ProfileId { get; set; }
        public SalespersonPerformance? Profile { get; set; }

        public int SalesPersonUserId { get; set; }
        public string BadgeCode { get; set; } = string.Empty; // CENTURY_CLUB, MARGIN_GUARDIAN, FAST_CLOSER, CASH_CHAMPION, STREAK_MASTER, PACESETTER
        public string BadgeTitle { get; set; } = string.Empty;
        public string BadgeCategory { get; set; } = "Quota"; // Quota, Margin, Cash, Velocity
        public string Icon { get; set; } = "trophy";
        public string Color { get; set; } = "emerald";
        public string EvaluationCriteriaSnapshot { get; set; } = "{}";

        public DateTimeOffset AwardedDate { get; set; } = DateTimeOffset.UtcNow;
    }
}

