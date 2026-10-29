using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales
{
    public interface IAdvancePaymentDashboardService
    {
        Task<AdvancePaymentDashboardKpiDto> GetDashboardKpisAsync(string tenantId = "default", CancellationToken cancellationToken = default);
    }
}
