using HRMS.Authorization;
using HRMS.Data;
using HRMS.DTOs;
using HRMS.Models;
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

    public AuthService(HRMSDbContext context, IJwtTokenService jwtTokenService)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<(LoginResponseDto? Response, string? Error)> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users
            .AsNoTracking()
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Email.ToLower() == email && x.IsActive, cancellationToken);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return (null, "Invalid email or password.");
        }

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
            EmployeeId = user.EmployeeId,
            Permissions = HrmsRolePermissions.GetPermissions(role).ToArray()
        };
    }
}
