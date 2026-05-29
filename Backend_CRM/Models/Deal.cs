using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CRM.models
{
    [Table("deals")]
    public class Deal : IAuditableByUser
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("organization_id")]
        public int? OrganizationId { get; set; }

        [Column("contact_id")]
        public int? ContactId { get; set; }

        [Column("organization_name")]
        [MaxLength(512)]
        public string OrganizationName { get; set; } = string.Empty;

        [Column("salutation")]
        [MaxLength(32)]
        public string Salutation { get; set; } = string.Empty;

        [Column("first_name")]
        [MaxLength(128)]
        public string FirstName { get; set; } = string.Empty;

        [Column("last_name")]
        [MaxLength(128)]
        public string LastName { get; set; } = string.Empty;

        [Column("email")]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Column("mobile")]
        [MaxLength(64)]
        public string Mobile { get; set; } = string.Empty;

        [Column("gender")]
        [MaxLength(32)]
        public string Gender { get; set; } = string.Empty;

        [Column("annual_revenue")]
        public decimal? AnnualRevenue { get; set; }

        [Column("employees")]
        [MaxLength(128)]
        public string Employees { get; set; } = string.Empty;

        [Column("website")]
        [MaxLength(512)]
        public string Website { get; set; } = string.Empty;

        [Column("territory")]
        [MaxLength(256)]
        public string Territory { get; set; } = string.Empty;

        [Column("industry")]
        [MaxLength(256)]
        public string Industry { get; set; } = string.Empty;

        [Column("deal_status_id")]
        public int? DealStatusId { get; set; }

        [ForeignKey(nameof(DealStatusId))]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DealStatus? DealStatus { get; set; }

        /// <summary>Denormalized status label; kept in sync with <see cref="DealStatus"/>.</summary>
        [Column("status")]
        [MaxLength(64)]
        public string Status { get; set; } = "Quotation Shared";

        [Column("deal_owner_id")]
        public int? DealOwnerId { get; set; }

        [Column("assigned_to_user_id")]
        public int? AssignedToUserId { get; set; }

        [Column("assigned_initials")]
        [MaxLength(16)]
        public string AssignedInitials { get; set; } = string.Empty;

        [Column("related_contact_id")]
        public int? RelatedContactId { get; set; }

        [Column("related_organization_id")]
        public int? RelatedOrganizationId { get; set; }

        [Column("probability_percent")]
        public int? ProbabilityPercent { get; set; }

        [Column("next_step")]
        public string NextStep { get; set; } = string.Empty;

        [Column("next_follow_up_date")]
        public DateTime? NextFollowUpDate { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("last_modified")]
        public DateTime LastModified { get; set; }

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
