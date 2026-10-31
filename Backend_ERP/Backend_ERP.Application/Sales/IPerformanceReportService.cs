using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales
{
    public interface IPerformanceReportService
    {
        Task<ExecutiveCommissionPayoutReportDto> GetExecutiveCommissionPayoutStatementAsync(
            int financialYear,
            string? branch,
            string? salesTeam,
            CancellationToken ct = default);

        Task<RepCohortVelocityMatrixReportDto> GetRepCohortVelocityMatrixAsync(
            int financialYear,
            string? branch,
            string? salesTeam,
            CancellationToken ct = default);

        Task<DiscountMarginErosionReportDto> GetDiscountMarginErosionReportAsync(
            int financialYear,
            string? branch,
            string? salesTeam,
            CancellationToken ct = default);

        Task<PipEarlyWarningAuditReportDto> GetPipEarlyWarningAuditAsync(
            int financialYear,
            string? branch,
            string? salesTeam,
            CancellationToken ct = default);

        Task<byte[]> ExportReportToCsvAsync(
            string reportType,
            int financialYear,
            string? branch,
            string? salesTeam,
            CancellationToken ct = default);
    }
}

