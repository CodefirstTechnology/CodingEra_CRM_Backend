using System.Collections.Generic;
using ERP.Domain.Enums;

namespace ERP.Application.Common.Security
{
    /// <summary>
    /// Centralized abstraction representing the currently authenticated identity in ERP.
    /// Obtains user information strictly from the validated CRM-issued JWT.
    /// </summary>
    public interface ICurrentUser
    {
        /// <summary>
        /// CRM User ID of the authenticated user.
        /// </summary>
        int? UserId { get; }

        /// <summary>
        /// Email address of the authenticated user.
        /// </summary>
        string? Email { get; }

        /// <summary>
        /// Full name of the authenticated user.
        /// </summary>
        string? FullName { get; }

        /// <summary>
        /// Primary role name of the authenticated user (e.g. "Admin", "Sales Executive").
        /// </summary>
        string? Role { get; }

        /// <summary>
        /// True if the request is authenticated via a valid token.
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// True if the authenticated user has an Admin role ("Admin" or "Administrator").
        /// </summary>
        bool IsAdmin { get; }

        /// <summary>
        /// The effective access visibility scope. In Phase 1, Admin always has AccessScope.All.
        /// </summary>
        AccessScope Scope { get; }

        /// <summary>
        /// Evaluates whether the current user has the specified permission.
        /// </summary>
        bool HasPermission(string permission);

        /// <summary>
        /// Evaluates whether the current user has any of the specified permissions.
        /// </summary>
        bool HasAnyPermission(params string[] permissions);

        /// <summary>
        /// Returns all effective permissions for the current user.
        /// </summary>
        IReadOnlyCollection<string> Permissions { get; }
    }
}
