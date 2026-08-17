using HRMS.Authorization;
using HRMS.Data;
using HRMS.DTOs;
using HRMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Controllers;

[Route("api/leave-allocations")]
[ApiController]
public class LeaveAllocationsController : ControllerBase
{
    private readonly HRMSDbContext _context;
    private readonly ICurrentUserAccessor _currentUser;

    public LeaveAllocationsController(HRMSDbContext context, ICurrentUserAccessor currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? employeeId,
        [FromQuery] int? year,
        CancellationToken cancellationToken = default)
    {
        var query = _context.LeaveAllocations
            .AsNoTracking()
            .Include(x => x.Employee)
            .Include(x => x.LeaveType)
            .AsQueryable();

        if (_currentUser.IsAdmin)
        {
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

        if (year.HasValue)
        {
            query = query.Where(x => x.Year == year.Value);
        }

        return Ok(await query.OrderBy(x => x.Employee!.FullName).ToListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _context.LeaveAllocations
            .AsNoTracking()
            .Include(x => x.Employee)
            .Include(x => x.LeaveType)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (item == null)
        {
            return NotFound();
        }

        var accessError = this.EnsureEmployeeAccess(_currentUser, item.EmployeeId);
        return accessError ?? Ok(item);
    }

    [HttpPost]
    [RequirePermission(HrmsPermissions.LeaveManage)]
    public async Task<IActionResult> Create([FromBody] LeaveAllocationUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!await _context.Employees.AnyAsync(x => x.Id == dto.EmployeeId, cancellationToken))
        {
            return BadRequest("Employee not found.");
        }

        if (!await _context.LeaveTypes.AnyAsync(x => x.Id == dto.LeaveTypeId, cancellationToken))
        {
            return BadRequest("Leave type not found.");
        }

        if (await _context.LeaveAllocations.AnyAsync(
                x => x.EmployeeId == dto.EmployeeId &&
                     x.LeaveTypeId == dto.LeaveTypeId &&
                     x.Year == dto.Year,
                cancellationToken))
        {
            return Conflict("Leave allocation already exists for this employee, leave type, and year.");
        }

        var entity = new LeaveAllocation
        {
            EmployeeId = dto.EmployeeId,
            LeaveTypeId = dto.LeaveTypeId,
            Year = dto.Year,
            AllocatedDays = dto.AllocatedDays,
            UsedDays = dto.UsedDays
        };

        _context.LeaveAllocations.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(entity);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(HrmsPermissions.LeaveManage)]
    public async Task<IActionResult> Update(int id, [FromBody] LeaveAllocationUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var entity = await _context.LeaveAllocations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return NotFound();
        }

        if (await _context.LeaveAllocations.AnyAsync(
                x => x.EmployeeId == dto.EmployeeId &&
                     x.LeaveTypeId == dto.LeaveTypeId &&
                     x.Year == dto.Year &&
                     x.Id != id,
                cancellationToken))
        {
            return Conflict("Leave allocation already exists for this employee, leave type, and year.");
        }

        entity.EmployeeId = dto.EmployeeId;
        entity.LeaveTypeId = dto.LeaveTypeId;
        entity.Year = dto.Year;
        entity.AllocatedDays = dto.AllocatedDays;
        entity.UsedDays = dto.UsedDays;
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(HrmsPermissions.LeaveManage)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var entity = await _context.LeaveAllocations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return NotFound();
        }

        _context.LeaveAllocations.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
