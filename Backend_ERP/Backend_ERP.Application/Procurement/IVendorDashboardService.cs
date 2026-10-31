using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement.Dtos;

namespace ERP.Application.Procurement
{
    public interface IVendorDashboardService
    {
        Task<VendorDashboardKpiDto> GetDashboardKpisAsync(
            string tenantId = "default",
            bool forceRefresh = false,
            CancellationToken cancellationToken = default);

        Task<VendorDashboardChartsDto> GetDashboardChartsAsync(
            string tenantId = "default",
            bool forceRefresh = false,
            CancellationToken cancellationToken = default);
    }
}

