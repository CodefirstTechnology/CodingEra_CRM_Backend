using System.ComponentModel.DataAnnotations;

namespace HRMS.DTOs;

public class EmployeeUpsertDto
{
    [MaxLength(32)]
    public string? EmployeeCode { get; set; }

    [MaxLength(128)]
    public string? FirstName { get; set; }

    [MaxLength(128)]
    public string? MiddleName { get; set; }

    [MaxLength(128)]
    public string? LastName { get; set; }

    [Required]
    [MaxLength(256)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(256)]
    public string? WorkEmail { get; set; }

    [Required]
    [MaxLength(32)]
    public string PhoneNumber { get; set; } = string.Empty;

    [MaxLength(32)]
    public string? AlternatePhone { get; set; }

    [Required]
    public int DepartmentId { get; set; }

    [Required]
    public int DesignationId { get; set; }

    [Required]
    public int BranchId { get; set; }

    public int? GradeId { get; set; }
    public int? CostCenterId { get; set; }
    public int? ShiftId { get; set; }
    public int? ReportingManagerId { get; set; }

    [MaxLength(128)]
    public string? WorkLocation { get; set; }

    [MaxLength(64)]
    public string EmploymentType { get; set; } = "Full Time";

    [Required]
    [MaxLength(32)]
    public string Status { get; set; } = "Active";

    [Required]
    public DateOnly JoiningDate { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [MaxLength(32)]
    public string? Gender { get; set; }

    [MaxLength(8)]
    public string? BloodGroup { get; set; }

    [MaxLength(32)]
    public string? MaritalStatus { get; set; }

    [MaxLength(512)]
    public string? ProfilePhotoUrl { get; set; }

    [MaxLength(512)]
    public string? CurrentAddress { get; set; }

    [MaxLength(512)]
    public string? PermanentAddress { get; set; }

    [MaxLength(128)]
    public string? EmergencyContactName { get; set; }

    [MaxLength(32)]
    public string? EmergencyContactPhone { get; set; }

    [MaxLength(32)]
    public string? Pan { get; set; }

    [MaxLength(32)]
    public string? Aadhaar { get; set; }

    [MaxLength(32)]
    public string? PassportNumber { get; set; }

    [MaxLength(32)]
    public string? DrivingLicense { get; set; }

    [MaxLength(32)]
    public string? Uan { get; set; }

    [MaxLength(32)]
    public string? EsicNumber { get; set; }

    [MaxLength(128)]
    public string? BankName { get; set; }

    [MaxLength(32)]
    public string? AccountNumber { get; set; }

    [MaxLength(16)]
    public string? IfscCode { get; set; }

    [MaxLength(128)]
    public string? AccountHolderName { get; set; }

    public decimal? CurrentCtc { get; set; }
}

public class EmployeeDto
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? WorkEmail { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? AlternatePhone { get; set; }

    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int DesignationId { get; set; }
    public string DesignationName { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;

    public int? GradeId { get; set; }
    public string? GradeName { get; set; }
    public int? CostCenterId { get; set; }
    public string? CostCenterName { get; set; }
    public int? ShiftId { get; set; }
    public string? ShiftName { get; set; }
    public int? ReportingManagerId { get; set; }
    public string? ReportingManagerName { get; set; }

    public string? WorkLocation { get; set; }
    public string EmploymentType { get; set; } = "Full Time";
    public string Status { get; set; } = string.Empty;
    public DateOnly JoiningDate { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? BloodGroup { get; set; }
    public string? MaritalStatus { get; set; }
    public string? ProfilePhotoUrl { get; set; }

    public string? CurrentAddress { get; set; }
    public string? PermanentAddress { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }

    public string? Pan { get; set; }
    public string? Aadhaar { get; set; }
    public string? PassportNumber { get; set; }
    public string? DrivingLicense { get; set; }
    public string? Uan { get; set; }
    public string? EsicNumber { get; set; }

    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string? IfscCode { get; set; }
    public string? AccountHolderName { get; set; }
    public decimal? CurrentCtc { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class EmployeeQueryFilterDto
{
    public string? SearchTerm { get; set; }
    public int? BranchId { get; set; }
    public int? DepartmentId { get; set; }
    public int? DesignationId { get; set; }
    public int? GradeId { get; set; }
    public string? EmploymentType { get; set; }
    public string? Status { get; set; }
    public DateOnly? JoiningFrom { get; set; }
    public DateOnly? JoiningTo { get; set; }
}
