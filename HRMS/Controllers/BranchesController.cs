using HRMS.Authorization;
using HRMS.Data;
using HRMS.DTOs;
using HRMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Controllers;

[Route("api/branches")]
[ApiController]
public class BranchesController : ControllerBase
{
    private readonly HRMSDbContext _context;

    public BranchesController(HRMSDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Branches.AsNoTracking();
        if (activeOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        return Ok(await query.OrderBy(x => x.Name).ToListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _context.Branches.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
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
        if (await _context.Branches.AnyAsync(x => x.Name == name, cancellationToken))
        {
            return Conflict("A branch with this name already exists.");
        }

        var entity = new Branch { Name = name, IsActive = dto.IsActive };
        _context.Branches.Add(entity);
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

        var entity = await _context.Branches.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return NotFound();
        }

        var name = dto.Name.Trim();
        if (await _context.Branches.AnyAsync(x => x.Name == name && x.Id != id, cancellationToken))
        {
            return Conflict("A branch with this name already exists.");
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
        var entity = await _context.Branches.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return NotFound();
        }

        _context.Branches.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
