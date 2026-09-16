using HRMS.Authorization;
using HRMS.Data;
using HRMS.DTOs;
using HRMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Services;

public interface IAuthService
{
    Task<(LoginResponseDto? Response, string? Error)> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    AuthUserDto ToAuthUser(User user);
}

public sealed class AuthService : IAuthService
{
    private readonly HRMSDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuditService _auditService;

    public AuthService(
        HRMSDbContext context,
        IJwtTokenService jwtTokenService,
        IHttpContextAccessor httpContextAccessor,
        IAuditService auditService)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _httpContextAccessor = httpContextAccessor;
        _auditService = auditService;
    }

    public async Task<(LoginResponseDto? Response, string? Error)> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users
            .IgnoreQueryFilters()
            .Include(x => x.Role)
            .Include(x => x.Tenant)
            .FirstOrDefaultAsync(x => x.Email.ToLower() == email, cancellationToken);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return (null, "Invalid email or password.");
        }

        if (!user.IsActive || string.Equals(user.Status, "Suspended", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(user.Status, "Deactivated", StringComparison.OrdinalIgnoreCase))
        {
            return (null, "Your user account is suspended or deactivated. Please contact your administrator.");
        }

        if (user.Tenant != null &&
            (string.Equals(user.Tenant.Status, "Suspended", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(user.Tenant.Status, "Deactivated", StringComparison.OrdinalIgnoreCase)))
        {
            return (null, $"Organization '{user.Tenant.Name}' is currently suspended or inactive. Please contact support.");
        }

        var ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        user.LastLoginAt = DateTime.UtcNow;
        user.LastLoginIp = ip;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            "LOGIN",
            "User",
            user.Id.ToString(),
            new { user.Email, user.FullName, user.TenantId, Ip = ip },
            user.TenantId,
            cancellationToken);

        var (token, expiresAt) = _jwtTokenService.CreateToken(user);
        return (new LoginResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = ToAuthUser(user)
        }, null);
    }

    public AuthUserDto ToAuthUser(User user)
    {
        var role = RoleMapper.ToUserRole(user.Role);
        return new AuthUserDto
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
            Permissions = HrmsRolePermissions.GetPermissions(role).ToArray()
        };
    }
}
