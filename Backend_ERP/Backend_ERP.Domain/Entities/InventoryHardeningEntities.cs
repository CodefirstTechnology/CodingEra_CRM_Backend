using System;

namespace Backend_ERP.Domain.Entities
{
    public class OutboxMessageEntity
    {
        public Guid OutboxId { get; set; } = Guid.NewGuid();
        public string EventType { get; set; } = null!;
        public string PayloadJson { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
        public string? ErrorLog { get; set; }
        public int RetryCount { get; set; } = 0;
    }
}
