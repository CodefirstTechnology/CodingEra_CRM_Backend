using System.Security.Claims;
using HRMS.Models;
using Microsoft.AspNetCore.Authorization;

namespace HRMS.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var roleClaim = context.User.FindFirstValue(ClaimTypes.Role)
            ?? context.User.FindFirstValue("role");

        if (string.IsNullOrWhiteSpace(roleClaim))
        {
            return Task.CompletedTask;
        }

        if (!Enum.TryParse<UserRole>(roleClaim, ignoreCase: true, out var role))
        {
            return Task.CompletedTask;
        }

        if (HrmsRolePermissions.HasPermission(role, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
