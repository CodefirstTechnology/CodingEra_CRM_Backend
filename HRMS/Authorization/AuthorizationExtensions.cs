using HRMS.Models;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Authorization;

public static class AuthorizationExtensions
{
    public static IActionResult? EnsureEmployeeAccess(this ControllerBase controller, ICurrentUserAccessor currentUser, int employeeId)
    {
        if (!currentUser.CanAccessEmployee(employeeId))
        {
            return controller.Forbid();
        }

        return null;
    }

    public static IActionResult? EnsureTenantAccess(this ControllerBase controller, ICurrentUserAccessor currentUser, int tenantId)
    {
        if (!currentUser.CanAccessTenant(tenantId))
        {
            return controller.Forbid();
        }

        return null;
    }

    public static IActionResult? EnsurePermission(this ControllerBase controller, ICurrentUserAccessor currentUser, string permission)
    {
        if (!currentUser.HasPermission(permission))
        {
            return controller.Forbid();
        }

        return null;
    }

    public static IActionResult? EnsureRole(this ControllerBase controller, ICurrentUserAccessor currentUser, params UserRole[] roles)
    {
        if (!currentUser.Role.HasValue || !roles.Contains(currentUser.Role.Value))
        {
            return controller.Forbid();
        }

        return null;
    }
}
