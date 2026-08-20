using System.Collections.Generic;
using ERP.Domain.Sales;

namespace ERP.Application.Sales.Dtos
{
    public class VehicleAssignmentListItemDto
    {
        public int Id { get; set; }
        public string AssignmentNumber { get; set; } = string.Empty;
        public string DispatchNumber { get; set; } = string.Empty;
        public string VehicleNumber { get; set; } = string.Empty;
        public VehicleType VehicleType { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public string TransportCompanyName { get; set; } = string.Empty;
        public string LoadingDate { get; set; } = string.Empty;
        public decimal AssignedQuantity { get; set; }
        public VehicleAssignmentStatus Status { get; set; }
    }

    public class VehicleAssignmentDto
    {
        public int Id { get; set; }
        public string AssignmentNumber { get; set; } = string.Empty;
        public int DispatchId { get; set; }
        public string DispatchNumber { get; set; } = string.Empty;
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string VehicleNumber { get; set; } = string.Empty;
        public VehicleType VehicleType { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public string DriverContact { get; set; } = string.Empty;
        public int TransportCompanyId { get; set; }
        public string TransportCompanyName { get; set; } = string.Empty;
        public string LoadingDate { get; set; } = string.Empty;
        public string LoadingTime { get; set; } = string.Empty;
        public string ExpectedDeparture { get; set; } = string.Empty;
        public decimal Capacity { get; set; }
        public decimal AssignedQuantity { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public List<DispatchAttachmentDto> Attachments { get; set; } = new();
        public List<DispatchTimelineEventDto> Timeline { get; set; } = new();
        public VehicleAssignmentStatus Status { get; set; }
        public int? TransportId { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
    }

    public class VehicleAssignmentCreateRequestDto
    {
        public int DispatchId { get; set; }
        public string DispatchNumber { get; set; } = string.Empty;
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string VehicleNumber { get; set; } = string.Empty;
        public VehicleType VehicleType { get; set; } = VehicleType.Truck;
        public string DriverName { get; set; } = string.Empty;
        public string DriverContact { get; set; } = string.Empty;
        public int TransportCompanyId { get; set; }
        public string TransportCompanyName { get; set; } = string.Empty;
        public string LoadingDate { get; set; } = string.Empty;
        public string LoadingTime { get; set; } = string.Empty;
        public string ExpectedDeparture { get; set; } = string.Empty;
        public decimal Capacity { get; set; }
        public decimal AssignedQuantity { get; set; }
        public string? Remarks { get; set; }
        public string? Notes { get; set; }
    }

    public class VehicleAssignmentUpdateRequestDto : VehicleAssignmentCreateRequestDto
    {
    }

    public class VehicleAssignmentDashboardDto
    {
        public int VehiclesAssigned { get; set; }
        public int LoadingToday { get; set; }
        public int PendingAssignment { get; set; }
        public int CompletedDeliveries { get; set; }
    }

    public class TransportDto
    {
        public int Id { get; set; }
        public string TransportNumber { get; set; } = string.Empty;
        public int DispatchId { get; set; }
        public string DispatchNumber { get; set; } = string.Empty;
        public int VehicleAssignmentId { get; set; }
        public string AssignmentNumber { get; set; } = string.Empty;
        public string VehicleNumber { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string TransportCompanyName { get; set; } = string.Empty;
        public TransportMode Mode { get; set; } = TransportMode.Road;
        public string Route { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public decimal EstimatedTimeHours { get; set; }
        public string? ActualDeparture { get; set; }
        public string? ActualArrival { get; set; }
        public string FuelNotes { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public List<DispatchAttachmentDto> Attachments { get; set; } = new();
        public List<DispatchTimelineEventDto> Timeline { get; set; } = new();
        public TransportStatus Status { get; set; } = TransportStatus.Draft;
        public int? LrId { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
    }
}
