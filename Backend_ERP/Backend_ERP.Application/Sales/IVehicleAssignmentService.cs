using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales
{
    public interface IVehicleAssignmentService
    {
        Task<List<VehicleAssignmentListItemDto>> GetVehicleAssignmentsAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default);

        Task<VehicleAssignmentDto> GetVehicleAssignmentByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<VehicleAssignmentDto> CreateVehicleAssignmentAsync(VehicleAssignmentCreateRequestDto payload, string currentUser, CancellationToken cancellationToken = default);

        Task<VehicleAssignmentDto> UpdateVehicleAssignmentAsync(int id, VehicleAssignmentUpdateRequestDto payload, string currentUser, CancellationToken cancellationToken = default);

        Task DeleteVehicleAssignmentAsync(int id, CancellationToken cancellationToken = default);

        Task<VehicleAssignmentDto> ChangeVehicleAsync(int id, VehicleAssignmentUpdateRequestDto payload, string currentUser, CancellationToken cancellationToken = default);

        Task<VehicleAssignmentDto> CancelVehicleAssignmentAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default);

        Task<VehicleAssignmentDto> MarkLoadedAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default);

        Task<VehicleAssignmentDto> MarkVehicleDispatchedAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default);

        Task<VehicleAssignmentDto> CompleteVehicleAssignmentAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default);

        Task<TransportDto> GenerateTransportAsync(int vehicleAssignmentId, string currentUser, CancellationToken cancellationToken = default);

        Task<VehicleAssignmentDashboardDto> GetVehicleAssignmentDashboardAsync(CancellationToken cancellationToken = default);

        Task<VehicleAssignmentDto> AssignVehicleToDispatchAsync(int dispatchId, VehicleAssignmentCreateRequestDto payload, string currentUser, CancellationToken cancellationToken = default);
    }
}
