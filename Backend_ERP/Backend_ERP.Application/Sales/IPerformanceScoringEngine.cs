using System.Collections.Generic;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;

namespace ERP.Application.Sales
{
    public interface IPerformanceScoringEngine
    {
        PerformanceScoreEvaluationResult EvaluateRepScore(
            decimal attainmentPercentage,
            decimal cashCollectionPercentage,
            decimal grossMarginPercentage,
            decimal avgDealVelocityDays,
            decimal conversionRate,
            decimal pacingIndex);

        RadarSkillMatrixDto CalculateRadarSkills(
            decimal attainmentPercentage,
            decimal cashCollectionPercentage,
            decimal grossMarginPercentage,
            decimal avgDealVelocityDays,
            decimal conversionRate);

        List<SalesRepBadge> EvaluateBadges(
            int salesPersonUserId,
            int profileId,
            int financialYear,
            decimal attainmentPercentage,
            decimal cashCollectionPercentage,
            decimal grossMarginPercentage,
            decimal avgDealVelocityDays,
            int currentRank,
            int winningStreakMonths,
            decimal pacingIndex);

        decimal CalculatePacingIndex(
            decimal attainmentPercentage,
            int financialYear);
    }

    public record PerformanceScoreEvaluationResult(
        decimal WeightedScore,
        string TierBadge,
        decimal DiscountDisciplineScore
    );
}

