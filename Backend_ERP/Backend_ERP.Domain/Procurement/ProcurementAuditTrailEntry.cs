using System;

namespace ERP.Domain.Procurement
{
    public class ProcurementAuditTrailEntry
    {
        public int Id { get; set; }

        public string Module { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;

        public int EntityId { get; set; }

        public string EntityNumber { get; set; } = string.Empty;

        public string User { get; set; } = string.Empty;

        public DateTime Date { get; set; } = DateTime.UtcNow;

        public string OldValue { get; set; } = string.Empty;

        public string NewValue { get; set; } = string.Empty;

        public string Remarks { get; set; } = string.Empty;
    }
}
