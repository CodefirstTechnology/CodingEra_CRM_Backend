using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales
{
    public interface IEwayBillService
    {
        Task<List<EwayListItemDto>> GetEwayBillsAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default);

        Task<EwayDto> GetEwayBillByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<EwayDto> CreateEwayBillAsync(EwayCreateRequestDto payload, string currentUser, CancellationToken cancellationToken = default);

        Task<EwayDto> UpdateEwayBillAsync(int id, EwayUpdateRequestDto payload, string currentUser, CancellationToken cancellationToken = default);

        Task DeleteEwayBillAsync(int id, CancellationToken cancellationToken = default);

        Task<EwayDto> GenerateEwayBillAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default);

        Task<EwayDto> ActivateEwayBillAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default);

        Task<EwayDto> ExtendValidityAsync(int id, EwayExtendRequestDto? payload, string currentUser, CancellationToken cancellationToken = default);

        Task<EwayDto> CloseEwayBillAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default);

        Task<EwayDashboardDto> GetEwayDashboardAsync(CancellationToken cancellationToken = default);
    }
}
