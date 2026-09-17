using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models;

[Table("job_requisitions")]
public class JobRequisition : ITenantEntity, IAuditableEntity
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("tenant_id")]
    public int TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }

    [Column("title")]
    [MaxLength(128)]
    public string Title { get; set; } = string.Empty;

    [Column("department_id")]
    public int? DepartmentId { get; set; }

    [ForeignKey(nameof(DepartmentId))]
    public Department? Department { get; set; }

    [Column("branch_id")]
    public int? BranchId { get; set; }

    [ForeignKey(nameof(BranchId))]
    public Branch? Branch { get; set; }

    [Column("openings_count")]
    public int OpeningsCount { get; set; } = 1;

    [Column("min_experience_years")]
    public int MinExperienceYears { get; set; } = 0;

    [Column("job_description")]
    public string? JobDescription { get; set; }

    [Column("target_date")]
    public DateOnly? TargetDate { get; set; }

    [Column("status")]
    [MaxLength(32)]
    public string Status { get; set; } = "Open"; // Draft, Open, On Hold, Closed

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("updated_by")]
    public int? UpdatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[Table("candidates")]
public class Candidate : ITenantEntity, IAuditableEntity
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("tenant_id")]
    public int TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }

    [Column("job_requisition_id")]
    public int? JobRequisitionId { get; set; }

    [ForeignKey(nameof(JobRequisitionId))]
    public JobRequisition? JobRequisition { get; set; }

    [Column("full_name")]
    [MaxLength(128)]
    public string FullName { get; set; } = string.Empty;

    [Column("email")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Column("phone_number")]
    [MaxLength(32)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Column("position")]
    [MaxLength(128)]
    public string Position { get; set; } = string.Empty;

    [Column("department_id")]
    public int? DepartmentId { get; set; }

    [ForeignKey(nameof(DepartmentId))]
    public Department? Department { get; set; }

    [Column("experience_years")]
    public decimal ExperienceYears { get; set; } = 0;

    [Column("resume_path")]
    [MaxLength(512)]
    public string? ResumePath { get; set; }

    [Column("source")]
    [MaxLength(64)]
    public string? Source { get; set; } // LinkedIn, Referral, Job Portal, Direct

    [Column("status")]
    [MaxLength(32)]
    public string Status { get; set; } = "APPLIED"; // APPLIED, SCREENING, INTERVIEW, SELECTED, REJECTED, OFFERED, OFFER_ACCEPTED, JOINED

    [Column("notes")]
    [MaxLength(512)]
    public string? Notes { get; set; }

    [Column("joined_employee_id")]
    public int? JoinedEmployeeId { get; set; }

    [ForeignKey(nameof(JoinedEmployeeId))]
    public Employee? JoinedEmployee { get; set; }

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("updated_by")]
    public int? UpdatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[Table("onboarding_tasks")]
public class OnboardingTask : ITenantEntity, IAuditableEntity
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("tenant_id")]
    public int TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }

    [Column("employee_id")]
    public int? EmployeeId { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    [Column("task_name")]
    [MaxLength(256)]
    public string TaskName { get; set; } = string.Empty;

    [Column("category")]
    [MaxLength(64)]
    public string Category { get; set; } = "Profile"; // Profile, Identity Documents, Address Proof, Education, Bank Details, Tax Info, Policy Agreement, Asset Allocation, System Access, Manager Intro

    [Column("is_mandatory")]
    public bool IsMandatory { get; set; } = true;

    [Column("is_completed")]
    public bool IsCompleted { get; set; } = false;

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("notes")]
    [MaxLength(512)]
    public string? Notes { get; set; }

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("updated_by")]
    public int? UpdatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
