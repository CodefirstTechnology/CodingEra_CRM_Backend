using HRMS.Authorization;
using HRMS.Data;
using HRMS.DTOs;
using HRMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Controllers;

[Route("api/users")]
[ApiController]
[Authorize(Roles = "SUPER_ADMIN")]
public class UsersController : ControllerBase
{
    private readonly HRMSDbContext _context;

    public UsersController(HRMSDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [RequirePermission(HrmsPermissions.ManageHrAdmins)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var users = await _context.Users
            .AsNoTracking()
            .Include(x => x.Role)
            .Where(x => x.RoleId == RoleSeed.HrAdmin.Id || x.RoleId == RoleSeed.Employee.Id)
            .OrderBy(x => x.FullName)
            .Select(x => new UserResponseDto
            {
                Id = x.Id,
                FullName = x.FullName,
                Email = x.Email,
                Role = x.Role.Code,
                EmployeeId = x.EmployeeId,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpPost]
    [RequirePermission(HrmsPermissions.ManageHrAdmins)]
    public async Task<IActionResult> Create([FromBody] UserUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!Enum.TryParse<UserRole>(dto.Role, ignoreCase: true, out var role) ||
            role is UserRole.SUPER_ADMIN)
        {
            return BadRequest("Only HR_ADMIN or EMPLOYEE roles can be managed.");
        }

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _context.Users.AnyAsync(x => x.Email.ToLower() == email, cancellationToken))
        {
            return Conflict("Email already exists.");
        }

        var roleId = await RoleSeed.GetRoleIdAsync(_context, role, cancellationToken);
        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            RoleId = roleId,
            EmployeeId = dto.EmployeeId,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new UserResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = RoleMapper.ToRoleCode(role),
            EmployeeId = user.EmployeeId,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        });
    }

    [HttpPut("{id:int}")]
    [RequirePermission(HrmsPermissions.ManageHrAdmins)]
    public async Task<IActionResult> Update(int id, [FromBody] UserUpsertDto dto, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user == null)
        {
            return NotFound();
        }

        if (user.RoleId == RoleSeed.SuperAdmin.Id)
        {
            return Forbid();
        }

        if (!Enum.TryParse<UserRole>(dto.Role, ignoreCase: true, out var role) ||
            role is UserRole.SUPER_ADMIN)
        {
            return BadRequest("Only HR_ADMIN or EMPLOYEE roles can be managed.");
        }

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _context.Users.AnyAsync(x => x.Id != id && x.Email.ToLower() == email, cancellationToken))
        {
            return Conflict("Email already exists.");
        }

        user.FullName = dto.FullName.Trim();
        user.Email = email;
        user.RoleId = await RoleSeed.GetRoleIdAsync(_context, role, cancellationToken);
        user.EmployeeId = dto.EmployeeId;
        user.IsActive = dto.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new UserResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = RoleMapper.ToRoleCode(role),
            EmployeeId = user.EmployeeId,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        });
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(HrmsPermissions.ManageHrAdmins)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user == null)
        {
            return NotFound();
        }

        if (user.RoleId == RoleSeed.SuperAdmin.Id)
        {
            return Forbid();
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
