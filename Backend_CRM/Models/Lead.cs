using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.models
{
    /// <summary>Manual CRM lead or IndiaMART import (<c>lead_source = IndiaMART</c> + optional IndiaMART-only fields).</summary>
    [Table("leads")]
    public class Lead
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        [MaxLength(512)]
        public string Name { get; set; } = string.Empty;

        [Column("first_name")]
        [MaxLength(128)]
        public string FirstName { get; set; } = string.Empty;

        [Column("last_name")]
        [MaxLength(128)]
        public string LastName { get; set; } = string.Empty;

        [Column("salutation")]
        [MaxLength(32)]
        public string Salutation { get; set; } = string.Empty;

        [Column("gender")]
        [MaxLength(32)]
        public string Gender { get; set; } = string.Empty;

        [Column("mobile")]
        [MaxLength(64)]
        public string Mobile { get; set; } = string.Empty;

        [Column("email")]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Column("organization")]
        [MaxLength(512)]
        public string Organization { get; set; } = string.Empty;

        [Column("organization_id")]
        public int? OrganizationId { get; set; }

        [Column("employees")]
        [MaxLength(128)]
        public string Employees { get; set; } = string.Empty;

        [Column("annual_revenue")]
        public decimal? AnnualRevenue { get; set; }

        [Column("website")]
        [MaxLength(512)]
        public string Website { get; set; } = string.Empty;

        [Column("territory")]
        [MaxLength(256)]
        public string Territory { get; set; } = string.Empty;

        [Column("industry")]
        [MaxLength(256)]
        public string Industry { get; set; } = string.Empty;

        [Column("job_title")]
        [MaxLength(256)]
        public string JobTitle { get; set; } = string.Empty;

        /// <summary>New | Contacted | Qualified | Lost | Converted (IndiaMART omits Lost).</summary>
        [Column("status")]
        [MaxLength(64)]
        public string Status { get; set; } = "New";

        [Column("request_type")]
        [MaxLength(256)]
        public string RequestType { get; set; } = string.Empty;

        [Column("notes")]
        public string Notes { get; set; } = string.Empty;

        [Column("source")]
        [MaxLength(256)]
        public string Source { get; set; } = string.Empty;

        [Column("lead_owner_name")]
        [MaxLength(256)]
        public string LeadOwnerName { get; set; } = string.Empty;

        [Column("owner")]
        [MaxLength(256)]
        public string Owner { get; set; } = string.Empty;

        [Column("lead_owner_id")]
        public int? LeadOwnerId { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        /// <summary>Manual | IndiaMART</summary>
        [Column("lead_source")]
        [MaxLength(64)]
        public string LeadSource { get; set; } = "Manual";

        [Column("sort_timestamp")]
        public long? SortTimestamp { get; set; }

        // IndiaMART / import channel (nullable for manual leads)

        [Column("external_ref")]
        [MaxLength(256)]
        public string? ExternalRef { get; set; }

        [Column("product")]
        [MaxLength(512)]
        public string? Product { get; set; }

        [Column("quantity")]
        public int? Quantity { get; set; }

        [Column("message")]
        public string? Message { get; set; }

        [Column("city")]
        [MaxLength(256)]
        public string? City { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }
    }
}
