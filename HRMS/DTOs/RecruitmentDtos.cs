using System.ComponentModel.DataAnnotations;

namespace HRMS.DTOs;

public class JobRequisitionDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public int OpeningsCount { get; set; }
    public int MinExperienceYears { get; set; }
    public string? JobDescription { get; set; }
    public DateOnly? TargetDate { get; set; }
    public string Status { get; set; } = "Open";
    public DateTime CreatedAt { get; set; }
}

public class JobRequisitionUpsertDto
{
    [Required]
    [MaxLength(128)]
    public string Title { get; set; } = string.Empty;

    public int? DepartmentId { get; set; }
    public int? BranchId { get; set; }

    [Range(1, 1000)]
    public int OpeningsCount { get; set; } = 1;

    public int MinExperienceYears { get; set; } = 0;
    public string? JobDescription { get; set; }
    public DateOnly? TargetDate { get; set; }

    [MaxLength(32)]
    public string Status { get; set; } = "Open"; // Draft, Open, On Hold, Closed
}

public class CandidateDto
{
    public int Id { get; set; }
    public int? JobRequisitionId { get; set; }
    public string? JobTitle { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public decimal ExperienceYears { get; set; }
    public string? ResumePath { get; set; }
    public string? Source { get; set; }
    public string Status { get; set; } = "APPLIED";
    public string? Notes { get; set; }
    public int? JoinedEmployeeId { get; set; }
    public string? JoinedEmployeeName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CandidateUpsertDto
{
    public int? JobRequisitionId { get; set; }

    [Required]
    [MaxLength(128)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string Position { get; set; } = string.Empty;

    public int? DepartmentId { get; set; }
    public decimal ExperienceYears { get; set; } = 0;

    [MaxLength(512)]
    public string? ResumePath { get; set; }

    [MaxLength(64)]
    public string? Source { get; set; } = "Job Portal"; // LinkedIn, Referral, Job Portal, Direct

    [MaxLength(32)]
    public string Status { get; set; } = "APPLIED"; // APPLIED, SCREENING, INTERVIEW, SELECTED, REJECTED, OFFERED, OFFER_ACCEPTED, JOINED

    [MaxLength(512)]
    public string? Notes { get; set; }
}

public class CandidateStatusUpdateDto
{
    [Required]
    [MaxLength(32)]
    public string Status { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Notes { get; set; }

    public int? JoinedEmployeeId { get; set; }
}

public class OnboardingTaskDto
{
    public int Id { get; set; }
    public int? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public string Category { get; set; } = "Profile";
    public bool IsMandatory { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OnboardingTaskUpsertDto
{
    public int? EmployeeId { get; set; }

    [Required]
    [MaxLength(256)]
    public string TaskName { get; set; } = string.Empty;

    [MaxLength(64)]
    public string Category { get; set; } = "Profile";

    public bool IsMandatory { get; set; } = true;
    public bool IsCompleted { get; set; } = false;

    [MaxLength(512)]
    public string? Notes { get; set; }
}
