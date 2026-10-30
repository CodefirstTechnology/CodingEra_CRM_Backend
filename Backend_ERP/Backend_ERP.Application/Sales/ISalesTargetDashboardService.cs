using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales
{
    public interface ISalesTargetDashboardService
    {
        Task<SalesTargetDashboardDto> GetDashboardKPIsAsync(
            int fiscalYear,
            string? period,
            string? branch,
            int? repUserId,
            CancellationToken ct = default);

        Task InvalidateDashboardCacheAsync(string tenantId, CancellationToken ct = default);
    }
}

