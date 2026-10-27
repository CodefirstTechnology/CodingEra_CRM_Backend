using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales
{
    public interface ISalesOrderAllocationService
    {
        Task<AllocationResultDto> AllocateOrderInventoryAsync(
            int salesOrderId,
            string actingUser,
            CancellationToken ct = default);

        Task<AllocationResultDto> SplitWarehouseAllocationAsync(
            int salesOrderId,
            SplitAllocationRequestDto request,
            string actingUser,
            CancellationToken ct = default);

        Task ReleaseOrderAllocationsAsync(
            int salesOrderId,
            string reason,
            string actingUser,
            CancellationToken ct = default);

        Task<IReadOnlyList<SalesOrderAllocationDto>> GetAllocationsForOrderAsync(
            int salesOrderId,
            CancellationToken ct = default);
    }
}
