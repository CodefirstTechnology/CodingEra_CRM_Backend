using System;

namespace ERP.Domain.Procurement
{
    public class IncomingInspectionSample
    {
        public int Id { get; set; }

        public int IncomingChecklistItemId { get; set; }

        public IncomingChecklistItem? IncomingChecklistItem { get; set; }

        public int SampleIndex { get; set; }

        public decimal ObservedNumericValue { get; set; }

        public bool IsWithinLimits { get; set; } = true;

        public string MeasurementToolId { get; set; } = string.Empty;

        public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
    }
}
