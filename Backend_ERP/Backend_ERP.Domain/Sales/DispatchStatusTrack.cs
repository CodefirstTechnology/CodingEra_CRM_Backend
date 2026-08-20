using System;
using System.Collections.Generic;

namespace ERP.Domain.Sales
{
    public enum DispatchTrackStatus
    {
        Ready,
        Dispatched,
        InTransit,
        Delivered,
        Completed,
        Delayed
    }

    public enum WarehouseTrackStatus
    {
        Staged,
        Packing,
        Issued
    }

    public enum VehicleTrackStatus
    {
        Pending,
        Assigned,
        Loaded,
        Dispatched,
        Completed
    }

    public enum TransportTrackStatus
    {
        Pending,
        InTransit,
        Delivered,
        Closed
    }

    public class DispatchStatusTrack
    {
        public int Id { get; set; }

        public int DispatchId { get; set; }

        public DispatchPlan? DispatchPlan { get; set; }

        public string DispatchNumber { get; set; } = string.Empty;

        public DispatchTrackStatus CurrentStatus { get; set; } = DispatchTrackStatus.Ready;

        public WarehouseTrackStatus WarehouseStatus { get; set; } = WarehouseTrackStatus.Staged;

        public VehicleTrackStatus VehicleStatus { get; set; } = VehicleTrackStatus.Pending;

        public TransportTrackStatus TransportStatus { get; set; } = TransportTrackStatus.Pending;

        public DateTime DispatchDate { get; set; } = DateTime.UtcNow;

        public DateTime ExpectedDelivery { get; set; } = DateTime.UtcNow.AddDays(1);

        public DateTime? ActualDelivery { get; set; }

        public int DelayHours { get; set; }

        public string Remarks { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<DispatchStatusTimelineEvent> Timeline { get; set; } = new List<DispatchStatusTimelineEvent>();
    }

    public class DispatchStatusTimelineEvent
    {
        public int Id { get; set; }

        public int DispatchStatusTrackId { get; set; }

        public DispatchStatusTrack DispatchStatusTrack { get; set; } = null!;

        public string EventId { get; set; } = Guid.NewGuid().ToString();

        public DateTime Date { get; set; } = DateTime.UtcNow;

        public string User { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;

        public string? FromStatus { get; set; }

        public string? ToStatus { get; set; }

        public string? Remarks { get; set; }
    }
}
