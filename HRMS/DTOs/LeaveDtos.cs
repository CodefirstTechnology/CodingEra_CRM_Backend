using System.ComponentModel.DataAnnotations;

namespace HRMS.DTOs;

public class LeaveApplyDto
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
    [Range(1, 365)]
    public int TotalDays { get; set; }

    [Required]
    [MaxLength(512)]
    public string Reason { get; set; } = string.Empty;
}

public class LeaveActionDto
{
    [MaxLength(512)]
    public string? Remarks { get; set; }
}

public class LeaveBalanceItemDto
{
    public int LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public string LeaveTypeCode { get; set; } = string.Empty;
    public int AllocatedDays { get; set; }
    public int UsedDays { get; set; }
    public int PendingDays { get; set; }
    public int AvailableDays { get; set; }
    public int Year { get; set; }
}

public class LeaveRequestResponseDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string? EmployeeCode { get; set; }
    public string? Department { get; set; }
    public int LeaveTypeId { get; set; }
    public string? LeaveTypeName { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int TotalDays { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? AttachmentPath { get; set; }
    public string? AttachmentFileName { get; set; }
    public string? ApprovalRemarks { get; set; }
    public int? ApprovedByUserId { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class LeaveNotificationDto
{
    public int Id { get; set; }
    public int? LeaveRequestId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
