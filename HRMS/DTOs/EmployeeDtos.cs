using System.ComponentModel.DataAnnotations;

namespace HRMS.DTOs;

public class EmployeeUpsertDto
{
    [MaxLength(32)]
    public string? EmployeeCode { get; set; }

    [Required]
    [MaxLength(256)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    public int DepartmentId { get; set; }

    [Required]
    public int DesignationId { get; set; }

    [Required]
    public int BranchId { get; set; }

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

    [MaxLength(128)]
    public string? BankName { get; set; }

    [MaxLength(32)]
    public string? AccountNumber { get; set; }

    [MaxLength(16)]
    public string? IfscCode { get; set; }
}

public class EmployeeDto
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int DesignationId { get; set; }
    public string DesignationName { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateOnly JoiningDate { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? BloodGroup { get; set; }
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string? IfscCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
