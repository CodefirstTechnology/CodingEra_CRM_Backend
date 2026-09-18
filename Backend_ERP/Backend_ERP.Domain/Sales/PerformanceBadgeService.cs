using System.Text.Json.Serialization;

namespace ERP.Domain.Sales
{
    public record PerformanceBadge(
        [property: JsonPropertyName("code")] string Code,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("icon")] string Icon,
        [property: JsonPropertyName("color")] string Color
    );

    public static class PerformanceBadgeService
    {
        public static readonly PerformanceBadge CenturyClub = new("CENTURY_CLUB", "Century Club", "Attained >= 100% of target quota", "trophy", "emerald");
        public static readonly PerformanceBadge MarginProtector = new("MARGIN_PROTECTOR", "Margin Protector", "Maintained >= 25% gross margin", "shield-check", "blue");
        public static readonly PerformanceBadge FastCloser = new("FAST_CLOSER", "Fast Closer", "Average deal cycle under 7 days", "bolt", "amber");
        public static readonly PerformanceBadge Pacesetter = new("PACESETTER", "Pacesetter", "Rank #1 on the leaderboard", "crown", "gold");

        public static List<PerformanceBadge> EvaluateBadges(
            decimal attainmentPercentage,
            decimal grossMarginPercentage,
            decimal avgDealVelocityDays,
            int currentRank)
        {
            var badges = new List<PerformanceBadge>();

            if (attainmentPercentage >= 100m) badges.Add(CenturyClub);
            if (grossMarginPercentage >= 25m) badges.Add(MarginProtector);
            if (avgDealVelocityDays > 0m && avgDealVelocityDays <= 7m) badges.Add(FastCloser);
            if (currentRank == 1) badges.Add(Pacesetter);

            return badges;
        }
    }
}
