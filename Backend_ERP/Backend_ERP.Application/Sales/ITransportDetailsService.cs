using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales
{
    public interface ITransportDetailsService
    {
        Task<List<TransportListItemDto>> GetTransportsAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default);

        Task<TransportDto> GetTransportByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<TransportDto> CreateTransportAsync(TransportCreateRequestDto payload, string currentUser, CancellationToken cancellationToken = default);

        Task<TransportDto> UpdateTransportAsync(int id, TransportUpdateRequestDto payload, string currentUser, CancellationToken cancellationToken = default);

        Task DeleteTransportAsync(int id, CancellationToken cancellationToken = default);

        Task<TransportDto> StartJourneyAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default);

        Task<TransportDto> MarkInTransitAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default);

        Task<TransportDto> MarkDeliveredAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default);

        Task<TransportDto> CloseTransportAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default);

        Task<LrDto> GenerateLrAsync(int id, string currentUser, CancellationToken cancellationToken = default);

        Task<TransportDashboardDto> GetTransportDashboardAsync(CancellationToken cancellationToken = default);
    }
}
