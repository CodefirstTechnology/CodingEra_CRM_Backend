using System;
using System.Linq;
using System.Threading.Tasks;
using ERP.Application.Common.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.API.Security
{
    /// <summary>
    /// Enforces server-side RBAC authorization for ERP endpoints.
    /// Returns 401 Unauthorized if unauthenticated, and 403 Forbidden if the user lacks the required permission(s).
    /// Authenticated Admins automatically bypass permission checks with ALL scope.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public string[] Permissions { get; }

        public RequirePermissionAttribute(params string[] permissions)
        {
            Permissions = permissions ?? Array.Empty<string>();
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // Allow Anonymous endpoints to bypass authorization
            if (context.ActionDescriptor.EndpointMetadata.Any(em => em is Microsoft.AspNetCore.Authorization.IAllowAnonymous))
            {
                return;
            }

            var currentUser = context.HttpContext.RequestServices.GetService<ICurrentUser>();
            if (currentUser == null || !currentUser.IsAuthenticated)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    message = "Authentication is required to access this resource."
                });
                return;
            }

            // Admins have full access across all endpoints with ALL scope
            if (currentUser.IsAdmin)
            {
                return;
            }

            if (Permissions.Length > 0 && !currentUser.HasAnyPermission(Permissions))
            {
                context.Result = new ObjectResult(new
                {
                    message = "You do not have permission to perform this action.",
                    requiredPermissions = Permissions
                })
                {
                    StatusCode = 403
                };
                return;
            }

            await Task.CompletedTask;
        }
    }
}
