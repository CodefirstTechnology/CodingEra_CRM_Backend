using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales
{
    public interface IProformaDashboardService
    {
        Task<ProformaDashboardTelemetryDto> GetDashboardTelemetryAsync(
            string tenantId = "default",
            bool forceRefresh = false,
            CancellationToken ct = default);

        Task InvalidateDashboardCacheAsync(string tenantId = "default", CancellationToken ct = default);
    }
}
