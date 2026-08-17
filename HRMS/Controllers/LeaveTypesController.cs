using HRMS.Authorization;
using HRMS.Data;
using HRMS.DTOs;
using HRMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Controllers;

[Route("api/leave-types")]
[ApiController]
public class LeaveTypesController : ControllerBase
{
    private readonly HRMSDbContext _context;

    public LeaveTypesController(HRMSDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var query = _context.LeaveTypes.AsNoTracking();
        if (activeOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        return Ok(await query.OrderBy(x => x.Name).ToListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _context.LeaveTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [RequirePermission(HrmsPermissions.MasterDataManage)]
    public async Task<IActionResult> Create([FromBody] LeaveTypeUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var code = dto.Code.Trim().ToUpperInvariant();
        if (await _context.LeaveTypes.AnyAsync(x => x.Code == code, cancellationToken))
        {
            return Conflict("A leave type with this code already exists.");
        }

        var entity = new LeaveType
        {
            Name = dto.Name.Trim(),
            Code = code,
            DefaultAllocatedDays = dto.DefaultAllocatedDays,
            IsActive = dto.IsActive
        };

        _context.LeaveTypes.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(entity);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(HrmsPermissions.MasterDataManage)]
    public async Task<IActionResult> Update(int id, [FromBody] LeaveTypeUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var entity = await _context.LeaveTypes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return NotFound();
        }

        var code = dto.Code.Trim().ToUpperInvariant();
        if (await _context.LeaveTypes.AnyAsync(x => x.Code == code && x.Id != id, cancellationToken))
        {
            return Conflict("A leave type with this code already exists.");
        }

        entity.Name = dto.Name.Trim();
        entity.Code = code;
        entity.DefaultAllocatedDays = dto.DefaultAllocatedDays;
        entity.IsActive = dto.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(HrmsPermissions.MasterDataManage)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var entity = await _context.LeaveTypes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return NotFound();
        }

        _context.LeaveTypes.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
