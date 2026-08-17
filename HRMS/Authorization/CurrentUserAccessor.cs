using System.Security.Claims;
using HRMS.Models;

namespace HRMS.Authorization;

public interface ICurrentUserAccessor
{
    int? UserId { get; }
    UserRole? Role { get; }
    int? EmployeeId { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
    bool CanAccessEmployee(int employeeId);
    bool HasPermission(string permission);
}

public sealed class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public int? UserId
    {
        get
        {
            var value = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User?.FindFirstValue("sub");
            return int.TryParse(value, out var id) ? id : null;
        }
    }

    public UserRole? Role
    {
        get
        {
            var value = User?.FindFirstValue(ClaimTypes.Role)
                ?? User?.FindFirstValue("role");
            return Enum.TryParse<UserRole>(value, ignoreCase: true, out var role) ? role : null;
        }
    }

    public int? EmployeeId
    {
        get
        {
            var value = User?.FindFirstValue("employeeId");
            return int.TryParse(value, out var id) ? id : null;
        }
    }

    public bool IsAdmin => Role.HasValue && HrmsRolePermissions.IsAdminRole(Role.Value);

    public bool CanAccessEmployee(int employeeId) =>
        IsAdmin || (EmployeeId.HasValue && EmployeeId.Value == employeeId);

    public bool HasPermission(string permission) =>
        Role.HasValue && HrmsRolePermissions.HasPermission(Role.Value, permission);
}
