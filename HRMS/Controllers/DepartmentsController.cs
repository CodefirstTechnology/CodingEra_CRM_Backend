using HRMS.Authorization;
using HRMS.Data;
using HRMS.DTOs;
using HRMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Controllers;

[Route("api/departments")]
[ApiController]
public class DepartmentsController : ControllerBase
{
    private readonly HRMSDbContext _context;

    public DepartmentsController(HRMSDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Departments.AsNoTracking();
        if (activeOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        return Ok(await query.OrderBy(x => x.Name).ToListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _context.Departments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [RequirePermission(HrmsPermissions.MasterDataManage)]
    public async Task<IActionResult> Create([FromBody] MasterDataUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var name = dto.Name.Trim();
        if (await _context.Departments.AnyAsync(x => x.Name == name, cancellationToken))
        {
            return Conflict("A department with this name already exists.");
        }

        var entity = new Department { Name = name, IsActive = dto.IsActive };
        _context.Departments.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(entity);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(HrmsPermissions.MasterDataManage)]
    public async Task<IActionResult> Update(int id, [FromBody] MasterDataUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var entity = await _context.Departments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return NotFound();
        }

        var name = dto.Name.Trim();
        if (await _context.Departments.AnyAsync(x => x.Name == name && x.Id != id, cancellationToken))
        {
            return Conflict("A department with this name already exists.");
        }

        entity.Name = name;
        entity.IsActive = dto.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(HrmsPermissions.MasterDataManage)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var entity = await _context.Departments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return NotFound();
        }

        _context.Departments.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
