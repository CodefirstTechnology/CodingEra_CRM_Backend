namespace CRM.Helpers
{
    /// <summary>Identifies full-access admin roles (by id or name — not hard-coded to a single role id).</summary>
    public static class RbacAdminHelper
    {
        public static bool IsAdminRoleName(string? roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return false;
            }

            var n = roleName.Trim().ToLowerInvariant();
            return n == "admin"
                || n == "administrator"
                || CrmRoleHelper.IsSuperAdminRoleName(roleName);
        }

        public static bool IsAdminRole(int? roleId, string? roleName)
        {
            if (roleId == AdminUserValidation.AdminRoleId)
            {
                return true;
            }

            return IsAdminRoleName(roleName);
        }
    }
}
