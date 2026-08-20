using System;
using System.Collections.Generic;

namespace ERP.Domain.Sales
{
    public enum PodStatus
    {
        Pending,
        Delivered,
        Confirmed,
        Closed
    }

    public class PodDocumentSequence
    {
        public int Id { get; set; }

        public string Prefix { get; set; } = "POD";

        public int LastSequence { get; set; }
    }

    public class DeliveryConfirmation
    {
        public int Id { get; set; }

        public string PodNumber { get; set; } = string.Empty;

        public int DispatchId { get; set; }

        public DispatchPlan? DispatchPlan { get; set; }

        public string DispatchNumber { get; set; } = string.Empty;

        public int CustomerId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public DateTime DeliveryDate { get; set; }

        public string ReceiverName { get; set; } = string.Empty;

        public string ReceiverContact { get; set; } = string.Empty;

        public string DeliveryRemarks { get; set; } = string.Empty;

        public string DamageRemarks { get; set; } = string.Empty;

        public string Remarks { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public PodStatus Status { get; set; } = PodStatus.Pending;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<PodAttachment> Attachments { get; set; } = new List<PodAttachment>();

        public ICollection<PodTimelineEvent> Timeline { get; set; } = new List<PodTimelineEvent>();
    }

    public class PodAttachment
    {
        public int Id { get; set; }

        public int DeliveryConfirmationId { get; set; }

        public DeliveryConfirmation DeliveryConfirmation { get; set; } = null!;

        public string AttachmentId { get; set; } = Guid.NewGuid().ToString();

        public string Name { get; set; } = string.Empty;

        public int SizeKb { get; set; }

        public string UploadedBy { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public string? Kind { get; set; } = "document";
    }

    public class PodTimelineEvent
    {
        public int Id { get; set; }

        public int DeliveryConfirmationId { get; set; }

        public DeliveryConfirmation DeliveryConfirmation { get; set; } = null!;

        public string EventId { get; set; } = Guid.NewGuid().ToString();

        public DateTime Date { get; set; } = DateTime.UtcNow;

        public string User { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;

        public string? FromStatus { get; set; }

        public string? ToStatus { get; set; }

        public string? Remarks { get; set; }
    }
}
