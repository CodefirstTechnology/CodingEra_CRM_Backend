using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales
{
    public interface IDispatchStatusService
    {
        Task<List<DispatchStatusListItemDto>> GetDispatchStatusesAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default);

        Task<DispatchStatusDto> GetDispatchStatusByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<DispatchStatusDto> UpdateDispatchStatusAsync(int id, DispatchStatusUpdateRequestDto payload, string currentUser, CancellationToken cancellationToken = default);

        Task<DispatchStatusDashboardDto> GetDispatchStatusDashboardAsync(CancellationToken cancellationToken = default);
    }
}
