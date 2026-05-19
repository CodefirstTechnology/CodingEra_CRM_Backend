using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CRM.models
{
    /// <summary>
    /// CRM note linked to a Lead or Deal (<c>record_id</c>), with status, priority, tags, and attachments.
    /// </summary>
    [Table("notes")]
    public class Note : IAuditableByUser
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>Lead or Deal identifier this note belongs to.</summary>
        [Column("record_id")]
        public int RecordId { get; set; }

        /// <summary>Logged-in user who created or owns the note.</summary>
        [Column("author_id")]
        public int? AuthorId { get; set; }

        /// <summary>Display name for the note.</summary>
        [Column("name")]
        [MaxLength(256)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Title or headline of the note.</summary>
        [Column("title")]
        [MaxLength(512)]
        public string Title { get; set; } = string.Empty;

        /// <summary>Main note text / body.</summary>
        [Column("note")]
        [JsonPropertyName("body")]
        public string NoteText { get; set; } = string.Empty;

        /// <summary>lead | deal | contact | organization</summary>
        [Column("related_type")]
        [MaxLength(32)]
        public string RelatedType { get; set; } = "lead";

        [Column("related_id")]
        public int? RelatedEntityId { get; set; }

        [Column("related_name")]
        [MaxLength(512)]
        public string RelatedName { get; set; } = string.Empty;

        /// <summary>team | private</summary>
        [Column("visibility")]
        [MaxLength(32)]
        public string Visibility { get; set; } = "team";

        [Column("related_lead_id")]
        public int? RelatedLeadId { get; set; }

        [Column("related_deal_id")]
        public int? RelatedDealId { get; set; }

        [Column("related_contact_id")]
        public int? RelatedContactId { get; set; }

        [Column("related_organization_id")]
        public int? RelatedOrganizationId { get; set; }

        /// <summary>One of: active, archived, deleted.</summary>
        [Column("status")]
        [MaxLength(32)]
        public string Status { get; set; } = "active";

        /// <summary>One of: low, medium, high.</summary>
        [Column("priority")]
        [MaxLength(16)]
        public string Priority { get; set; } = "medium";

        /// <summary>Tags for filtering or search (e.g. JSON array or comma-separated values).</summary>
        [Column("tags")]
        public string Tags { get; set; } = string.Empty;

        /// <summary>Serialized file or document references (e.g. JSON array of URLs or storage keys).</summary>
        [Column("attachments")]
        public string Attachments { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_by")]
        public int? CreatedBy { get; set; }

        [Column("updated_by")]
        public int? UpdatedBy { get; set; }
    }
}
