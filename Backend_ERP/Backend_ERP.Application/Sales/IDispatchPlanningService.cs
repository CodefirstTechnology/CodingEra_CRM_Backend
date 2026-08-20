using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales
{
    public interface IDispatchPlanningService
    {
        Task<List<DispatchPlanListItemDto>> GetDispatchPlansAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default);

        Task<DispatchPlanDto> GetDispatchPlanByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<DispatchPlanDto> CreateDispatchPlanAsync(DispatchPlanCreateRequestDto payload, string currentUser, CancellationToken cancellationToken = default);

        Task<DispatchPlanDto> UpdateDispatchPlanAsync(int id, DispatchPlanUpdateRequestDto payload, string currentUser, CancellationToken cancellationToken = default);

        Task DeleteDispatchPlanAsync(int id, CancellationToken cancellationToken = default);

        Task<DispatchPlanDto> DuplicateDispatchPlanAsync(int id, string currentUser, CancellationToken cancellationToken = default);

        Task<DispatchPlanDto> PlanDispatchAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default);

        Task<DispatchPlanDto> ApproveDispatchAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default);

        Task<DispatchPlanDto> MarkReadyForDispatchAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default);

        Task<DispatchPlanDto> CloseDispatchAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default);

        Task<DispatchPlanDashboardDto> GetDispatchPlanDashboardAsync(CancellationToken cancellationToken = default);
    }
}
