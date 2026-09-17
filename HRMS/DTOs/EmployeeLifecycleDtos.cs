using System.ComponentModel.DataAnnotations;

namespace HRMS.DTOs;

public class EmployeeTransferDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;

    public int? CurrentBranchId { get; set; }
    public string? CurrentBranchName { get; set; }
    public int? NewBranchId { get; set; }
    public string? NewBranchName { get; set; }

    public int? CurrentDepartmentId { get; set; }
    public string? CurrentDepartmentName { get; set; }
    public int? NewDepartmentId { get; set; }
    public string? NewDepartmentName { get; set; }

    public int? CurrentDesignationId { get; set; }
    public string? CurrentDesignationName { get; set; }
    public int? NewDesignationId { get; set; }
    public string? NewDesignationName { get; set; }

    public int? CurrentReportingManagerId { get; set; }
    public string? CurrentReportingManagerName { get; set; }
    public int? NewReportingManagerId { get; set; }
    public string? NewReportingManagerName { get; set; }

    public int? CurrentCostCenterId { get; set; }
    public string? CurrentCostCenterName { get; set; }
    public int? NewCostCenterId { get; set; }
    public string? NewCostCenterName { get; set; }

    public DateOnly EffectiveDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public int? RequestedByUserId { get; set; }
    public int? ApprovedByUserId { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
}

public class EmployeeTransferCreateDto
{
    [Required]
    public int EmployeeId { get; set; }

    public int? NewBranchId { get; set; }
    public int? NewDepartmentId { get; set; }
    public int? NewDesignationId { get; set; }
    public int? NewReportingManagerId { get; set; }
    public int? NewCostCenterId { get; set; }

    [Required]
    public DateOnly EffectiveDate { get; set; }

    [Required]
    [MaxLength(512)]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Remarks { get; set; }
}

public class EmployeePromotionDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;

    public int? CurrentDesignationId { get; set; }
    public string? CurrentDesignationName { get; set; }
    public int? NewDesignationId { get; set; }
    public string? NewDesignationName { get; set; }

    public int? CurrentGradeId { get; set; }
    public string? CurrentGradeName { get; set; }
    public int? NewGradeId { get; set; }
    public string? NewGradeName { get; set; }

    public int? CurrentDepartmentId { get; set; }
    public string? CurrentDepartmentName { get; set; }
    public int? NewDepartmentId { get; set; }
    public string? NewDepartmentName { get; set; }

    public decimal? CurrentSalary { get; set; }
    public decimal? NewSalary { get; set; }
    public string? SalaryRevisionReference { get; set; }

    public DateOnly EffectiveDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public int? RequestedByUserId { get; set; }
    public int? ApprovedByUserId { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
}

public class EmployeePromotionCreateDto
{
    [Required]
    public int EmployeeId { get; set; }

    public int? NewDesignationId { get; set; }
    public int? NewGradeId { get; set; }
    public int? NewDepartmentId { get; set; }
    public decimal? NewSalary { get; set; }
    public string? SalaryRevisionReference { get; set; }

    [Required]
    public DateOnly EffectiveDate { get; set; }

    [Required]
    [MaxLength(512)]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Remarks { get; set; }
}

public class SalaryRevisionRequestDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;

    public decimal CurrentSalary { get; set; }
    public decimal NewSalary { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public string RevisionType { get; set; } = "Annual Increment";
    public string Reason { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public int? RequestedByUserId { get; set; }
    public int? ApprovedByUserId { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime CreatedAt { get; set; }
}

public class SalaryRevisionCreateDto
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public decimal NewSalary { get; set; }

    [Required]
    public DateOnly EffectiveDate { get; set; }

    [MaxLength(64)]
    public string RevisionType { get; set; } = "Annual Increment"; // Annual Increment, Promotion, Market Adjustment, Correction, Other

    [Required]
    [MaxLength(512)]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Remarks { get; set; }
}

public class EmployeeExitDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string DesignationName { get; set; } = string.Empty;

    public string ExitType { get; set; } = "RESIGNATION";
    public DateOnly ResignationDate { get; set; }
    public int NoticePeriodDays { get; set; } = 30;
    public DateOnly LastWorkingDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public string Status { get; set; } = "Initiated";

    public string AssetClearanceStatus { get; set; } = "Pending";
    public string LeaveSettlementStatus { get; set; } = "Pending";
    public string PayrollSettlementStatus { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; }
}

public class EmployeeExitCreateDto
{
    [Required]
    public int EmployeeId { get; set; }

    [MaxLength(64)]
    public string ExitType { get; set; } = "RESIGNATION";

    [Required]
    public DateOnly ResignationDate { get; set; }

    public int NoticePeriodDays { get; set; } = 30;

    [Required]
    public DateOnly LastWorkingDate { get; set; }

    [Required]
    [MaxLength(512)]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Remarks { get; set; }
}

public class EmployeeExitClearanceUpdateDto
{
    [MaxLength(32)]
    public string? AssetClearanceStatus { get; set; }

    [MaxLength(32)]
    public string? LeaveSettlementStatus { get; set; }

    [MaxLength(32)]
    public string? PayrollSettlementStatus { get; set; }

    [MaxLength(32)]
    public string? Status { get; set; }
}

public class EmployeeLifecycleHistoryDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Reason { get; set; }
    public int? ChangedByUserId { get; set; }
    public string? ChangedByName { get; set; }
    public DateTime CreatedAt { get; set; }
}
