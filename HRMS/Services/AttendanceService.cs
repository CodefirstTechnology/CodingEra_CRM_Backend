using HRMS.Authorization;
using HRMS.Data;
using HRMS.DTOs;
using HRMS.Interfaces;
using HRMS.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace HRMS.Services;

public class AttendanceService : IAttendanceService
{
    private static readonly TimeOnly LateThreshold = new(9, 15);
    private static readonly TimeOnly StandardEndTime = new(18, 0);
    private const int StandardWorkMinutes = 480;
    private const int HalfDayMinutes = 240;

    private readonly HRMSDbContext _context;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(
        HRMSDbContext context,
        ICurrentUserAccessor currentUser,
        ILogger<AttendanceService> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<(AttendanceResponseDto? Result, string? Error, int StatusCode)> ClockInAsync(
        AttendanceClockDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.HasPermission(HrmsPermissions.AttendanceMarkOwn) && !_currentUser.IsAdmin)
        {
            return (null, "You do not have permission to mark attendance.", StatusCodes.Status403Forbidden);
        }

        var employeeId = _currentUser.EmployeeId;
        if (!employeeId.HasValue)
        {
            return (null, "Employee profile is not linked to your account.", StatusCodes.Status400BadRequest);
        }

        var today = AttendanceTimeHelper.TodayLocal();
        var existing = await _context.AttendanceRecords
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId.Value && x.AttendanceDate == today, cancellationToken);

        if (existing?.CheckIn != null)
        {
            return (null, "You have already clocked in today.", StatusCodes.Status409Conflict);
        }

        var serverUtc = DateTime.UtcNow;
        var deviceUtc = dto.DeviceTime?.ToUniversalTime();
        var localNow = AttendanceTimeHelper.ResolveClockInstant(deviceUtc, serverUtc);
        var checkInTime = TimeOnly.FromDateTime(localNow);
        var isLate = checkInTime > LateThreshold;
        var status = isLate ? AttendanceStatus.Late : AttendanceStatus.Present;

        if (existing == null)
        {
            existing = new AttendanceRecord
            {
                EmployeeId = employeeId.Value,
                AttendanceDate = today,
                CreatedAt = serverUtc
            };
            _context.AttendanceRecords.Add(existing);
        }

        existing.CheckIn = checkInTime;
        existing.ClockInDeviceAt = deviceUtc ?? serverUtc;
        existing.ClockInServerAt = serverUtc;
        existing.Status = status;
        existing.IsLate = isLate;
        existing.UpdatedAt = serverUtc;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Employee {EmployeeId} clocked in at {CheckIn}", employeeId.Value, checkInTime);

        return (await LoadResponseAsync(existing.Id, cancellationToken), null, StatusCodes.Status200OK);
    }

    public async Task<(AttendanceResponseDto? Result, string? Error, int StatusCode)> ClockOutAsync(
        AttendanceClockDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.HasPermission(HrmsPermissions.AttendanceMarkOwn) && !_currentUser.IsAdmin)
        {
            return (null, "You do not have permission to mark attendance.", StatusCodes.Status403Forbidden);
        }

        var employeeId = _currentUser.EmployeeId;
        if (!employeeId.HasValue)
        {
            return (null, "Employee profile is not linked to your account.", StatusCodes.Status400BadRequest);
        }

        var today = AttendanceTimeHelper.TodayLocal();
        var existing = await _context.AttendanceRecords
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId.Value && x.AttendanceDate == today, cancellationToken);

        if (existing == null || existing.CheckIn == null)
        {
            return (null, "You must clock in before clocking out.", StatusCodes.Status400BadRequest);
        }

        if (existing.CheckOut != null)
        {
            return (null, "You have already clocked out today.", StatusCodes.Status409Conflict);
        }

        var serverUtc = DateTime.UtcNow;
        var deviceUtc = dto.DeviceTime?.ToUniversalTime();
        var localNow = AttendanceTimeHelper.ResolveClockInstant(deviceUtc, serverUtc);
        var checkOutTime = TimeOnly.FromDateTime(localNow);
        var breakMinutes = dto.BreakDurationMinutes ?? existing.BreakDurationMinutes ?? 0;

        existing.CheckOut = checkOutTime;
        existing.ClockOutDeviceAt = deviceUtc ?? serverUtc;
        existing.ClockOutServerAt = serverUtc;
        existing.BreakDurationMinutes = breakMinutes;
        existing.UpdatedAt = serverUtc;

        ApplyWorkingMetrics(existing);

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Employee {EmployeeId} clocked out at {CheckOut}", employeeId.Value, checkOutTime);

        return (await LoadResponseAsync(existing.Id, cancellationToken), null, StatusCodes.Status200OK);
    }

    public async Task<(AttendanceResponseDto? Result, string? Error, int StatusCode)> GetTodayAsync(
        int? employeeId,
        CancellationToken cancellationToken = default)
    {
        var targetEmployeeId = ResolveEmployeeId(employeeId);
        if (!targetEmployeeId.HasValue)
        {
            return (null, "Employee not found.", StatusCodes.Status400BadRequest);
        }

        if (!CanAccessEmployee(targetEmployeeId.Value))
        {
            return (null, "You do not have permission to view this attendance.", StatusCodes.Status403Forbidden);
        }

        var today = AttendanceTimeHelper.TodayLocal();
        var record = await BuildQuery()
            .FirstOrDefaultAsync(x => x.EmployeeId == targetEmployeeId.Value && x.AttendanceDate == today, cancellationToken);

        return (record == null ? null : MapToResponse(record), null, StatusCodes.Status200OK);
    }

    public async Task<IReadOnlyList<AttendanceResponseDto>> GetRecordsAsync(
        AttendanceQueryDto query,
        CancellationToken cancellationToken = default)
    {
        if (!CanViewRecords())
        {
            return Array.Empty<AttendanceResponseDto>();
        }

        var records = await ApplyFilters(BuildQuery(), query)
            .OrderByDescending(x => x.AttendanceDate)
            .ThenBy(x => x.Employee!.FullName)
            .ToListAsync(cancellationToken);

        return records.Select(MapToResponse).ToList();
    }

    public async Task<AttendanceResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var record = await BuildQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (record == null || !CanAccessEmployee(record.EmployeeId))
        {
            return null;
        }

        return MapToResponse(record);
    }

    public async Task<AttendanceSummaryDto> GetSummaryAsync(
        AttendanceQueryDto query,
        CancellationToken cancellationToken = default)
    {
        if (!CanViewRecords())
        {
            return new AttendanceSummaryDto();
        }

        var records = await ApplyFilters(BuildQuery(), query).ToListAsync(cancellationToken);
        var totalEmployees = _currentUser.IsAdmin
            ? await _context.Employees.CountAsync(cancellationToken)
            : 1;

        return new AttendanceSummaryDto
        {
            Date = query.Date ?? query.FromDate,
            TotalEmployees = totalEmployees,
            TotalRecords = records.Count,
            Present = records.Count(x => x.Status is AttendanceStatus.Present or AttendanceStatus.Late),
            Absent = records.Count(x => x.Status == AttendanceStatus.Absent),
            Late = records.Count(x => x.Status == AttendanceStatus.Late || x.IsLate),
            OnLeave = records.Count(x => x.Status is AttendanceStatus.OnLeave or AttendanceStatus.Leave),
            HalfDay = records.Count(x => x.Status == AttendanceStatus.HalfDay)
        };
    }

    public async Task<byte[]> ExportCsvAsync(
        AttendanceQueryDto query,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAdmin || !_currentUser.HasPermission(HrmsPermissions.AttendanceViewAll))
        {
            return Encoding.UTF8.GetBytes("Access denied");
        }

        var records = await GetRecordsAsync(query, cancellationToken);
        var sb = new StringBuilder();
        sb.AppendLine("Employee Code,Employee Name,Department,Branch,Date,Check In,Check Out,Working Hours,Overtime (min),Break (min),Status,Late,Early Leave");

        foreach (var r in records)
        {
            sb.AppendLine(string.Join(',',
                Csv(r.EmployeeCode),
                Csv(r.EmployeeName),
                Csv(r.Department),
                Csv(r.Branch),
                r.AttendanceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                r.CheckIn ?? "",
                r.CheckOut ?? "",
                FormatHours(r.WorkingMinutes),
                r.OvertimeMinutes?.ToString(CultureInfo.InvariantCulture) ?? "0",
                r.BreakDurationMinutes?.ToString(CultureInfo.InvariantCulture) ?? "0",
                Csv(r.Status),
                r.IsLate ? "Yes" : "No",
                r.IsEarlyLeave ? "Yes" : "No"));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private IQueryable<AttendanceRecord> BuildQuery() =>
        _context.AttendanceRecords
            .AsNoTracking()
            .Include(x => x.Employee)!
            .ThenInclude(e => e!.Department)
            .Include(x => x.Employee)!
            .ThenInclude(e => e!.Branch)
            .Include(x => x.Employee)!
            .ThenInclude(e => e!.Designation);

    private IQueryable<AttendanceRecord> ApplyFilters(IQueryable<AttendanceRecord> query, AttendanceQueryDto filter)
    {
        if (!_currentUser.IsAdmin)
        {
            if (_currentUser.EmployeeId.HasValue)
            {
                query = query.Where(x => x.EmployeeId == _currentUser.EmployeeId.Value);
            }
            else
            {
                return query.Where(_ => false);
            }
        }
        else if (filter.EmployeeId.HasValue)
        {
            query = query.Where(x => x.EmployeeId == filter.EmployeeId.Value);
        }

        if (filter.Date.HasValue)
        {
            query = query.Where(x => x.AttendanceDate == filter.Date.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(x => x.AttendanceDate >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(x => x.AttendanceDate <= filter.ToDate.Value);
        }

        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(x => x.Employee!.DepartmentId == filter.DepartmentId.Value);
        }

        if (filter.BranchId.HasValue)
        {
            query = query.Where(x => x.Employee!.BranchId == filter.BranchId.Value);
        }

        if (filter.DesignationId.HasValue)
        {
            query = query.Where(x => x.Employee!.DesignationId == filter.DesignationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            query = query.Where(x => x.Status == filter.Status.Trim());
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                (x.Employee!.FullName != null && x.Employee.FullName.ToLower().Contains(term)) ||
                (x.Employee.EmployeeCode != null && x.Employee.EmployeeCode.ToLower().Contains(term)) ||
                x.EmployeeId.ToString(CultureInfo.InvariantCulture) == term);
        }

        return query;
    }

    private async Task<AttendanceResponseDto?> LoadResponseAsync(int id, CancellationToken cancellationToken)
    {
        var record = await BuildQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return record == null ? null : MapToResponse(record);
    }

    private static void ApplyWorkingMetrics(AttendanceRecord record)
    {
        if (record.CheckIn == null || record.CheckOut == null)
        {
            return;
        }

        var checkIn = record.CheckIn.Value;
        var checkOut = record.CheckOut.Value;
        var totalMinutes = (int)Math.Max(0, (checkOut.ToTimeSpan() - checkIn.ToTimeSpan()).TotalMinutes);
        var breakMinutes = record.BreakDurationMinutes ?? 0;
        var workingMinutes = Math.Max(0, totalMinutes - breakMinutes);

        record.WorkingMinutes = workingMinutes;
        record.OvertimeMinutes = Math.Max(0, workingMinutes - StandardWorkMinutes);
        record.IsEarlyLeave = checkOut < StandardEndTime && workingMinutes < StandardWorkMinutes;

        if (workingMinutes > 0 && workingMinutes < HalfDayMinutes)
        {
            record.Status = AttendanceStatus.HalfDay;
        }
        else if (record.IsLate)
        {
            record.Status = AttendanceStatus.Late;
        }
        else
        {
            record.Status = AttendanceStatus.Present;
        }
    }

    private static AttendanceResponseDto MapToResponse(AttendanceRecord item) =>
        new()
        {
            Id = item.Id,
            EmployeeId = item.EmployeeId,
            EmployeeName = item.Employee?.FullName,
            EmployeeCode = item.Employee?.EmployeeCode,
            Department = item.Employee?.Department?.Name,
            Branch = item.Employee?.Branch?.Name,
            Designation = item.Employee?.Designation?.Name,
            AttendanceDate = item.AttendanceDate,
            CheckIn = AttendanceTimeHelper.FormatDisplay(item.ClockInServerAt, item.CheckIn),
            CheckOut = AttendanceTimeHelper.FormatDisplay(item.ClockOutServerAt, item.CheckOut),
            WorkingMinutes = item.WorkingMinutes,
            BreakDurationMinutes = item.BreakDurationMinutes,
            OvertimeMinutes = item.OvertimeMinutes,
            Status = item.Status,
            IsLate = item.IsLate,
            IsEarlyLeave = item.IsEarlyLeave,
            ClockInDeviceAt = item.ClockInDeviceAt,
            ClockInServerAt = item.ClockInServerAt,
            ClockOutDeviceAt = item.ClockOutDeviceAt,
            ClockOutServerAt = item.ClockOutServerAt,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };

    private int? ResolveEmployeeId(int? employeeId)
    {
        if (_currentUser.IsAdmin)
        {
            return employeeId ?? _currentUser.EmployeeId;
        }

        return _currentUser.EmployeeId;
    }

    private bool CanAccessEmployee(int employeeId) =>
        _currentUser.CanAccessEmployee(employeeId);

    private bool CanViewRecords() =>
        _currentUser.IsAdmin
            ? _currentUser.HasPermission(HrmsPermissions.AttendanceViewAll)
            : _currentUser.HasPermission(HrmsPermissions.AttendanceViewOwn);

    private static string Csv(string? value)
    {
        var safe = value ?? string.Empty;
        return safe.Contains('"') || safe.Contains(',')
            ? $"\"{safe.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : safe;
    }

    private static string FormatHours(int? minutes) =>
        minutes.HasValue ? (minutes.Value / 60.0).ToString("0.##", CultureInfo.InvariantCulture) : "0";
}
