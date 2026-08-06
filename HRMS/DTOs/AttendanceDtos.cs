using System.ComponentModel.DataAnnotations;

using HRMS.Models;

namespace HRMS.DTOs;

public class AttendanceClockDto
{
    public DateTime? DeviceTime { get; set; }
    public int? BreakDurationMinutes { get; set; }
}

public class AttendanceQueryDto
{
    public DateOnly? Date { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public int? EmployeeId { get; set; }
    public int? DepartmentId { get; set; }
    public int? BranchId { get; set; }
    public int? DesignationId { get; set; }
    public string? Status { get; set; }
    public string? Search { get; set; }
}

public class AttendanceResponseDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string? EmployeeCode { get; set; }
    public string? Department { get; set; }
    public string? Branch { get; set; }
    public string? Designation { get; set; }
    public DateOnly AttendanceDate { get; set; }
    public string? CheckIn { get; set; }
    public string? CheckOut { get; set; }
    public int? WorkingMinutes { get; set; }
    public int? BreakDurationMinutes { get; set; }
    public int? OvertimeMinutes { get; set; }
    public string Status { get; set; } = AttendanceStatus.Present;
    public bool IsLate { get; set; }
    public bool IsEarlyLeave { get; set; }
    public DateTime? ClockInDeviceAt { get; set; }
    public DateTime? ClockInServerAt { get; set; }
    public DateTime? ClockOutDeviceAt { get; set; }
    public DateTime? ClockOutServerAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AttendanceSummaryDto
{
    public int TotalEmployees { get; set; }
    public int TotalRecords { get; set; }
    public int Present { get; set; }
    public int Absent { get; set; }
    public int Late { get; set; }
    public int OnLeave { get; set; }
    public int HalfDay { get; set; }
    public DateOnly? Date { get; set; }
}
