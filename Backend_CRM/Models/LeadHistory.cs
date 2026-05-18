using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.models
{
    /// <summary>Immutable snapshot of a <see cref="Lead"/> row immediately before an update (same scalars as <c>leads</c> at that point in time).</summary>
    [Table("lead_histories")]
    public class LeadHistory
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("lead_id")]
        public int LeadId { get; set; }

        [Column("archived_at")]
        public DateTime ArchivedAt { get; set; }

        [Column("first_name")]
        [MaxLength(128)]
        public string FirstName { get; set; } = string.Empty;

        [Column("last_name")]
        [MaxLength(128)]
        public string LastName { get; set; } = string.Empty;

        [Column("salutation_id")]
        public int? SalutationId { get; set; }

        [Column("gender")]
        [MaxLength(32)]
        public string Gender { get; set; } = string.Empty;

        [Column("mobile")]
        [MaxLength(64)]
        public string Mobile { get; set; } = string.Empty;

        [Column("email")]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Column("organization_id")]
        public int? OrganizationId { get; set; }

        [Column("lead_status_id")]
        public int? LeadStatusId { get; set; }

        [Column("request_type_id")]
        public int? RequestTypeId { get; set; }

        [Column("notes")]
        public string Notes { get; set; } = string.Empty;

        [Column("lead_owner_id")]
        public int? LeadOwnerId { get; set; }

        [Column("lead_source")]
        [MaxLength(64)]
        public string LeadSource { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
