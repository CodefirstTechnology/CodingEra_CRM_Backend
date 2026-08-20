using System.Collections.Generic;
using ERP.Domain.Sales;

namespace ERP.Application.Sales.Dtos
{
    public class PodListItemDto
    {
        public int Id { get; set; }
        public string PodNumber { get; set; } = string.Empty;
        public string DispatchNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string DeliveryDate { get; set; } = string.Empty;
        public string ReceiverName { get; set; } = string.Empty;
        public string ReceiverContact { get; set; } = string.Empty;
        public PodStatus Status { get; set; } = PodStatus.Pending;
    }

    public class PodDto
    {
        public int Id { get; set; }
        public string PodNumber { get; set; } = string.Empty;
        public int DispatchId { get; set; }
        public string DispatchNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string DeliveryDate { get; set; } = string.Empty;
        public string ReceiverName { get; set; } = string.Empty;
        public string ReceiverContact { get; set; } = string.Empty;
        public string DeliveryRemarks { get; set; } = string.Empty;
        public string DamageRemarks { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public PodStatus Status { get; set; } = PodStatus.Pending;
        public List<DispatchAttachmentDto> ProofOfDelivery { get; set; } = new();
        public List<DispatchAttachmentDto> Signature { get; set; } = new();
        public List<DispatchAttachmentDto> Photos { get; set; } = new();
        public List<DispatchAttachmentDto> Attachments { get; set; } = new();
        public List<DispatchTimelineEventDto> Timeline { get; set; } = new();
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
    }

    public class PodCreateRequestDto
    {
        public int DispatchId { get; set; }
        public string DispatchNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string DeliveryDate { get; set; } = string.Empty;
        public string ReceiverName { get; set; } = string.Empty;
        public string ReceiverContact { get; set; } = string.Empty;
        public string? DeliveryRemarks { get; set; }
        public string? DamageRemarks { get; set; }
        public string? Remarks { get; set; }
        public string? Notes { get; set; }
        public List<DispatchAttachmentDto>? ProofOfDelivery { get; set; }
        public List<DispatchAttachmentDto>? Signature { get; set; }
        public List<DispatchAttachmentDto>? Photos { get; set; }
    }

    public class PodUpdateRequestDto : PodCreateRequestDto
    {
    }

    public class PodDashboardDto
    {
        public int PendingPod { get; set; }
        public int Delivered { get; set; }
        public int Confirmed { get; set; }
    }
}
