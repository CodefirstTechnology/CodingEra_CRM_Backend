using CRM.Business.Common;
using CRM.DATA;
using CRM.DTO;
using CRM.models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Business.Services;

public sealed class AuthService(TaskDbcontext db, IAuditUserService auditUserService) : IAuthService
{
    public async Task<ServiceResult<UserSessionDto>> RegisterAsync(int? auditUserId, RegisterRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
        {
            return ServiceResult<UserSessionDto>.Fail(ServiceStatus.BadRequest, "Email and password are required.");
        }

        if (auditUserId is int auditUid && auditUid > 0)
        {
            var auditResult = await auditUserService.ValidateAndSetAuditUserAsync(auditUid);
            if (auditResult.Status != ServiceStatus.Success)
            {
                return ServiceResult<UserSessionDto>.Fail(auditResult.Status, auditResult.Message);
            }
        }

        var email = req.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email.ToLower() == email))
        {
            return ServiceResult<UserSessionDto>.Fail(ServiceStatus.Conflict, "An account with this email already exists.");
        }

        var (roleId, roleError) = await ResolveRegisterRoleIdAsync(req.RoleId);
        if (roleError != null)
        {
            return roleError;
        }

        var user = new User
        {
            FullName = req.FullName?.Trim() ?? string.Empty,
            Email = email,
            Phone = req.Phone?.Trim() ?? string.Empty,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            RoleId = roleId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();
        await db.Entry(user).Reference(u => u.Role).LoadAsync();
        return ServiceResult<UserSessionDto>.Ok(ToSession(user));
    }

    public async Task<ServiceResult<UserSessionDto>> LoginAsync(LoginRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
        {
            return ServiceResult<UserSessionDto>.Fail(ServiceStatus.BadRequest, "Email and password are required.");
        }

        var email = req.Email.Trim().ToLowerInvariant();
        var user = await db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
        {
            return ServiceResult<UserSessionDto>.Fail(ServiceStatus.Unauthorized, "Invalid email or password.");
        }

        if (!user.IsActive)
        {
            return ServiceResult<UserSessionDto>.Fail(ServiceStatus.Unauthorized, "Account is inactive.");
        }

        return ServiceResult<UserSessionDto>.Ok(ToSession(user));
    }

    public async Task<IReadOnlyList<UserListItemDto>> GetAllUsersAsync() =>
        await db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .OrderBy(u => u.FullName)
            .ThenBy(u => u.Email)
            .Select(u => new UserListItemDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.Phone,
                RoleId = u.RoleId,
                Role = u.Role != null ? u.Role.Name : string.Empty,
                CreatedAt = u.CreatedAt,
            })
            .ToListAsync();

    public async Task<ServiceResult<UserSessionDto>> GetUserAsync(int id)
    {
        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id);
        return user == null
            ? ServiceResult<UserSessionDto>.Fail(ServiceStatus.NotFound)
            : ServiceResult<UserSessionDto>.Ok(ToSession(user));
    }

    private async Task<(int? RoleId, ServiceResult<UserSessionDto>? Error)> ResolveRegisterRoleIdAsync(int? requestedRoleId)
    {
        if (requestedRoleId is int rid && rid > 0)
        {
            if (!await db.Roles.AnyAsync(r => r.Id == rid && r.IsActive))
            {
                return (null, ServiceResult<UserSessionDto>.Fail(
                    ServiceStatus.BadRequest,
                    $"Role id {rid} does not exist or is inactive."));
            }

            return (rid, null);
        }

        var defaultRoleId = await db.Roles.AsNoTracking()
            .Where(r => r.IsActive && r.Name.ToLower() == "user")
            .Select(r => (int?)r.Id)
            .FirstOrDefaultAsync();
        if (defaultRoleId == null)
        {
            return (null, ServiceResult<UserSessionDto>.Fail(
                ServiceStatus.BadRequest,
                "No active role named 'user' exists. Create it via /api/MasterData/roles or send roleId."));
        }

        return (defaultRoleId, null);
    }

    private static UserSessionDto ToSession(User u) => new()
    {
        Id = u.Id,
        FullName = u.FullName,
        Email = u.Email,
        Phone = u.Phone,
        RoleId = u.RoleId,
        Role = u.Role?.Name ?? string.Empty,
        Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())[..22],
    };
}
