using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models;

[Table("employees")]
public class Employee : ITenantEntity, IAuditableEntity
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("tenant_id")]
    public int TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }

    [Column("employee_code")]
    [MaxLength(32)]
    public string EmployeeCode { get; set; } = string.Empty;

    [Column("first_name")]
    [MaxLength(128)]
    public string? FirstName { get; set; }

    [Column("middle_name")]
    [MaxLength(128)]
    public string? MiddleName { get; set; }

    [Column("last_name")]
    [MaxLength(128)]
    public string? LastName { get; set; }

    [Column("full_name")]
    [MaxLength(256)]
    public string FullName { get; set; } = string.Empty;

    [Column("email")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Column("work_email")]
    [MaxLength(256)]
    public string? WorkEmail { get; set; }

    [Column("phone_number")]
    [MaxLength(32)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Column("alternate_phone")]
    [MaxLength(32)]
    public string? AlternatePhone { get; set; }

    [Column("department_id")]
    public int DepartmentId { get; set; }

    [ForeignKey(nameof(DepartmentId))]
    public Department? Department { get; set; }

    [Column("designation_id")]
    public int DesignationId { get; set; }

    [ForeignKey(nameof(DesignationId))]
    public Designation? Designation { get; set; }

    [Column("branch_id")]
    public int BranchId { get; set; }

    [ForeignKey(nameof(BranchId))]
    public Branch? Branch { get; set; }

    [Column("grade_id")]
    public int? GradeId { get; set; }

    [ForeignKey(nameof(GradeId))]
    public Grade? Grade { get; set; }

    [Column("cost_center_id")]
    public int? CostCenterId { get; set; }

    [ForeignKey(nameof(CostCenterId))]
    public CostCenter? CostCenter { get; set; }

    [Column("shift_id")]
    public int? ShiftId { get; set; }

    [ForeignKey(nameof(ShiftId))]
    public Shift? Shift { get; set; }

    [Column("reporting_manager_id")]
    public int? ReportingManagerId { get; set; }

    [ForeignKey(nameof(ReportingManagerId))]
    public Employee? ReportingManager { get; set; }

    [Column("work_location")]
    [MaxLength(128)]
    public string? WorkLocation { get; set; }

    [Column("employment_type")]
    [MaxLength(64)]
    public string EmploymentType { get; set; } = "Full Time"; // Full Time, Part Time, Contract, Intern, Temporary, Consultant

    [Column("status")]
    [MaxLength(32)]
    public string Status { get; set; } = "Active"; // Active, Probation, Confirmed, On Notice, Suspended, Resigned, Terminated, Exited, Inactive

    [Column("joining_date")]
    public DateOnly JoiningDate { get; set; }

    [Column("date_of_birth")]
    public DateOnly? DateOfBirth { get; set; }

    [Column("gender")]
    [MaxLength(32)]
    public string? Gender { get; set; }

    [Column("blood_group")]
    [MaxLength(8)]
    public string? BloodGroup { get; set; }

    [Column("marital_status")]
    [MaxLength(32)]
    public string? MaritalStatus { get; set; }

    [Column("profile_photo_url")]
    [MaxLength(512)]
    public string? ProfilePhotoUrl { get; set; }

    [Column("current_address")]
    [MaxLength(512)]
    public string? CurrentAddress { get; set; }

    [Column("permanent_address")]
    [MaxLength(512)]
    public string? PermanentAddress { get; set; }

    [Column("emergency_contact_name")]
    [MaxLength(128)]
    public string? EmergencyContactName { get; set; }

    [Column("emergency_contact_phone")]
    [MaxLength(32)]
    public string? EmergencyContactPhone { get; set; }

    [Column("pan")]
    [MaxLength(32)]
    public string? Pan { get; set; }

    [Column("aadhaar")]
    [MaxLength(32)]
    public string? Aadhaar { get; set; }

    [Column("passport_number")]
    [MaxLength(32)]
    public string? PassportNumber { get; set; }

    [Column("driving_license")]
    [MaxLength(32)]
    public string? DrivingLicense { get; set; }

    [Column("uan")]
    [MaxLength(32)]
    public string? Uan { get; set; }

    [Column("esic_number")]
    [MaxLength(32)]
    public string? EsicNumber { get; set; }

    [Column("bank_name")]
    [MaxLength(128)]
    public string? BankName { get; set; }

    [Column("account_number")]
    [MaxLength(32)]
    public string? AccountNumber { get; set; }

    [Column("ifsc_code")]
    [MaxLength(16)]
    public string? IfscCode { get; set; }

    [Column("account_holder_name")]
    [MaxLength(128)]
    public string? AccountHolderName { get; set; }

    [Column("current_ctc")]
    public decimal? CurrentCtc { get; set; }

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("updated_by")]
    public int? UpdatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
