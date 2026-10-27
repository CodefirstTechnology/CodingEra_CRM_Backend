using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales
{
    public interface IMtoProductionTriggerService
    {
        Task<MtoEvaluationResultDto> EvaluateAndSpawnProductionAsync(
            int salesOrderId,
            string actingUser,
            CancellationToken ct = default);
    }
}
