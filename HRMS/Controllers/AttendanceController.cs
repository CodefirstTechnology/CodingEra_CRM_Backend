using HRMS.Authorization;
using HRMS.Data;
using HRMS.DTOs;
using HRMS.Interfaces;
using HRMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Controllers;

[Route("api/attendance")]
[ApiController]
public class AttendanceController : ControllerBase
{
    private readonly HRMSDbContext _context;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(
        HRMSDbContext context,
        ICurrentUserAccessor currentUser,
        IAttendanceService attendanceService)
    {
        _context = context;
        _currentUser = currentUser;
        _attendanceService = attendanceService;
    }

    [HttpPost("clock-in")]
    public async Task<IActionResult> ClockIn([FromBody] AttendanceClockDto? dto, CancellationToken cancellationToken)
    {
        var (result, error, statusCode) = await _attendanceService.ClockInAsync(dto ?? new AttendanceClockDto(), cancellationToken);
        if (error != null)
        {
            return StatusCode(statusCode, new { message = error });
        }

        return Ok(result);
    }

    [HttpPost("clock-out")]
    public async Task<IActionResult> ClockOut([FromBody] AttendanceClockDto? dto, CancellationToken cancellationToken)
    {
        var (result, error, statusCode) = await _attendanceService.ClockOutAsync(dto ?? new AttendanceClockDto(), cancellationToken);
        if (error != null)
        {
            return StatusCode(statusCode, new { message = error });
        }

        return Ok(result);
    }

    [HttpGet("today")]
    public async Task<IActionResult> GetToday([FromQuery] int? employeeId, CancellationToken cancellationToken)
    {
        var (result, error, statusCode) = await _attendanceService.GetTodayAsync(employeeId, cancellationToken);
        if (error != null)
        {
            return StatusCode(statusCode, new { message = error });
        }

        return Ok(result);
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] int? employeeId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var records = await _attendanceService.GetRecordsAsync(new AttendanceQueryDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            EmployeeId = employeeId,
            Status = status
        }, cancellationToken);

        return Ok(records);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateOnly? date,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] int? departmentId,
        [FromQuery] int? branchId,
        [FromQuery] int? designationId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var summary = await _attendanceService.GetSummaryAsync(new AttendanceQueryDto
        {
            Date = date,
            FromDate = fromDate,
            ToDate = toDate,
            DepartmentId = departmentId,
            BranchId = branchId,
            DesignationId = designationId,
            Status = status
        }, cancellationToken);

        return Ok(summary);
    }

    [HttpGet("export")]
    [RequirePermission(HrmsPermissions.AttendanceViewAll)]
    public async Task<IActionResult> Export(
        [FromQuery] DateOnly? date,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] int? employeeId,
        [FromQuery] int? departmentId,
        [FromQuery] int? branchId,
        [FromQuery] int? designationId,
        [FromQuery] string? status,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var csv = await _attendanceService.ExportCsvAsync(new AttendanceQueryDto
        {
            Date = date,
            FromDate = fromDate,
            ToDate = toDate,
            EmployeeId = employeeId,
            DepartmentId = departmentId,
            BranchId = branchId,
            DesignationId = designationId,
            Status = status,
            Search = search
        }, cancellationToken);

        var fileName = $"attendance-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        return File(csv, "text/csv", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateOnly? date,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] int? employeeId,
        [FromQuery] int? departmentId,
        [FromQuery] int? branchId,
        [FromQuery] int? designationId,
        [FromQuery] string? status,
        [FromQuery] string? search,
        CancellationToken cancellationToken = default)
    {
        if (fromDate.HasValue || toDate.HasValue || !string.IsNullOrWhiteSpace(search) ||
            branchId.HasValue || designationId.HasValue || employeeId.HasValue)
        {
            var records = await _attendanceService.GetRecordsAsync(new AttendanceQueryDto
            {
                Date = date,
                FromDate = fromDate,
                ToDate = toDate,
                EmployeeId = employeeId,
                DepartmentId = departmentId,
                BranchId = branchId,
                DesignationId = designationId,
                Status = status,
                Search = search
            }, cancellationToken);

            return Ok(records);
        }

        var query = _context.AttendanceRecords
            .AsNoTracking()
            .Include(x => x.Employee)!
            .ThenInclude(e => e!.Department)
            .AsQueryable();

        if (!_currentUser.IsAdmin)
        {
            if (!_currentUser.EmployeeId.HasValue)
            {
                return Forbid();
            }

            query = query.Where(x => x.EmployeeId == _currentUser.EmployeeId.Value);
        }

        if (date.HasValue)
        {
            query = query.Where(x => x.AttendanceDate == date.Value);
        }

        if (departmentId.HasValue)
        {
            query = query.Where(x => x.Employee!.DepartmentId == departmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        var legacyRecords = await query
            .OrderByDescending(x => x.AttendanceDate)
            .ThenBy(x => x.Employee!.FullName)
            .ToListAsync(cancellationToken);

        var dtos = legacyRecords.Select(item => new AttendanceResponseDto
        {
            Id = item.Id,
            EmployeeId = item.EmployeeId,
            EmployeeName = item.Employee?.FullName,
            EmployeeCode = item.Employee?.EmployeeCode,
            Department = item.Employee?.Department?.Name,
            Branch = item.Employee?.Branch?.Name,
            Designation = item.Employee?.Designation?.Name,
            AttendanceDate = item.AttendanceDate,
            CheckIn = item.CheckIn?.ToString("h:mm tt", System.Globalization.CultureInfo.InvariantCulture),
            CheckOut = item.CheckOut?.ToString("h:mm tt", System.Globalization.CultureInfo.InvariantCulture),
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
        }).ToList();

        return Ok(dtos);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var record = await _attendanceService.GetByIdAsync(id, cancellationToken);
        if (record == null)
        {
            var legacy = await _context.AttendanceRecords
                .AsNoTracking()
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (legacy == null)
            {
                return NotFound();
            }

            var accessError = this.EnsureEmployeeAccess(_currentUser, legacy.EmployeeId);
            if (accessError != null)
            {
                return accessError;
            }

            var response = new AttendanceResponseDto
            {
                Id = legacy.Id,
                EmployeeId = legacy.EmployeeId,
                EmployeeName = legacy.Employee?.FullName,
                EmployeeCode = legacy.Employee?.EmployeeCode,
                Department = legacy.Employee?.Department?.Name,
                Branch = legacy.Employee?.Branch?.Name,
                Designation = legacy.Employee?.Designation?.Name,
                AttendanceDate = legacy.AttendanceDate,
                CheckIn = legacy.CheckIn?.ToString("h:mm tt", System.Globalization.CultureInfo.InvariantCulture),
                CheckOut = legacy.CheckOut?.ToString("h:mm tt", System.Globalization.CultureInfo.InvariantCulture),
                WorkingMinutes = legacy.WorkingMinutes,
                BreakDurationMinutes = legacy.BreakDurationMinutes,
                OvertimeMinutes = legacy.OvertimeMinutes,
                Status = legacy.Status,
                IsLate = legacy.IsLate,
                IsEarlyLeave = legacy.IsEarlyLeave,
                ClockInDeviceAt = legacy.ClockInDeviceAt,
                ClockInServerAt = legacy.ClockInServerAt,
                ClockOutDeviceAt = legacy.ClockOutDeviceAt,
                ClockOutServerAt = legacy.ClockOutServerAt,
                CreatedAt = legacy.CreatedAt,
                UpdatedAt = legacy.UpdatedAt
            };

            return Ok(response);
        }

        return Ok(record);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AttendanceUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (_currentUser.IsAdmin)
        {
            var permissionError = this.EnsurePermission(_currentUser, HrmsPermissions.AttendanceManage);
            if (permissionError != null)
            {
                return permissionError;
            }
        }
        else
        {
            var accessError = this.EnsureEmployeeAccess(_currentUser, dto.EmployeeId);
            if (accessError != null)
            {
                return accessError;
            }

            var permissionError = this.EnsurePermission(_currentUser, HrmsPermissions.AttendanceMarkOwn);
            if (permissionError != null)
            {
                return permissionError;
            }
        }

        if (!await _context.Employees.AnyAsync(x => x.Id == dto.EmployeeId, cancellationToken))
        {
            return BadRequest("Employee not found.");
        }

        if (await _context.AttendanceRecords.AnyAsync(
                x => x.EmployeeId == dto.EmployeeId && x.AttendanceDate == dto.AttendanceDate,
                cancellationToken))
        {
            return Conflict("Attendance already exists for this employee on the selected date.");
        }

        var entity = MapToEntity(dto);
        _context.AttendanceRecords.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(entity);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] AttendanceUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var entity = await _context.AttendanceRecords.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return NotFound();
        }

        if (_currentUser.IsAdmin)
        {
            var permissionError = this.EnsurePermission(_currentUser, HrmsPermissions.AttendanceManage);
            if (permissionError != null)
            {
                return permissionError;
            }
        }
        else
        {
            var accessError = this.EnsureEmployeeAccess(_currentUser, entity.EmployeeId);
            if (accessError != null)
            {
                return accessError;
            }

            var permissionError = this.EnsurePermission(_currentUser, HrmsPermissions.AttendanceMarkOwn);
            if (permissionError != null)
            {
                return permissionError;
            }

            if (dto.EmployeeId != entity.EmployeeId)
            {
                return Forbid();
            }
        }

        if (await _context.AttendanceRecords.AnyAsync(
                x => x.EmployeeId == dto.EmployeeId &&
                     x.AttendanceDate == dto.AttendanceDate &&
                     x.Id != id,
                cancellationToken))
        {
            return Conflict("Attendance already exists for this employee on the selected date.");
        }

        ApplyDto(entity, dto);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(HrmsPermissions.AttendanceManage)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var entity = await _context.AttendanceRecords.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return NotFound();
        }

        _context.AttendanceRecords.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static AttendanceRecord MapToEntity(AttendanceUpsertDto dto)
    {
        var entity = new AttendanceRecord();
        ApplyDto(entity, dto);
        return entity;
    }

    private static void ApplyDto(AttendanceRecord entity, AttendanceUpsertDto dto)
    {
        entity.EmployeeId = dto.EmployeeId;
        entity.AttendanceDate = dto.AttendanceDate;
        entity.CheckIn = dto.CheckIn;
        entity.CheckOut = dto.CheckOut;
        entity.WorkingMinutes = dto.WorkingMinutes;
        entity.OvertimeMinutes = dto.OvertimeMinutes;
        entity.Status = dto.Status.Trim();
        entity.UpdatedAt = DateTime.UtcNow;
        if (entity.CreatedAt == default)
        {
            entity.CreatedAt = DateTime.UtcNow;
        }
    }
}
