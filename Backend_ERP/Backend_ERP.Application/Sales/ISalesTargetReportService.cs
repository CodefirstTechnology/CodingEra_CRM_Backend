using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales
{
    public interface ISalesTargetReportService
    {
        Task<SalesTargetVarianceReportDto> GetVarianceReportAsync(
            SalesTargetReportFilterDto filter,
            CancellationToken ct = default);

        Task<SalesTargetRepLeaderboardDto> GetRepLeaderboardAuditAsync(
            int fiscalYear,
            string? period,
            CancellationToken ct = default);

        Task<SalesTargetCategoryMatrixDto> GetCategoryContributionMatrixAsync(
            int fiscalYear,
            string? period,
            CancellationToken ct = default);
    }
}

