using HRMS.Authorization;
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

    public static IActionResult? EnsurePermission(this ControllerBase controller, ICurrentUserAccessor currentUser, string permission)
    {
        if (!currentUser.HasPermission(permission))
        {
            return controller.Forbid();
        }

        return null;
    }
}
