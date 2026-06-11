using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CRM.models
{
    public enum LeadSyncType
    {
        Manual = 0,
        Auto = 1,
    }

    public enum LeadSyncStatus
    {
        Running = 0,
        Completed = 1,
        Failed = 2,
        Partial = 3,
    }

    [Table("lead_sync_logs")]
    public class LeadSyncLog
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("source_id")]
        public int SourceId { get; set; }

        [ForeignKey(nameof(SourceId))]
        [JsonIgnore]
        public LeadSyncSource? Source { get; set; }

        [Column("sync_type")]
        public LeadSyncType SyncType { get; set; }

        [Column("started_at")]
        public DateTime StartedAt { get; set; }

        [Column("ended_at")]
        public DateTime? EndedAt { get; set; }

        [Column("total_received")]
        public int TotalReceived { get; set; }

        [Column("total_created")]
        public int TotalCreated { get; set; }

        [Column("failed_count")]
        public int FailedCount { get; set; }

        [Column("triggered_by_user_id")]
        public int? TriggeredByUserId { get; set; }

        [ForeignKey(nameof(TriggeredByUserId))]
        [JsonIgnore]
        public User? TriggeredByUser { get; set; }

        [Column("status")]
        public LeadSyncStatus Status { get; set; }

        [Column("error_message")]
        [MaxLength(512)]
        public string? ErrorMessage { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
