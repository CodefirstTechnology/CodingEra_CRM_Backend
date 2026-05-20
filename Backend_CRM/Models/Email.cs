using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.models
{
    [Table("emails")]
    public class Email : IAuditableByUser
    {
        public const string StatusSent = "sent";
        public const string StatusFailed = "failed";

        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>lead | deal | contact</summary>
        [Column("entity_type")]
        [MaxLength(32)]
        public string EntityType { get; set; } = "lead";

        [Column("entity_id")]
        public int EntityId { get; set; }

        [Column("to_email")]
        [MaxLength(320)]
        public string ToEmail { get; set; } = string.Empty;

        [Column("subject")]
        [MaxLength(512)]
        public string Subject { get; set; } = string.Empty;

        [Column("body")]
        public string Body { get; set; } = string.Empty;

        /// <summary>sent | failed</summary>
        [Column("status")]
        [MaxLength(16)]
        public string Status { get; set; } = StatusFailed;

        [Column("failure_message")]
        [MaxLength(256)]
        public string? FailureMessage { get; set; }

        [Column("sent_by")]
        public int? SentBy { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("created_by")]
        public int? CreatedBy { get; set; }

        [Column("updated_by")]
        public int? UpdatedBy { get; set; }
    }
}
