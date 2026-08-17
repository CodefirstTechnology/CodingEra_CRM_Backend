using HRMS.Authorization;
using HRMS.Data;
using HRMS.DTOs;
using HRMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Controllers;

[Route("api/employee-documents")]
[ApiController]
public class EmployeeDocumentsController : ControllerBase
{
    private readonly HRMSDbContext _context;
    private readonly ICurrentUserAccessor _currentUser;

    public EmployeeDocumentsController(HRMSDbContext context, ICurrentUserAccessor currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? employeeId,
        [FromQuery] int? documentCategoryId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.EmployeeDocuments
            .AsNoTracking()
            .Include(x => x.Employee)
            .Include(x => x.DocumentCategory)
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

        if (documentCategoryId.HasValue)
        {
            query = query.Where(x => x.DocumentCategoryId == documentCategoryId.Value);
        }

        return Ok(await query.OrderByDescending(x => x.UploadedAt).ToListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _context.EmployeeDocuments
            .AsNoTracking()
            .Include(x => x.Employee)
            .Include(x => x.DocumentCategory)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (item == null)
        {
            return NotFound();
        }

        var accessError = this.EnsureEmployeeAccess(_currentUser, item.EmployeeId);
        return accessError ?? Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EmployeeDocumentUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (_currentUser.IsAdmin)
        {
            var permissionError = this.EnsurePermission(_currentUser, HrmsPermissions.DocumentsManageAll);
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

            var permissionError = this.EnsurePermission(_currentUser, HrmsPermissions.DocumentsManageOwn);
            if (permissionError != null)
            {
                return permissionError;
            }
        }

        if (!await _context.Employees.AnyAsync(x => x.Id == dto.EmployeeId, cancellationToken))
        {
            return BadRequest("Employee not found.");
        }

        if (!await _context.DocumentCategories.AnyAsync(x => x.Id == dto.DocumentCategoryId, cancellationToken))
        {
            return BadRequest("Document category not found.");
        }

        var entity = new EmployeeDocument
        {
            EmployeeId = dto.EmployeeId,
            DocumentCategoryId = dto.DocumentCategoryId,
            DocumentName = dto.DocumentName.Trim(),
            FilePath = dto.FilePath.Trim(),
            UploadedAt = DateTime.UtcNow
        };

        _context.EmployeeDocuments.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(entity);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] EmployeeDocumentUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var entity = await _context.EmployeeDocuments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return NotFound();
        }

        if (_currentUser.IsAdmin)
        {
            var permissionError = this.EnsurePermission(_currentUser, HrmsPermissions.DocumentsManageAll);
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
        }

        entity.EmployeeId = dto.EmployeeId;
        entity.DocumentCategoryId = dto.DocumentCategoryId;
        entity.DocumentName = dto.DocumentName.Trim();
        entity.FilePath = dto.FilePath.Trim();
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var entity = await _context.EmployeeDocuments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return NotFound();
        }

        if (_currentUser.IsAdmin)
        {
            var permissionError = this.EnsurePermission(_currentUser, HrmsPermissions.DocumentsManageAll);
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
        }

        _context.EmployeeDocuments.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
