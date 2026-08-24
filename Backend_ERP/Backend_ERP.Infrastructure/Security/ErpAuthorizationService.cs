using System;
using ERP.Application.Common.Security;
using ERP.Domain.Enums;

namespace ERP.Infrastructure.Security
{
    /// <summary>
    /// Implements IErpAuthorizationService by delegating to ICurrentUser and scope rules.
    /// </summary>
    public class ErpAuthorizationService : IErpAuthorizationService
    {
        private readonly ICurrentUser _currentUser;

        public ErpAuthorizationService(ICurrentUser currentUser)
        {
            _currentUser = currentUser;
        }

        public bool Authorize(string permission)
        {
            return _currentUser.HasPermission(permission);
        }

        public bool AuthorizeAny(params string[] permissions)
        {
            return _currentUser.HasAnyPermission(permissions);
        }

        public bool CanAccessRecord(int? recordOwnerUserId, AccessScope requiredScope = AccessScope.Own)
        {
            if (!_currentUser.IsAuthenticated)
            {
                return false;
            }

            // Admin always has ALL scope
            if (_currentUser.IsAdmin || _currentUser.Scope == AccessScope.All)
            {
                return true;
            }

            if (_currentUser.Scope == AccessScope.Own)
            {
                return recordOwnerUserId.HasValue && recordOwnerUserId.Value == _currentUser.UserId;
            }

            return false;
        }
    }
}
