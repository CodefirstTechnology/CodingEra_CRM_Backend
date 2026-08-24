using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using ERP.Application.Common.Security;
using ERP.Domain.Enums;
using ERP.Shared.Security;
using Microsoft.AspNetCore.Http;

namespace ERP.Infrastructure.Security
{
    /// <summary>
    /// Implements ICurrentUser by resolving authenticated identity claims from HttpContext.
    /// </summary>
    public class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUser(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

        public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

        public int? UserId
        {
            get
            {
                var principal = Principal;
                if (principal == null || !principal.Identity?.IsAuthenticated == true)
                {
                    return null;
                }

                // Check standard JWT claim keys in priority order
                var candidateKeys = new[]
                {
                    "sub",
                    "userId",
                    "user_id",
                    "UserId",
                    "id",
                    "nameid",
                    ClaimTypes.NameIdentifier
                };

                foreach (var key in candidateKeys)
                {
                    var claim = principal.FindFirst(key);
                    if (claim != null && int.TryParse(claim.Value, out var id) && id > 0)
                    {
                        return id;
                    }
                }

                return null;
            }
        }

        public string? Email
        {
            get
            {
                var principal = Principal;
                if (principal == null) return null;

                return principal.FindFirst("email")?.Value
                    ?? principal.FindFirst("Email")?.Value
                    ?? principal.FindFirst(ClaimTypes.Email)?.Value
                    ?? principal.FindFirst(ClaimTypes.Name)?.Value;
            }
        }

        public string? FullName
        {
            get
            {
                var principal = Principal;
                if (principal == null) return null;

                return principal.FindFirst("name")?.Value
                    ?? principal.FindFirst("FullName")?.Value
                    ?? principal.FindFirst("fullName")?.Value
                    ?? principal.FindFirst("unique_name")?.Value
                    ?? principal.FindFirst(ClaimTypes.Name)?.Value;
            }
        }

        public string? Role
        {
            get
            {
                var principal = Principal;
                if (principal == null) return null;

                var roleClaim = principal.FindFirst("role")?.Value
                    ?? principal.FindFirst("Role")?.Value
                    ?? principal.FindFirst(ClaimTypes.Role)?.Value;

                if (!string.IsNullOrWhiteSpace(roleClaim))
                {
                    return roleClaim.Trim();
                }

                return null;
            }
        }

        public bool IsAdmin
        {
            get
            {
                var role = Role;
                if (string.IsNullOrWhiteSpace(role))
                {
                    return false;
                }

                var r = role.Trim().ToLowerInvariant();
                return r == "admin" || r == "administrator";
            }
        }

        public AccessScope Scope
        {
            get
            {
                // In Phase 1: Admin always receives ALL scope
                if (IsAdmin)
                {
                    return AccessScope.All;
                }

                return AccessScope.Own;
            }
        }

        public IReadOnlyCollection<string> Permissions
        {
            get
            {
                if (IsAdmin)
                {
                    return ErpPermissions.All;
                }

                var principal = Principal;
                if (principal == null)
                {
                    return Array.Empty<string>();
                }

                var userPerms = principal.FindAll("permission")
                    .Concat(principal.FindAll("permissions"))
                    .Select(c => c.Value.Trim())
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return userPerms;
            }
        }

        public bool HasPermission(string permission)
        {
            if (!IsAuthenticated)
            {
                return false;
            }

            if (IsAdmin)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(permission))
            {
                return true;
            }

            return Permissions.Contains(permission.Trim(), StringComparer.OrdinalIgnoreCase);
        }

        public bool HasAnyPermission(params string[] permissions)
        {
            if (!IsAuthenticated)
            {
                return false;
            }

            if (IsAdmin)
            {
                return true;
            }

            if (permissions == null || permissions.Length == 0)
            {
                return true;
            }

            return permissions.Any(p => HasPermission(p));
        }
    }
}
