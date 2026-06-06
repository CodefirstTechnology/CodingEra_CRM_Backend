using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CRM.models
{
    /// <summary>Manual CRM lead or IndiaMART import (<c>lead_source = IndiaMART</c> + optional IndiaMART-only fields).</summary>
    [Table("leads")]
    public class Lead : IAuditableByUser
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("first_name")]
        [MaxLength(128)]
        public string FirstName { get; set; } = string.Empty;

        [Column("last_name")]
        [MaxLength(128)]
        public string LastName { get; set; } = string.Empty;

        [Column("salutation_id")]
        public int? SalutationId { get; set; }

        [ForeignKey(nameof(SalutationId))]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Salutation? Salutation { get; set; }

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

        /// <summary>Linked company record; use <see cref="OrganizationId"/> for the FK.</summary>
        [ForeignKey(nameof(OrganizationId))]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Organization? Organization { get; set; }


        [Column("lead_status_id")]
        public int? LeadStatusId { get; set; }

        [ForeignKey(nameof(LeadStatusId))]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public LeadStatus? LeadStatus { get; set; }

        [Column("request_type_id")]
        public int? RequestTypeId { get; set; }

        [ForeignKey(nameof(RequestTypeId))]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RequestType? RequestType { get; set; }

        [Column("notes")]
        public string Notes { get; set; } = string.Empty;

        [Column("lead_owner_id")]
        public int? LeadOwnerId { get; set; }

        [ForeignKey(nameof(LeadOwnerId))]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public User? LeadOwner { get; set; }

        [Column("lead_source")]
        [MaxLength(64)]
        public string LeadSource { get; set; } = "Manual";

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("created_by")]
        public int? CreatedBy { get; set; }

        [Column("updated_by")]
        public int? UpdatedBy { get; set; }
    }
}
