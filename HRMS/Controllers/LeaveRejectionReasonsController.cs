using HRMS.Authorization;
using HRMS.Data;
using HRMS.DTOs;
using HRMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Controllers;

[Route("api/leave-rejection-reasons")]
[ApiController]
public class LeaveRejectionReasonsController : ControllerBase
{
    private readonly HRMSDbContext _context;

    public LeaveRejectionReasonsController(HRMSDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        var query = _context.LeaveRejectionReasons.AsNoTracking();
        if (activeOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        var items = await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Title)
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _context.LeaveRejectionReasons.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [RequirePermission(HrmsPermissions.MasterDataManage)]
    public async Task<IActionResult> Create([FromBody] LeaveRejectionReasonUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var title = dto.Title.Trim();
        if (await _context.LeaveRejectionReasons.AnyAsync(x => x.Title == title, cancellationToken))
        {
            return Conflict("A rejection reason with this title already exists.");
        }

        var entity = new LeaveRejectionReason
        {
            Title = title,
            IsActive = dto.IsActive,
            SortOrder = dto.SortOrder
        };

        _context.LeaveRejectionReasons.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(entity);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(HrmsPermissions.MasterDataManage)]
    public async Task<IActionResult> Update(int id, [FromBody] LeaveRejectionReasonUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var entity = await _context.LeaveRejectionReasons.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return NotFound();
        }

        var title = dto.Title.Trim();
        if (await _context.LeaveRejectionReasons.AnyAsync(x => x.Title == title && x.Id != id, cancellationToken))
        {
            return Conflict("A rejection reason with this title already exists.");
        }

        entity.Title = title;
        entity.IsActive = dto.IsActive;
        entity.SortOrder = dto.SortOrder;
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(HrmsPermissions.MasterDataManage)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var entity = await _context.LeaveRejectionReasons.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return NotFound();
        }

        _context.LeaveRejectionReasons.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
