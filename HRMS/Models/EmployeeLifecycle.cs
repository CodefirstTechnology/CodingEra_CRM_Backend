using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models;

[Table("employee_transfers")]
public class EmployeeTransfer : ITenantEntity, IAuditableEntity
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
    public int EmployeeId { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    [Column("current_branch_id")]
    public int? CurrentBranchId { get; set; }

    [Column("new_branch_id")]
    public int? NewBranchId { get; set; }

    [Column("current_department_id")]
    public int? CurrentDepartmentId { get; set; }

    [Column("new_department_id")]
    public int? NewDepartmentId { get; set; }

    [Column("current_designation_id")]
    public int? CurrentDesignationId { get; set; }

    [Column("new_designation_id")]
    public int? NewDesignationId { get; set; }

    [Column("current_reporting_manager_id")]
    public int? CurrentReportingManagerId { get; set; }

    [Column("new_reporting_manager_id")]
    public int? NewReportingManagerId { get; set; }

    [Column("current_cost_center_id")]
    public int? CurrentCostCenterId { get; set; }

    [Column("new_cost_center_id")]
    public int? NewCostCenterId { get; set; }

    [Column("effective_date")]
    public DateOnly EffectiveDate { get; set; }

    [Column("reason")]
    [MaxLength(512)]
    public string Reason { get; set; } = string.Empty;

    [Column("remarks")]
    [MaxLength(512)]
    public string? Remarks { get; set; }

    [Column("requested_by_user_id")]
    public int? RequestedByUserId { get; set; }

    [Column("approved_by_user_id")]
    public int? ApprovedByUserId { get; set; }

    [Column("status")]
    [MaxLength(32)]
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("updated_by")]
    public int? UpdatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[Table("employee_promotions")]
public class EmployeePromotion : ITenantEntity, IAuditableEntity
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
    public int EmployeeId { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    [Column("current_designation_id")]
    public int? CurrentDesignationId { get; set; }

    [Column("new_designation_id")]
    public int? NewDesignationId { get; set; }

    [Column("current_grade_id")]
    public int? CurrentGradeId { get; set; }

    [Column("new_grade_id")]
    public int? NewGradeId { get; set; }

    [Column("current_department_id")]
    public int? CurrentDepartmentId { get; set; }

    [Column("new_department_id")]
    public int? NewDepartmentId { get; set; }

    [Column("current_salary")]
    public decimal? CurrentSalary { get; set; }

    [Column("new_salary")]
    public decimal? NewSalary { get; set; }

    [Column("salary_revision_reference")]
    [MaxLength(64)]
    public string? SalaryRevisionReference { get; set; }

    [Column("effective_date")]
    public DateOnly EffectiveDate { get; set; }

    [Column("reason")]
    [MaxLength(512)]
    public string Reason { get; set; } = string.Empty;

    [Column("remarks")]
    [MaxLength(512)]
    public string? Remarks { get; set; }

    [Column("requested_by_user_id")]
    public int? RequestedByUserId { get; set; }

    [Column("approved_by_user_id")]
    public int? ApprovedByUserId { get; set; }

    [Column("status")]
    [MaxLength(32)]
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("updated_by")]
    public int? UpdatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[Table("salary_revision_requests")]
public class SalaryRevisionRequest : ITenantEntity, IAuditableEntity
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
    public int EmployeeId { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    [Column("current_salary")]
    public decimal CurrentSalary { get; set; }

    [Column("new_salary")]
    public decimal NewSalary { get; set; }

    [Column("effective_date")]
    public DateOnly EffectiveDate { get; set; }

    [Column("revision_type")]
    [MaxLength(64)]
    public string RevisionType { get; set; } = "Annual Increment"; // Annual Increment, Promotion, Market Adjustment, Correction, Other

    [Column("reason")]
    [MaxLength(512)]
    public string Reason { get; set; } = string.Empty;

    [Column("remarks")]
    [MaxLength(512)]
    public string? Remarks { get; set; }

    [Column("requested_by_user_id")]
    public int? RequestedByUserId { get; set; }

    [Column("approved_by_user_id")]
    public int? ApprovedByUserId { get; set; }

    [Column("status")]
    [MaxLength(32)]
    public string Status { get; set; } = "Draft"; // Draft, Pending Approval, Approved, Rejected

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("updated_by")]
    public int? UpdatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[Table("employee_exits")]
public class EmployeeExit : ITenantEntity, IAuditableEntity
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
    public int EmployeeId { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    [Column("exit_type")]
    [MaxLength(64)]
    public string ExitType { get; set; } = "RESIGNATION"; // RESIGNATION, TERMINATION, RETIREMENT, CONTRACT_END, OTHER

    [Column("resignation_date")]
    public DateOnly ResignationDate { get; set; }

    [Column("notice_period_days")]
    public int NoticePeriodDays { get; set; } = 30;

    [Column("last_working_date")]
    public DateOnly LastWorkingDate { get; set; }

    [Column("reason")]
    [MaxLength(512)]
    public string Reason { get; set; } = string.Empty;

    [Column("remarks")]
    [MaxLength(512)]
    public string? Remarks { get; set; }

    [Column("status")]
    [MaxLength(32)]
    public string Status { get; set; } = "Initiated"; // Initiated, Under Notice, Clearance Pending, Completed, Cancelled

    [Column("asset_clearance_status")]
    [MaxLength(32)]
    public string AssetClearanceStatus { get; set; } = "Pending";

    [Column("leave_settlement_status")]
    [MaxLength(32)]
    public string LeaveSettlementStatus { get; set; } = "Pending";

    [Column("payroll_settlement_status")]
    [MaxLength(32)]
    public string PayrollSettlementStatus { get; set; } = "Pending";

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("updated_by")]
    public int? UpdatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[Table("employee_lifecycle_histories")]
public class EmployeeLifecycleHistory : ITenantEntity
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
    public int EmployeeId { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    [Column("event_type")]
    [MaxLength(128)]
    public string EventType { get; set; } = string.Empty; // Created, Department Changed, Branch Changed, Designation Changed, Manager Changed, Promotion, Transfer, Salary Revision, Status Changed, Resignation, Exit

    [Column("old_value")]
    [MaxLength(512)]
    public string? OldValue { get; set; }

    [Column("new_value")]
    [MaxLength(512)]
    public string? NewValue { get; set; }

    [Column("reason")]
    [MaxLength(512)]
    public string? Reason { get; set; }

    [Column("changed_by_user_id")]
    public int? ChangedByUserId { get; set; }

    [Column("changed_by_name")]
    [MaxLength(128)]
    public string? ChangedByName { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
