using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models;

[Table("document_types")]
public class DocumentType : ITenantEntity, IAuditableEntity
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("tenant_id")]
    public int TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }

    [Required]
    [Column("name")]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Column("code")]
    [MaxLength(64)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Column("category")]
    [MaxLength(64)]
    public string Category { get; set; } = "Other"; // Identity, Address, Education, Employment, Banking, Tax, Statutory, Joining, Company, Other

    [Column("description")]
    [MaxLength(512)]
    public string? Description { get; set; }

    [Column("is_required")]
    public bool IsRequired { get; set; } = true;

    [Column("is_mandatory_during_onboarding")]
    public bool IsMandatoryDuringOnboarding { get; set; } = false;

    [Column("requires_verification")]
    public bool RequiresVerification { get; set; } = true;

    [Column("has_expiry")]
    public bool HasExpiry { get; set; } = false;

    [Column("default_validity_period_months")]
    public int? DefaultValidityPeriodMonths { get; set; }

    [Column("allowed_file_types")]
    [MaxLength(128)]
    public string AllowedFileTypes { get; set; } = "pdf,jpg,jpeg,png";

    [Column("maximum_file_size_mb")]
    public int MaximumFileSizeMb { get; set; } = 5;

    [Column("status")]
    [MaxLength(32)]
    public string Status { get; set; } = "Active";

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("updated_by")]
    public int? UpdatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
