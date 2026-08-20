using System.Collections.Generic;
using ERP.Domain.Sales;

namespace ERP.Application.Sales.Dtos
{
    public class DispatchStatusListItemDto
    {
        public int Id { get; set; }
        public int DispatchId { get; set; }
        public string DispatchNumber { get; set; } = string.Empty;
        public DispatchTrackStatus CurrentStatus { get; set; } = DispatchTrackStatus.Ready;
        public WarehouseTrackStatus WarehouseStatus { get; set; } = WarehouseTrackStatus.Staged;
        public VehicleTrackStatus VehicleStatus { get; set; } = VehicleTrackStatus.Pending;
        public TransportTrackStatus TransportStatus { get; set; } = TransportTrackStatus.Pending;
        public string DispatchDate { get; set; } = string.Empty;
        public string ExpectedDelivery { get; set; } = string.Empty;
        public string? ActualDelivery { get; set; }
        public int DelayHours { get; set; }
    }

    public class DispatchStatusDto
    {
        public int Id { get; set; }
        public int DispatchId { get; set; }
        public string DispatchNumber { get; set; } = string.Empty;
        public DispatchTrackStatus CurrentStatus { get; set; } = DispatchTrackStatus.Ready;
        public WarehouseTrackStatus WarehouseStatus { get; set; } = WarehouseTrackStatus.Staged;
        public VehicleTrackStatus VehicleStatus { get; set; } = VehicleTrackStatus.Pending;
        public TransportTrackStatus TransportStatus { get; set; } = TransportTrackStatus.Pending;
        public string DispatchDate { get; set; } = string.Empty;
        public string ExpectedDelivery { get; set; } = string.Empty;
        public string? ActualDelivery { get; set; }
        public int DelayHours { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public List<DispatchTimelineEventDto> Timeline { get; set; } = new();
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
    }

    public class DispatchStatusUpdateRequestDto
    {
        public DispatchTrackStatus CurrentStatus { get; set; }
        public WarehouseTrackStatus? WarehouseStatus { get; set; }
        public VehicleTrackStatus? VehicleStatus { get; set; }
        public TransportTrackStatus? TransportStatus { get; set; }
        public string? ActualDelivery { get; set; }
        public int? DelayHours { get; set; }
        public string? Remarks { get; set; }
        public string? Notes { get; set; }
    }

    public class DispatchStatusDashboardDto
    {
        public int Ready { get; set; }
        public int InTransit { get; set; }
        public int Delivered { get; set; }
        public int Delayed { get; set; }
    }
}
