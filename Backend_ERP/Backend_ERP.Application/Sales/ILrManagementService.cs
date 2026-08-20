using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales
{
    public interface ILrManagementService
    {
        Task<List<LrListItemDto>> GetLrsAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default);

        Task<LrDto> GetLrByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<LrDto> CreateLrAsync(LrCreateRequestDto payload, string currentUser, CancellationToken cancellationToken = default);

        Task<LrDto> UpdateLrAsync(int id, LrUpdateRequestDto payload, string currentUser, CancellationToken cancellationToken = default);

        Task DeleteLrAsync(int id, CancellationToken cancellationToken = default);

        Task<LrDto> GenerateLrDocumentAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default);

        Task<LrDto> IssueLrAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default);

        Task<LrDto> CloseLrAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default);

        Task<EwayDto> GenerateEwayFromLrAsync(int lrId, string currentUser, CancellationToken cancellationToken = default);

        Task<LrDashboardDto> GetLrDashboardAsync(CancellationToken cancellationToken = default);
    }
}
