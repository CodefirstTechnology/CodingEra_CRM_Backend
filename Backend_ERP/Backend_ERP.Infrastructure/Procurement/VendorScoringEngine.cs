using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Infrastructure.Procurement
{
    public class VendorScoringEngine : IVendorScoringEngine
    {
        private readonly ERPDbContext _db;
        private readonly ILogger<VendorScoringEngine> _logger;

        public VendorScoringEngine(
            ERPDbContext db,
            ILogger<VendorScoringEngine> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<VendorPerformanceScoreDto> IngestLotInspectionAndScoreAsync(
            int vendorId,
            VendorLotInspectionIngestionDto dto,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var vendor = await _db.Vendors
                .Include(v => v.ApprovalHistories)
                .Include(v => v.StatusHistory)
                .FirstOrDefaultAsync(v => v.Id == vendorId && !v.IsDeleted, cancellationToken);

            if (vendor is null)
            {
                throw new KeyNotFoundException($"Vendor with ID {vendorId} not found.");
            }

            var periodKey = $"{dto.FiscalYear}-{dto.Month:D2}";
            var now = DateTime.UtcNow;

            var existingScore = await _db.VendorPerformanceScores
                .FirstOrDefaultAsync(s => s.VendorId == vendorId && s.PeriodKey == periodKey, cancellationToken);

            // 1. Compute Blended Rating Score:
            // 35% OTIF + 35% Quality Pass Rate + 15% Price Variance + 15% Compliance
            var otif = Math.Clamp(dto.OtifPercentage, 0m, 100m);
            var quality = Math.Clamp(dto.QualityPassRate, 0m, 100m);
            var price = Math.Clamp(dto.PriceVarianceScore, 0m, 100m);
            var compliance = Math.Clamp(dto.ComplianceScore, 0m, 100m);

            var blendedRating = Math.Round((0.35m * otif) + (0.35m * quality) + (0.15m * price) + (0.15m * compliance), 2);

            int totalLotsReceived;
            int totalLotsRejected;

            if (existingScore != null)
            {
                totalLotsReceived = existingScore.TotalLotsReceived + dto.LotsReceived;
                totalLotsRejected = existingScore.TotalLotsRejected + dto.LotsRejected;

                existingScore.OtifPercentage = otif;
                existingScore.QualityPassRate = quality;
                existingScore.PriceVarianceScore = price;
                existingScore.ComplianceScore = compliance;
                existingScore.BlendedRatingScore = blendedRating;
                existingScore.TotalLotsReceived = totalLotsReceived;
                existingScore.TotalLotsRejected = totalLotsRejected;
                existingScore.DefectPartsPerMillion = totalLotsReceived > 0
                    ? Math.Round(((decimal)totalLotsRejected / totalLotsReceived) * 1_000_000m, 2)
                    : 0m;
                existingScore.CalculatedAt = now;
            }
            else
            {
                totalLotsReceived = dto.LotsReceived;
                totalLotsRejected = dto.LotsRejected;
                var ppm = totalLotsReceived > 0
                    ? Math.Round(((decimal)totalLotsRejected / totalLotsReceived) * 1_000_000m, 2)
                    : 0m;

                existingScore = new VendorPerformanceScore
                {
                    TenantId = vendor.TenantId,
                    VendorId = vendorId,
                    PeriodKey = periodKey,
                    FiscalYear = dto.FiscalYear,
                    Month = dto.Month,
                    OtifPercentage = otif,
                    QualityPassRate = quality,
                    PriceVarianceScore = price,
                    ComplianceScore = compliance,
                    BlendedRatingScore = blendedRating,
                    TotalLotsReceived = totalLotsReceived,
                    TotalLotsRejected = totalLotsRejected,
                    DefectPartsPerMillion = ppm,
                    CalculatedAt = now
                };

                _db.VendorPerformanceScores.Add(existingScore);
            }

            // 2. Automated Disqualification & Circuit Breaker:
            // Score < 60.00 or Critical Defect flagged
            if (blendedRating < 60.00m || dto.HasCriticalSafetyDefect)
            {
                var previousStatus = vendor.Status;
                var newStatus = dto.HasCriticalSafetyDefect ? VendorStatus.Disqualified : VendorStatus.OnHold;

                if (vendor.Status != newStatus)
                {
                    vendor.Status = newStatus;
                    vendor.UpdatedAt = now;
                    vendor.UpdatedBy = actingUser;

                    var reason = dto.HasCriticalSafetyDefect
                        ? $"CRITICAL DEFECT: {dto.CriticalDefectDescription ?? "Safety defect flagged in incoming inspection."}"
                        : $"RATING CIRCUIT-BREAKER: Blended score {blendedRating:F2}/100.00 fell below safety SLA threshold (60.00).";

                    vendor.Notes = string.IsNullOrWhiteSpace(vendor.Notes)
                        ? $"[{now:u}] {reason}"
                        : $"{vendor.Notes}\n[{now:u}] {reason}";

                    vendor.ApprovalHistories.Add(new VendorApprovalHistory
                    {
                        TenantId = vendor.TenantId,
                        VendorId = vendor.Id,
                        PreviousStatus = previousStatus,
                        NewStatus = newStatus,
                        Action = "CircuitBreakerDisqualification",
                        Comments = reason,
                        ActionedByUserId = actingUser,
                        ActionedByUserName = actingUser,
                        ActionedAt = now
                    });

                    vendor.StatusHistory.Add(new VendorStatusHistory
                    {
                        VendorId = vendor.Id,
                        Status = newStatus,
                        PreviousStatus = previousStatus,
                        User = actingUser,
                        Remarks = reason,
                        Date = now
                    });

                    _logger.LogWarning("Vendor {VendorCode} ({VendorName}) disqualified/put on hold by circuit breaker. Reason: {Reason}",
                        vendor.VendorCode, vendor.Name, reason);
                }
            }

            // Save changes (in PostgreSQL, trigger fn_trg_sync_vendor_rating_score also runs)
            await _db.SaveChangesAsync(cancellationToken);

            // Also keep in-memory entity synchronized
            vendor.RatingScore = blendedRating;

            return VendorMapper.ToPerformanceScoreDto(existingScore);
        }

        public async Task<VendorPerformanceSummaryDto?> GetPerformanceSummaryAsync(
            int vendorId,
            CancellationToken cancellationToken = default)
        {
            var vendor = await _db.Vendors
                .Include(v => v.PerformanceScores.OrderByDescending(p => p.PeriodKey))
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == vendorId && !v.IsDeleted, cancellationToken);

            if (vendor is null) return null;

            var latestScore = vendor.PerformanceScores.FirstOrDefault();

            return new VendorPerformanceSummaryDto
            {
                VendorId = vendor.Id,
                VendorCode = vendor.VendorCode,
                VendorName = vendor.Name,
                TotalPurchaseOrders = 0,
                TotalPurchaseValue = vendor.TotalOpenCommitment,
                OnTimeDeliveryPercentage = (double)(latestScore?.OtifPercentage ?? 100.00m),
                RejectionRatioPercentage = (double)(latestScore != null && latestScore.TotalLotsReceived > 0
                    ? (latestScore.TotalLotsRejected / (decimal)latestScore.TotalLotsReceived) * 100m
                    : 0m),
                LeadTimeCompliancePercentage = (double)(latestScore?.OtifPercentage ?? 100.00m),
                PricingConsistencyScore = (double)(latestScore?.PriceVarianceScore ?? 100.00m),
                BlendedRatingScore = latestScore?.BlendedRatingScore ?? vendor.RatingScore,
                Note = "Real-time P2P Telemetry Ingested from GRN and Incoming Inspection"
            };
        }

        public async Task<IReadOnlyList<VendorPerformanceScoreDto>> GetPerformanceHistoryAsync(
            int vendorId,
            int? limitMonths = 12,
            CancellationToken cancellationToken = default)
        {
            var q = _db.VendorPerformanceScores
                .AsNoTracking()
                .Where(s => s.VendorId == vendorId)
                .OrderByDescending(s => s.PeriodKey);

            var items = limitMonths.HasValue && limitMonths.Value > 0
                ? await q.Take(limitMonths.Value).ToListAsync(cancellationToken)
                : await q.ToListAsync(cancellationToken);

            return items.Select(VendorMapper.ToPerformanceScoreDto).ToList();
        }
    }
}

