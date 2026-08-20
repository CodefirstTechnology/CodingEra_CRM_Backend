using System;
using System.Collections.Generic;

namespace ERP.Domain.Sales
{
    public class VehicleAssignment
    {
        public int Id { get; set; }

        public string AssignmentNumber { get; set; } = string.Empty;

        public int DispatchId { get; set; }

        public DispatchPlan? DispatchPlan { get; set; }

        public string DispatchNumber { get; set; } = string.Empty;

        public int VehicleId { get; set; }

        public string VehicleName { get; set; } = string.Empty;

        public string VehicleNumber { get; set; } = string.Empty;

        public VehicleType VehicleType { get; set; } = VehicleType.Truck;

        public string DriverName { get; set; } = string.Empty;

        public string DriverContact { get; set; } = string.Empty;

        public int TransportCompanyId { get; set; }

        public string TransportCompanyName { get; set; } = string.Empty;

        public DateTime LoadingDate { get; set; }

        public string LoadingTime { get; set; } = string.Empty;

        public DateTime ExpectedDeparture { get; set; }

        public decimal Capacity { get; set; }

        public decimal AssignedQuantity { get; set; }

        public string Remarks { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public VehicleAssignmentStatus Status { get; set; } = VehicleAssignmentStatus.Draft;

        public int? TransportId { get; set; }

        public TransportDetail? Transport { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<VehicleAssignmentAttachment> Attachments { get; set; } = new List<VehicleAssignmentAttachment>();

        public ICollection<VehicleAssignmentTimelineEvent> Timeline { get; set; } = new List<VehicleAssignmentTimelineEvent>();
    }

    public class TransportDetail
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

        public DateTime? ActualDeparture { get; set; }

        public DateTime? ActualArrival { get; set; }

        public string FuelNotes { get; set; } = string.Empty;

        public string Remarks { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public TransportStatus Status { get; set; } = TransportStatus.Draft;

        public int? LrId { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
