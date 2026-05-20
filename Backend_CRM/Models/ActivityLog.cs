using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.models
{
    /// <summary>Timeline entry for a CRM record (lead, deal, contact, organization).</summary>
    [Table("activity_logs")]
    public class ActivityLog
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>lead | deal | contact | organization</summary>
        [Column("entity_type")]
        [MaxLength(32)]
        public string EntityType { get; set; } = string.Empty;

        [Column("entity_id")]
        public int EntityId { get; set; }

        /// <summary>created | updated | status_changed | field_updated | note_added | comment_added | task_added | call_logged | email_sent | deleted</summary>
        [Column("action_type")]
        [MaxLength(64)]
        public string ActionType { get; set; } = string.Empty;

        [Column("actor_user_id")]
        public int? ActorUserId { get; set; }

        [Column("actor_name")]
        [MaxLength(256)]
        public string ActorName { get; set; } = string.Empty;

        /// <summary>Human-readable line for the Activity tab.</summary>
        [Column("message")]
        [MaxLength(1024)]
        public string Message { get; set; } = string.Empty;

        [Column("field_name")]
        [MaxLength(128)]
        public string? FieldName { get; set; }

        [Column("old_value")]
        [MaxLength(512)]
        public string? OldValue { get; set; }

        [Column("new_value")]
        [MaxLength(512)]
        public string? NewValue { get; set; }

        /// <summary>Optional link to note, task, call, or comment row.</summary>
        [Column("related_record_type")]
        [MaxLength(32)]
        public string? RelatedRecordType { get; set; }

        [Column("related_record_id")]
        public int? RelatedRecordId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
