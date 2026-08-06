using HRMS.Authorization;
using HRMS.Data;
using HRMS.DTOs;
using HRMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Controllers;

[Route("api/payroll")]
[ApiController]
public class PayrollController : ControllerBase
{
    private readonly HRMSDbContext _context;
    private readonly ICurrentUserAccessor _currentUser;

    public PayrollController(HRMSDbContext context, ICurrentUserAccessor currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? payMonth,
        [FromQuery] int? payYear,
        [FromQuery] int? employeeId,
        [FromQuery] string? status,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PayrollRecords
            .AsNoTracking()
            .Include(x => x.Employee)!
            .ThenInclude(e => e!.Designation)
            .AsQueryable();

        if (_currentUser.IsAdmin)
        {
            if (payMonth.HasValue)
            {
                query = query.Where(x => x.PayMonth == payMonth.Value);
            }

            if (payYear.HasValue)
            {
                query = query.Where(x => x.PayYear == payYear.Value);
            }

            if (employeeId.HasValue)
            {
                query = query.Where(x => x.EmployeeId == employeeId.Value);
            }
        }
        else
        {
            if (!_currentUser.EmployeeId.HasValue)
            {
                return Forbid();
            }

            query = query.Where(x => x.EmployeeId == _currentUser.EmployeeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        return Ok(await query.OrderByDescending(x => x.PayYear).ThenByDescending(x => x.PayMonth).ToListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _context.PayrollRecords
            .AsNoTracking()
            .Include(x => x.Employee)!
            .ThenInclude(e => e!.Designation)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (item == null)
        {
            return NotFound();
        }

        var accessError = this.EnsureEmployeeAccess(_currentUser, item.EmployeeId);
        return accessError ?? Ok(item);
    }

    [HttpPost]
    [RequirePermission(HrmsPermissions.PayrollProcess)]
    public async Task<IActionResult> Create([FromBody] PayrollUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!await _context.Employees.AnyAsync(x => x.Id == dto.EmployeeId, cancellationToken))
        {
            return BadRequest("Employee not found.");
        }

        if (await _context.PayrollRecords.AnyAsync(
                x => x.EmployeeId == dto.EmployeeId &&
                     x.PayMonth == dto.PayMonth &&
                     x.PayYear == dto.PayYear,
                cancellationToken))
        {
            return Conflict("Payroll record already exists for this employee and period.");
        }

        var entity = new PayrollRecord
        {
            EmployeeId = dto.EmployeeId,
            PayMonth = dto.PayMonth,
            PayYear = dto.PayYear,
            BasicSalary = dto.BasicSalary,
            Allowances = dto.Allowances,
            Deductions = dto.Deductions,
            NetSalary = dto.BasicSalary + dto.Allowances - dto.Deductions,
            Status = dto.Status.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.PayrollRecords.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(entity);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(HrmsPermissions.PayrollProcess)]
    public async Task<IActionResult> Update(int id, [FromBody] PayrollUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var entity = await _context.PayrollRecords.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return NotFound();
        }

        if (await _context.PayrollRecords.AnyAsync(
                x => x.EmployeeId == dto.EmployeeId &&
                     x.PayMonth == dto.PayMonth &&
                     x.PayYear == dto.PayYear &&
                     x.Id != id,
                cancellationToken))
        {
            return Conflict("Payroll record already exists for this employee and period.");
        }

        entity.EmployeeId = dto.EmployeeId;
        entity.PayMonth = dto.PayMonth;
        entity.PayYear = dto.PayYear;
        entity.BasicSalary = dto.BasicSalary;
        entity.Allowances = dto.Allowances;
        entity.Deductions = dto.Deductions;
        entity.NetSalary = dto.BasicSalary + dto.Allowances - dto.Deductions;
        entity.Status = dto.Status.Trim();
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(HrmsPermissions.PayrollProcess)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var entity = await _context.PayrollRecords.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return NotFound();
        }

        _context.PayrollRecords.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("process")]
    [RequirePermission(HrmsPermissions.PayrollProcess)]
    public async Task<IActionResult> ProcessPayroll([FromBody] PayrollProcessDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var employees = await _context.Employees
            .Include(x => x.Designation)
            .Where(x => x.Status == "Active")
            .ToListAsync(cancellationToken);

        var totalDays = DateTime.DaysInMonth(dto.PayYear, dto.PayMonth);
        var workingDays = GetWorkingDaysInMonth(dto.PayYear, dto.PayMonth);
        var start = new DateOnly(dto.PayYear, dto.PayMonth, 1);
        var end = new DateOnly(dto.PayYear, dto.PayMonth, totalDays);

        var attendances = await _context.AttendanceRecords
            .Where(x => x.AttendanceDate >= start && x.AttendanceDate <= end)
            .ToListAsync(cancellationToken);

        var attendanceGroups = attendances.GroupBy(x => x.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var processedRecords = new List<PayrollRecord>();

        foreach (var emp in employees)
        {
            // Determine basic salary based on designation name
            decimal basicSalary = 30000;
            var desName = emp.Designation?.Name ?? "";
            if (desName.Contains("Developer", StringComparison.OrdinalIgnoreCase) || desName.Contains("Engineer", StringComparison.OrdinalIgnoreCase))
            {
                basicSalary = 85000;
            }
            else if (desName.Contains("HR", StringComparison.OrdinalIgnoreCase) || desName.Contains("Lead", StringComparison.OrdinalIgnoreCase))
            {
                basicSalary = 60000;
            }
            else if (desName.Contains("Sales", StringComparison.OrdinalIgnoreCase) || desName.Contains("Executive", StringComparison.OrdinalIgnoreCase))
            {
                basicSalary = 40000;
            }

            int presentCount = 0;
            int absentCount = 0;
            int lateCount = 0;

            if (attendanceGroups.TryGetValue(emp.Id, out var empAttendances))
            {
                presentCount = empAttendances.Count(x => x.Status == AttendanceStatus.Present || x.Status == AttendanceStatus.Late || x.Status == AttendanceStatus.HalfDay);
                absentCount = empAttendances.Count(x => x.Status == AttendanceStatus.Absent);
                lateCount = empAttendances.Count(x => x.IsLate || x.Status == AttendanceStatus.Late);
            }

            decimal allowances = presentCount * 200;
            decimal lopDeductions = Math.Round(absentCount * (basicSalary / workingDays), 2);
            decimal lateDeductions = lateCount * 100; // ₹100 per late arrival
            decimal deductions = lopDeductions + lateDeductions;
            decimal netSalary = basicSalary + allowances - deductions;

            var existing = await _context.PayrollRecords
                .FirstOrDefaultAsync(x => x.EmployeeId == emp.Id && x.PayMonth == dto.PayMonth && x.PayYear == dto.PayYear, cancellationToken);

            if (existing != null)
            {
                if (existing.Status == "Pending" || existing.Status == "Draft")
                {
                    existing.BasicSalary = basicSalary;
                    existing.Allowances = allowances;
                    existing.Deductions = deductions;
                    existing.NetSalary = netSalary;
                    processedRecords.Add(existing);
                }
            }
            else
            {
                var newRecord = new PayrollRecord
                {
                    EmployeeId = emp.Id,
                    PayMonth = dto.PayMonth,
                    PayYear = dto.PayYear,
                    BasicSalary = basicSalary,
                    Allowances = allowances,
                    Deductions = deductions,
                    NetSalary = netSalary,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };
                _context.PayrollRecords.Add(newRecord);
                processedRecords.Add(newRecord);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Fetch fully populated objects including Employee/Designation info for response
        var resultIds = processedRecords.Select(x => x.Id).ToList();
        var resultList = await _context.PayrollRecords
            .AsNoTracking()
            .Include(x => x.Employee)!
            .ThenInclude(e => e!.Designation)
            .Where(x => resultIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        return Ok(resultList);
    }

    [HttpPost("bulk-status")]
    [RequirePermission(HrmsPermissions.PayrollProcess)]
    public async Task<IActionResult> BulkUpdateStatus([FromBody] PayrollBulkStatusDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var records = await _context.PayrollRecords
            .Where(x => x.PayMonth == dto.PayMonth && x.PayYear == dto.PayYear)
            .ToListAsync(cancellationToken);

        foreach (var record in records)
        {
            record.Status = dto.Status.Trim();
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = $"Successfully updated {records.Count} payroll records to '{dto.Status}' status." });
    }

    private int GetWorkingDaysInMonth(int year, int month)
    {
        int days = DateTime.DaysInMonth(year, month);
        int workDays = 0;
        for (int i = 1; i <= days; i++)
        {
            var date = new DateTime(year, month, i);
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
            {
                workDays++;
            }
        }
        return workDays;
    }
}

