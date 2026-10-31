using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Infrastructure.Sales
{
    public class PerformanceReportService : IPerformanceReportService
    {
        private readonly ERPDbContext _context;
        private readonly ILogger<PerformanceReportService> _logger;

        public PerformanceReportService(
            ERPDbContext context,
            ILogger<PerformanceReportService>? logger = null)
        {
            _context = context;
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PerformanceReportService>.Instance;
        }

        public async Task<ExecutiveCommissionPayoutReportDto> GetExecutiveCommissionPayoutStatementAsync(
            int financialYear,
            string? branch,
            string? salesTeam,
            CancellationToken ct = default)
        {
            var query = _context.SalespersonPerformances
                .Where(p => p.FinancialYear == financialYear && p.Status == "Active");

            if (!string.IsNullOrWhiteSpace(branch))
                query = query.Where(p => p.Branch == branch);

            if (!string.IsNullOrWhiteSpace(salesTeam))
                query = query.Where(p => p.SalesTeam == salesTeam);

            var perfs = await query.ToListAsync(ct);

            // Fetch pending cash gated sums grouped by user
            var gatedCommissions = await _context.SalesCommissionLedgers
                .Where(c => c.PayoutStatus == "GatedPendingCash")
                .GroupBy(c => c.SalesPersonUserId)
                .Select(g => new { UserId = g.Key, GatedSum = g.Sum(x => x.AccruedCommission) })
                .ToDictionaryAsync(g => g.UserId, g => g.GatedSum, ct);

            var rows = new List<CommissionPayoutRowDto>();
            foreach (var p in perfs)
            {
                gatedCommissions.TryGetValue(p.SalesPersonUserId, out decimal gatedAmount);

                rows.Add(new CommissionPayoutRowDto
                {
                    SalesPersonUserId = p.SalesPersonUserId,
                    SalesPersonName = p.SalesPersonName,
                    SalesTeam = p.SalesTeam,
                    Branch = p.Branch,
                    TargetValue = p.TotalTargetValue,
                    AchievedRevenue = p.TotalAchievedValue,
                    AttainmentPercentage = p.AttainmentPercentage,
                    GrossMarginPercentage = p.GrossMarginPercentage,
                    TotalAccrued = p.TotalAccruedCommission,
                    TotalClawback = p.TotalClawbackCommission,
                    TotalPaid = p.TotalPaidCommission,
                    NetPayable = p.NetPayableCommission,
                    GatedPendingCash = gatedAmount,
                    Status = p.Status
                });
            }

            return new ExecutiveCommissionPayoutReportDto
            {
                FinancialYear = financialYear,
                TotalCompanyAccruedCommission = rows.Sum(r => r.TotalAccrued),
                TotalCompanyClawbackCommission = rows.Sum(r => r.TotalClawback),
                TotalCompanyPaidCommission = rows.Sum(r => r.TotalPaid),
                TotalCompanyNetPayableCommission = rows.Sum(r => r.NetPayable),
                TotalCashGatedPendingCommission = rows.Sum(r => r.GatedPendingCash),
                Rows = rows
            };
        }

        public async Task<RepCohortVelocityMatrixReportDto> GetRepCohortVelocityMatrixAsync(
            int financialYear,
            string? branch,
            string? salesTeam,
            CancellationToken ct = default)
        {
            var query = _context.SalespersonPerformances
                .Where(p => p.FinancialYear == financialYear && p.Status == "Active");

            if (!string.IsNullOrWhiteSpace(branch))
                query = query.Where(p => p.Branch == branch);

            if (!string.IsNullOrWhiteSpace(salesTeam))
                query = query.Where(p => p.SalesTeam == salesTeam);

            var perfs = await query.ToListAsync(ct);
            var rows = new List<RepCohortVelocityRowDto>();

            foreach (var p in perfs)
            {
                string cohort = p.AvgDealVelocityDays switch
                {
                    <= 10.0m and > 0m => "Fast",
                    <= 30.0m and > 10.0m => "Medium",
                    _ => "Slow"
                };

                decimal avgDealSize = p.ConfirmedOrderCount > 0
                    ? Math.Round(p.TotalBookedRevenue / p.ConfirmedOrderCount, 2, MidpointRounding.AwayFromZero)
                    : 0m;

                rows.Add(new RepCohortVelocityRowDto
                {
                    SalesPersonUserId = p.SalesPersonUserId,
                    SalesPersonName = p.SalesPersonName,
                    SalesTeam = p.SalesTeam,
                    TotalQuotations = p.TotalQuotationCount,
                    TotalOrdersWon = p.ConfirmedOrderCount,
                    ConversionRatePercentage = p.ConversionRate,
                    AvgDealVelocityDays = p.AvgDealVelocityDays,
                    BookedRevenue = p.TotalBookedRevenue,
                    AverageDealSize = avgDealSize,
                    VelocityCohort = cohort
                });
            }

            return new RepCohortVelocityMatrixReportDto
            {
                FinancialYear = financialYear,
                Rows = rows
            };
        }

        public async Task<DiscountMarginErosionReportDto> GetDiscountMarginErosionReportAsync(
            int financialYear,
            string? branch,
            string? salesTeam,
            CancellationToken ct = default)
        {
            var query = _context.SalespersonPerformances
                .Where(p => p.FinancialYear == financialYear && p.Status == "Active");

            if (!string.IsNullOrWhiteSpace(branch))
                query = query.Where(p => p.Branch == branch);

            if (!string.IsNullOrWhiteSpace(salesTeam))
                query = query.Where(p => p.SalesTeam == salesTeam);

            var perfs = await query.ToListAsync(ct);
            var rows = new List<DiscountMarginErosionRowDto>();

            const decimal targetGrossMargin = 25.0m;
            const decimal hurdleMargin = 22.0m;

            foreach (var p in perfs)
            {
                decimal marginSpread = Math.Round(p.GrossMarginPercentage - hurdleMargin, 2);
                decimal avgDiscountOffered = Math.Max(0m, Math.Round(100m - p.DiscountDisciplineScore, 1));
                decimal erosionRate = Math.Max(0m, targetGrossMargin - p.GrossMarginPercentage);
                decimal estimatedErosion = Math.Round(p.TotalBookedRevenue * (erosionRate / 100m), 2);

                rows.Add(new DiscountMarginErosionRowDto
                {
                    SalesPersonUserId = p.SalesPersonUserId,
                    SalesPersonName = p.SalesPersonName,
                    SalesTeam = p.SalesTeam,
                    TotalBookedRevenue = p.TotalBookedRevenue,
                    AvgDiscountOfferedPercentage = avgDiscountOffered,
                    DiscountDisciplineScore = p.DiscountDisciplineScore,
                    RealizedGrossMarginPercentage = p.GrossMarginPercentage,
                    MarginHurdleSpread = marginSpread,
                    EstimatedMarginErosionAmount = estimatedErosion,
                    AcceleratorHurdlePassed = p.GrossMarginPercentage >= hurdleMargin
                });
            }

            return new DiscountMarginErosionReportDto
            {
                FinancialYear = financialYear,
                Rows = rows
            };
        }

        public async Task<PipEarlyWarningAuditReportDto> GetPipEarlyWarningAuditAsync(
            int financialYear,
            string? branch,
            string? salesTeam,
            CancellationToken ct = default)
        {
            var query = _context.SalespersonPerformances
                .Where(p => p.FinancialYear == financialYear && p.Status == "Active");

            if (!string.IsNullOrWhiteSpace(branch))
                query = query.Where(p => p.Branch == branch);

            if (!string.IsNullOrWhiteSpace(salesTeam))
                query = query.Where(p => p.SalesTeam == salesTeam);

            var perfs = await query.ToListAsync(ct);
            var rows = new List<PipEarlyWarningRowDto>();

            // Calculate expected elapsed attainment
            var fyStart = new DateTime(financialYear - 1, 4, 1, 0, 0, 0, DateTimeKind.Utc);
            var fyEnd = new DateTime(financialYear, 3, 31, 23, 59, 59, DateTimeKind.Utc);
            var now = DateTime.UtcNow;
            decimal expectedAttainment = 100m;
            if (now >= fyStart && now <= fyEnd)
            {
                double total = (fyEnd - fyStart).TotalDays;
                double elapsed = (now - fyStart).TotalDays;
                expectedAttainment = Math.Round((decimal)(elapsed / total) * 100m, 1);
            }

            foreach (var p in perfs)
            {
                decimal expectedAchieved = Math.Round(p.TotalTargetValue * (expectedAttainment / 100m), 2);
                decimal shortfall = Math.Max(0m, expectedAchieved - p.TotalAchievedValue);

                string riskLevel;
                string remediation;

                if (p.PacingIndex < 0.60m)
                {
                    riskLevel = "Critical";
                    remediation = "Initiate formal 30-day PIP; reassign cold accounts; schedule mandatory weekly pipeline reviews.";
                }
                else if (p.PacingIndex < 0.85m)
                {
                    riskLevel = "Warning";
                    remediation = "Conduct discount and pricing alignment training; increase outbound lead qualification.";
                }
                else
                {
                    riskLevel = "Low";
                    remediation = "On track; maintain deal velocity and focus on high-margin product mix.";
                }

                rows.Add(new PipEarlyWarningRowDto
                {
                    SalesPersonUserId = p.SalesPersonUserId,
                    SalesPersonName = p.SalesPersonName,
                    SalesTeam = p.SalesTeam,
                    Branch = p.Branch,
                    TargetValue = p.TotalTargetValue,
                    AchievedValue = p.TotalAchievedValue,
                    AttainmentPercentage = p.AttainmentPercentage,
                    ExpectedAttainmentPercentage = expectedAttainment,
                    PacingIndex = p.PacingIndex,
                    ShortfallAmount = shortfall,
                    RiskLevel = riskLevel,
                    RecommendedRemediation = remediation
                });
            }

            return new PipEarlyWarningAuditReportDto
            {
                FinancialYear = financialYear,
                AtRiskReps = rows.OrderBy(r => r.PacingIndex).ToList()
            };
        }

        public async Task<byte[]> ExportReportToCsvAsync(
            string reportType,
            int financialYear,
            string? branch,
            string? salesTeam,
            CancellationToken ct = default)
        {
            var sb = new StringBuilder();

            switch (reportType.ToLowerInvariant())
            {
                case "commission-payout":
                case "commission":
                    var commReport = await GetExecutiveCommissionPayoutStatementAsync(financialYear, branch, salesTeam, ct);
                    sb.AppendLine("SalesPersonUserId,SalesPersonName,SalesTeam,Branch,TargetValue,AchievedRevenue,AttainmentPercentage,GrossMarginPercentage,TotalAccrued,TotalClawback,TotalPaid,NetPayable,GatedPendingCash,Status");
                    foreach (var r in commReport.Rows)
                    {
                        sb.AppendLine($"{r.SalesPersonUserId},\"{EscapeCsv(r.SalesPersonName)}\",\"{EscapeCsv(r.SalesTeam)}\",\"{EscapeCsv(r.Branch)}\",{r.TargetValue},{r.AchievedRevenue},{r.AttainmentPercentage},{r.GrossMarginPercentage},{r.TotalAccrued},{r.TotalClawback},{r.TotalPaid},{r.NetPayable},{r.GatedPendingCash},{r.Status}");
                    }
                    break;

                case "velocity-matrix":
                case "velocity":
                    var velReport = await GetRepCohortVelocityMatrixAsync(financialYear, branch, salesTeam, ct);
                    sb.AppendLine("SalesPersonUserId,SalesPersonName,SalesTeam,TotalQuotations,TotalOrdersWon,ConversionRatePercentage,AvgDealVelocityDays,BookedRevenue,AverageDealSize,VelocityCohort");
                    foreach (var r in velReport.Rows)
                    {
                        sb.AppendLine($"{r.SalesPersonUserId},\"{EscapeCsv(r.SalesPersonName)}\",\"{EscapeCsv(r.SalesTeam)}\",{r.TotalQuotations},{r.TotalOrdersWon},{r.ConversionRatePercentage},{r.AvgDealVelocityDays},{r.BookedRevenue},{r.AverageDealSize},{r.VelocityCohort}");
                    }
                    break;

                case "margin-erosion":
                    var marginReport = await GetDiscountMarginErosionReportAsync(financialYear, branch, salesTeam, ct);
                    sb.AppendLine("SalesPersonUserId,SalesPersonName,SalesTeam,TotalBookedRevenue,AvgDiscountOfferedPercentage,DiscountDisciplineScore,RealizedGrossMarginPercentage,MarginHurdleSpread,EstimatedMarginErosionAmount,AcceleratorHurdlePassed");
                    foreach (var r in marginReport.Rows)
                    {
                        sb.AppendLine($"{r.SalesPersonUserId},\"{EscapeCsv(r.SalesPersonName)}\",\"{EscapeCsv(r.SalesTeam)}\",{r.TotalBookedRevenue},{r.AvgDiscountOfferedPercentage},{r.DiscountDisciplineScore},{r.RealizedGrossMarginPercentage},{r.MarginHurdleSpread},{r.EstimatedMarginErosionAmount},{r.AcceleratorHurdlePassed}");
                    }
                    break;

                case "pip-audit":
                case "pip":
                default:
                    var pipReport = await GetPipEarlyWarningAuditAsync(financialYear, branch, salesTeam, ct);
                    sb.AppendLine("SalesPersonUserId,SalesPersonName,SalesTeam,Branch,TargetValue,AchievedValue,AttainmentPercentage,ExpectedAttainmentPercentage,PacingIndex,ShortfallAmount,RiskLevel,RecommendedRemediation");
                    foreach (var r in pipReport.AtRiskReps)
                    {
                        sb.AppendLine($"{r.SalesPersonUserId},\"{EscapeCsv(r.SalesPersonName)}\",\"{EscapeCsv(r.SalesTeam)}\",\"{EscapeCsv(r.Branch)}\",{r.TargetValue},{r.AchievedValue},{r.AttainmentPercentage},{r.ExpectedAttainmentPercentage},{r.PacingIndex},{r.ShortfallAmount},{r.RiskLevel},\"{EscapeCsv(r.RecommendedRemediation)}\"");
                    }
                    break;
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("\"", "\"\"");
        }
    }
}
