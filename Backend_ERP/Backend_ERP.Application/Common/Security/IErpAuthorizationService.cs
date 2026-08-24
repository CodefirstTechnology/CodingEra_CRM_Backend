using System.Threading;
using System.Threading.Tasks;
using ERP.Domain.Enums;

namespace ERP.Application.Common.Security
{
    /// <summary>
    /// Core authorization service for validating permissions and record scopes.
    /// </summary>
    public interface IErpAuthorizationService
    {
        /// <summary>
        /// Checks if the current user is authorized for the given permission.
        /// </summary>
        bool Authorize(string permission);

        /// <summary>
        /// Checks if the current user is authorized for any of the given permissions.
        /// </summary>
        bool AuthorizeAny(params string[] permissions);

        /// <summary>
        /// Checks if the current user has access to a specific record based on its owner/creator and current scope.
        /// For Admin, this always returns true (ALL scope).
        /// </summary>
        bool CanAccessRecord(int? recordOwnerUserId, AccessScope requiredScope = AccessScope.Own);
    }
}
