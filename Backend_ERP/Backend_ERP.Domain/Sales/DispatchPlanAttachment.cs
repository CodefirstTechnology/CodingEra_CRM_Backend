using System;

namespace ERP.Domain.Sales
{
    public class DispatchPlanAttachment
    {
        public int Id { get; set; }

        public int DispatchPlanId { get; set; }

        public DispatchPlan DispatchPlan { get; set; } = null!;

        public string AttachmentId { get; set; } = Guid.NewGuid().ToString();

        public string Name { get; set; } = string.Empty;

        public int SizeKb { get; set; }

        public string UploadedBy { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public string? Kind { get; set; } = "document";
    }
}
