using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales
{
    public interface ISalesOrderAdvanceGateService
    {
        Task<AdvanceGateCheckResultDto> CanReleaseToProductionAsync(
            int salesOrderId,
            CancellationToken ct = default);

        Task<AdvanceGateCheckResultDto> CanReleaseToDispatchAsync(
            int salesOrderId,
            CancellationToken ct = default);
    }
}
