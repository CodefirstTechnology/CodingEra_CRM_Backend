using HRMS.Authorization;
using HRMS.Data;
using HRMS.DTOs;
using HRMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Controllers;

[Route("api/performance-reviews")]
[ApiController]
public class PerformanceReviewsController : ControllerBase
{
    private readonly HRMSDbContext _context;
    private readonly ICurrentUserAccessor _currentUser;

    public PerformanceReviewsController(HRMSDbContext context, ICurrentUserAccessor currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? employeeId,
        [FromQuery] string? status,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PerformanceReviews
            .AsNoTracking()
            .Include(x => x.Employee)!
            .ThenInclude(e => e!.Designation)
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

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        return Ok(await query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _context.PerformanceReviews
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
    [RequirePermission(HrmsPermissions.PerformanceManageAll)]
    public async Task<IActionResult> Create([FromBody] PerformanceReviewUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!await _context.Employees.AnyAsync(x => x.Id == dto.EmployeeId, cancellationToken))
        {
            return BadRequest("Employee not found.");
        }

        if (await _context.PerformanceReviews.AnyAsync(
                x => x.EmployeeId == dto.EmployeeId && x.ReviewPeriod == dto.ReviewPeriod.Trim(),
                cancellationToken))
        {
            return Conflict("Performance review already exists for this employee and review period.");
        }

        var entity = new PerformanceReview
        {
            EmployeeId = dto.EmployeeId,
            ReviewPeriod = dto.ReviewPeriod.Trim(),
            KeyAchievements = dto.KeyAchievements.Trim(),
            ManagerRating = dto.ManagerRating,
            Status = dto.Status.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.PerformanceReviews.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(entity);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] PerformanceReviewUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var entity = await _context.PerformanceReviews.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return NotFound();
        }

        if (_currentUser.IsAdmin)
        {
            var permissionError = this.EnsurePermission(_currentUser, HrmsPermissions.PerformanceManageAll);
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

            if (dto.EmployeeId != entity.EmployeeId)
            {
                return Forbid();
            }

            var permissionError = this.EnsurePermission(_currentUser, HrmsPermissions.PerformanceSelfReview);
            if (permissionError != null)
            {
                return permissionError;
            }
        }

        if (await _context.PerformanceReviews.AnyAsync(
                x => x.EmployeeId == dto.EmployeeId &&
                     x.ReviewPeriod == dto.ReviewPeriod.Trim() &&
                     x.Id != id,
                cancellationToken))
        {
            return Conflict("Performance review already exists for this employee and review period.");
        }

        entity.EmployeeId = dto.EmployeeId;
        entity.ReviewPeriod = dto.ReviewPeriod.Trim();
        entity.KeyAchievements = dto.KeyAchievements.Trim();

        if (_currentUser.IsAdmin)
        {
            entity.ManagerRating = dto.ManagerRating;
        }

        entity.Status = dto.Status.Trim();
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(HrmsPermissions.PerformanceManageAll)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var entity = await _context.PerformanceReviews.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return NotFound();
        }

        _context.PerformanceReviews.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
