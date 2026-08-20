using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales
{
    public interface IPodService
    {
        Task<List<PodListItemDto>> GetPodsAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default);

        Task<PodDto> GetPodByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<PodDto> CreatePodAsync(PodCreateRequestDto payload, string currentUser, CancellationToken cancellationToken = default);

        Task<PodDto> UpdatePodAsync(int id, PodUpdateRequestDto payload, string currentUser, CancellationToken cancellationToken = default);

        Task DeletePodAsync(int id, CancellationToken cancellationToken = default);

        Task<PodDto> MarkPodDeliveredAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default);

        Task<PodDto> ConfirmDeliveryAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default);

        Task<PodDto> ClosePodAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default);

        Task<PodDashboardDto> GetPodDashboardAsync(CancellationToken cancellationToken = default);
    }
}
