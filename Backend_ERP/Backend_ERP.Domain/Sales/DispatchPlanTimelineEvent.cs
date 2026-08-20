using System;

namespace ERP.Domain.Sales
{
    public class DispatchPlanTimelineEvent
    {
        public int Id { get; set; }

        public int DispatchPlanId { get; set; }

        public DispatchPlan DispatchPlan { get; set; } = null!;

        public string EventId { get; set; } = Guid.NewGuid().ToString();

        public DateTime Date { get; set; } = DateTime.UtcNow;

        public string User { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;

        public string? FromStatus { get; set; }

        public string? ToStatus { get; set; }

        public string? Remarks { get; set; }
    }
}
