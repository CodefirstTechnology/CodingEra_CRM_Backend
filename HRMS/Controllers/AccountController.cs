using HRMS.Authorization;
using HRMS.Data;
using HRMS.DTOs;
using HRMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Controllers;

[Route("api/account")]
[ApiController]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly HRMSDbContext _context;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IAuditService _auditService;

    public AccountController(
        HRMSDbContext context,
        ICurrentUserAccessor currentUser,
        IAuditService auditService)
    {
        _context = context;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return Unauthorized();
        }

        var user = await _context.Users
            .IgnoreQueryFilters()
            .Include(x => x.Role)
            .Include(x => x.Tenant)
            .FirstOrDefaultAsync(x => x.Id == _currentUser.UserId.Value, cancellationToken);

        if (user == null)
        {
            return NotFound();
        }

        var role = RoleMapper.ToUserRole(user.Role);
        return Ok(new UserProfileResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = role.ToString(),
            TenantId = user.TenantId,
            TenantName = user.Tenant?.Name,
            TenantCode = user.Tenant?.Code,
            TenantPlan = user.Tenant?.Plan,
            EmployeeId = user.EmployeeId,
            Status = user.Status,
            LastLoginAt = user.LastLoginAt,
            LastLoginIp = user.LastLoginIp,
            Permissions = HrmsRolePermissions.GetPermissions(role).ToArray()
        });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!_currentUser.UserId.HasValue)
        {
            return Unauthorized();
        }

        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == _currentUser.UserId.Value, cancellationToken);

        if (user == null)
        {
            return NotFound();
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
        {
            return BadRequest(new { message = "Incorrect current password." });
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.PasswordResetRequired = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("CHANGE_PASSWORD", "User", user.Id.ToString(), new { user.Email }, user.TenantId, cancellationToken);

        return Ok(new { message = "Password updated successfully." });
    }

    [HttpGet("login-activity")]
    public async Task<IActionResult> GetLoginActivity(CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return Unauthorized();
        }

        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == _currentUser.UserId.Value, cancellationToken);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(new LoginActivityDto
        {
            LastLoginAt = user.LastLoginAt,
            LastLoginIp = user.LastLoginIp,
            Status = user.Status,
            CreatedAt = user.CreatedAt
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (_currentUser.UserId.HasValue)
        {
            await _auditService.LogAsync("LOGOUT", "User", _currentUser.UserId.Value.ToString(), new { _currentUser.Email }, _currentUser.TenantId, cancellationToken);
        }

        return Ok(new { message = "Logged out successfully." });
    }
}
