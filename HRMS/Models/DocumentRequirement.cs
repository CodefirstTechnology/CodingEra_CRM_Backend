using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models;

[Table("document_requirements")]
public class DocumentRequirement : ITenantEntity, IAuditableEntity
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("tenant_id")]
    public int TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }

    [Column("document_type_id")]
    public int DocumentTypeId { get; set; }

    [ForeignKey(nameof(DocumentTypeId))]
    public DocumentType? DocumentType { get; set; }

    [Column("employment_type")]
    [MaxLength(64)]
    public string? EmploymentType { get; set; } // Full Time, Contract, Intern, etc. (null for all)

    [Column("department_id")]
    public int? DepartmentId { get; set; }

    [ForeignKey(nameof(DepartmentId))]
    public Department? Department { get; set; }

    [Column("designation_id")]
    public int? DesignationId { get; set; }

    [ForeignKey(nameof(DesignationId))]
    public Designation? Designation { get; set; }

    [Column("branch_id")]
    public int? BranchId { get; set; }

    [ForeignKey(nameof(BranchId))]
    public Branch? Branch { get; set; }

    [Column("gender")]
    [MaxLength(32)]
    public string? Gender { get; set; } // Male, Female, etc. (null for all)

    [Column("is_mandatory")]
    public bool IsMandatory { get; set; } = true;

    [Column("is_onboarding_mandatory")]
    public bool IsOnboardingMandatory { get; set; } = true;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("remarks")]
    [MaxLength(512)]
    public string? Remarks { get; set; }

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("updated_by")]
    public int? UpdatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
