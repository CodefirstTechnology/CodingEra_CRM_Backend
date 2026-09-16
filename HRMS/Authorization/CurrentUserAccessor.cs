using System.Security.Claims;
using HRMS.Models;
using Microsoft.AspNetCore.Http;

namespace HRMS.Authorization;

public interface ICurrentUserAccessor
{
    int? UserId { get; }
    string? Email { get; }
    string? FullName { get; }
    UserRole? Role { get; }
    int? TenantId { get; }
    string? TenantCode { get; }
    string? TenantName { get; }
    int? EmployeeId { get; }
    bool IsAuthenticated { get; }
    bool IsSuperAdmin { get; }
    bool IsHrAdmin { get; }
    bool IsEmployee { get; }
    bool IsAdmin { get; }
    string? IpAddress { get; }
    bool CanAccessEmployee(int employeeId);
    bool CanAccessTenant(int tenantId);
    bool HasPermission(string permission);
}

public sealed class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITenantAccessor _tenantAccessor;

    public CurrentUserAccessor(IHttpContextAccessor httpContextAccessor, ITenantAccessor tenantAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
        _tenantAccessor = tenantAccessor;
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

    public string? Email =>
        User?.FindFirstValue(ClaimTypes.Email) ?? User?.FindFirstValue("email");

    public string? FullName =>
        User?.FindFirstValue(ClaimTypes.Name) ?? User?.FindFirstValue("name");

    public UserRole? Role
    {
        get
        {
            var value = User?.FindFirstValue(ClaimTypes.Role)
                ?? User?.FindFirstValue("role");
            return Enum.TryParse<UserRole>(value, ignoreCase: true, out var role) ? role : null;
        }
    }

    public int? TenantId => _tenantAccessor.TenantId;

    public string? TenantCode => _tenantAccessor.TenantCode;

    public string? TenantName => User?.FindFirstValue("tenantName");

    public int? EmployeeId
    {
        get
        {
            var value = User?.FindFirstValue("employeeId");
            return int.TryParse(value, out var id) ? id : null;
        }
    }

    public bool IsSuperAdmin => Role == UserRole.SUPER_ADMIN;

    public bool IsHrAdmin => Role == UserRole.HR_ADMIN;

    public bool IsEmployee => Role == UserRole.EMPLOYEE;

    public bool IsAdmin => Role.HasValue && HrmsRolePermissions.IsAdminRole(Role.Value);

    public string? IpAddress =>
        _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public bool CanAccessEmployee(int employeeId)
    {
        if (IsSuperAdmin)
        {
            return true;
        }

        if (IsHrAdmin)
        {
            return true;
        }

        return EmployeeId.HasValue && EmployeeId.Value == employeeId;
    }

    public bool CanAccessTenant(int tenantId)
    {
        if (IsSuperAdmin)
        {
            return true;
        }

        return TenantId.HasValue && TenantId.Value == tenantId;
    }

    public bool HasPermission(string permission) =>
        Role.HasValue && HrmsRolePermissions.HasPermission(Role.Value, permission);
}
