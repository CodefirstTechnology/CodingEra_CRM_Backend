using System.Security.Claims;
using HRMS.Models;
using Microsoft.AspNetCore.Http;

namespace HRMS.Authorization;

public sealed class TenantAccessor : ITenantAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private int? _explicitTenantId;

    public TenantAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private HttpContext? HttpContext => _httpContextAccessor.HttpContext;
    private ClaimsPrincipal? User => HttpContext?.User;

    public bool IsSuperAdmin
    {
        get
        {
            var roleClaim = User?.FindFirstValue(ClaimTypes.Role) ?? User?.FindFirstValue("role");
            return Enum.TryParse<UserRole>(roleClaim, ignoreCase: true, out var role) && role == UserRole.SUPER_ADMIN;
        }
    }

    public int? TenantId
    {
        get
        {
            if (_explicitTenantId.HasValue)
            {
                return _explicitTenantId.Value;
            }

            // For non-SuperAdmin users, the JWT claim is always authoritative.
            var claimVal = User?.FindFirstValue("tenantId") ?? User?.FindFirstValue("tenant_id");
            if (int.TryParse(claimVal, out var claimTenantId) && claimTenantId > 0)
            {
                return claimTenantId;
            }

            // For SuperAdmin, allow switching tenant context via Header or Query
            if (IsSuperAdmin && HttpContext != null)
            {
                if (HttpContext.Request.Headers.TryGetValue("X-Tenant-ID", out var headerVal) &&
                    int.TryParse(headerVal.FirstOrDefault(), out var headerTenantId) &&
                    headerTenantId > 0)
                {
                    return headerTenantId;
                }

                if (HttpContext.Request.Query.TryGetValue("tenantId", out var queryVal) &&
                    int.TryParse(queryVal.FirstOrDefault(), out var queryTenantId) &&
                    queryTenantId > 0)
                {
                    return queryTenantId;
                }
            }

            return null;
        }
    }

    public string? TenantCode
    {
        get
        {
            return User?.FindFirstValue("tenantCode") ?? User?.FindFirstValue("tenant_code");
        }
    }

    public bool HasTenant => TenantId.HasValue && TenantId.Value > 0;

    public void SetExplicitTenantId(int? tenantId)
    {
        _explicitTenantId = tenantId;
    }
}
