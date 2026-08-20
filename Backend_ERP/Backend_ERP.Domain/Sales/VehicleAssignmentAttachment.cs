using System;

namespace ERP.Domain.Sales
{
    public class VehicleAssignmentAttachment
    {
        public int Id { get; set; }

        public int VehicleAssignmentId { get; set; }

        public VehicleAssignment VehicleAssignment { get; set; } = null!;

        public string AttachmentId { get; set; } = Guid.NewGuid().ToString();

        public string Name { get; set; } = string.Empty;

        public int SizeKb { get; set; }

        public string UploadedBy { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public string? Kind { get; set; } = "document";
    }

    public class VehicleAssignmentTimelineEvent
    {
        public int Id { get; set; }

        public int VehicleAssignmentId { get; set; }

        public VehicleAssignment VehicleAssignment { get; set; } = null!;

        public string EventId { get; set; } = Guid.NewGuid().ToString();

        public DateTime Date { get; set; } = DateTime.UtcNow;

        public string User { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;

        public string? FromStatus { get; set; }

        public string? ToStatus { get; set; }

        public string? Remarks { get; set; }
    }
}
