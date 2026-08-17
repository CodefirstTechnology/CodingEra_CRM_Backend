using System.ComponentModel.DataAnnotations;

namespace HRMS.DTOs;

public class MasterDataUpsertDto
{
    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class LeaveTypeUpsertDto
{
    [Required]
    [MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(8)]
    public string Code { get; set; } = string.Empty;

    public int DefaultAllocatedDays { get; set; }

    public bool IsActive { get; set; } = true;
}

public class LeaveRejectionReasonUpsertDto
{
    [Required]
    [MaxLength(128)]
    public string Title { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }
}

public class AttendanceUpsertDto
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateOnly AttendanceDate { get; set; }

    public TimeOnly? CheckIn { get; set; }
    public TimeOnly? CheckOut { get; set; }
    public int? WorkingMinutes { get; set; }
    public int? OvertimeMinutes { get; set; }

    [Required]
    [MaxLength(32)]
    public string Status { get; set; } = "Present";
}

public class LeaveAllocationUpsertDto
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int LeaveTypeId { get; set; }

    [Required]
    public int Year { get; set; }

    [Required]
    public int AllocatedDays { get; set; }

    public int UsedDays { get; set; }
}

public class LeaveRequestUpsertDto
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int LeaveTypeId { get; set; }

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }

    [Required]
    public int TotalDays { get; set; }

    [Required]
    [MaxLength(512)]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(32)]
    public string Status { get; set; } = "Pending";
}

public class PayrollUpsertDto
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    [Range(1, 12)]
    public int PayMonth { get; set; }

    [Required]
    public int PayYear { get; set; }

    [Required]
    public decimal BasicSalary { get; set; }

    public decimal Allowances { get; set; }
    public decimal Deductions { get; set; }

    [MaxLength(32)]
    public string Status { get; set; } = "Pending";
}

public class EmployeeDocumentUpsertDto
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int DocumentCategoryId { get; set; }

    [Required]
    [MaxLength(256)]
    public string DocumentName { get; set; } = string.Empty;

    [Required]
    [MaxLength(512)]
    public string FilePath { get; set; } = string.Empty;
}

public class PerformanceReviewUpsertDto
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    [MaxLength(32)]
    public string ReviewPeriod { get; set; } = string.Empty;

    [Required]
    [MaxLength(1024)]
    public string KeyAchievements { get; set; } = string.Empty;

    [Required]
    [Range(0, 5)]
    public decimal ManagerRating { get; set; }

    [MaxLength(32)]
    public string Status { get; set; } = "Pending";
}

public class LeaveStatusUpdateDto
{
    [Required]
    [MaxLength(32)]
    public string Status { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Remarks { get; set; }
}

public class PayrollProcessDto
{
    [Required]
    [Range(1, 12)]
    public int PayMonth { get; set; }

    [Required]
    public int PayYear { get; set; }
}

public class PayrollBulkStatusDto
{
    [Required]
    [Range(1, 12)]
    public int PayMonth { get; set; }

    [Required]
    public int PayYear { get; set; }

    [Required]
    [MaxLength(32)]
    public string Status { get; set; } = string.Empty;
}
