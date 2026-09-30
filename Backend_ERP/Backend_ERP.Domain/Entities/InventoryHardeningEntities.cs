using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend_ERP.Domain.Entities
{
    [Table("outbox_messages")]
    public class OutboxMessageEntity
    {
        [Key]
        [Column("outbox_id")]
        public Guid OutboxId { get; set; } = Guid.NewGuid();

        [Column("event_type")]
        public string EventType { get; set; } = null!;

        [Column("payload_json")]
        public string PayloadJson { get; set; } = null!;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("processed_at")]
        public DateTime? ProcessedAt { get; set; }

        [Column("error_log")]
        public string? ErrorLog { get; set; }

        [Column("retry_count")]
        public int RetryCount { get; set; } = 0;
    }
}
