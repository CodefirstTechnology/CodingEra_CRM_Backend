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
                return recordOwnerUserId.HasValue && _currentUser.UserId.HasValue && recordOwnerUserId.Value == _currentUser.UserId.Value;
            }

            return false;
        }

        public bool CanAccessRecord(string? recordOwnerString, AccessScope requiredScope = AccessScope.Own)
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
                if (string.IsNullOrWhiteSpace(recordOwnerString))
                {
                    return false;
                }

                var trimmed = recordOwnerString.Trim();

                if (_currentUser.UserId.HasValue && int.TryParse(trimmed, out var id) && id == _currentUser.UserId.Value)
                {
                    return true;
                }

                if (_currentUser.UserId.HasValue && string.Equals(trimmed, _currentUser.UserId.Value.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(_currentUser.Email) && string.Equals(trimmed, _currentUser.Email.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(_currentUser.FullName) && string.Equals(trimmed, _currentUser.FullName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                return false;
            }

            return false;
        }
    }
}
