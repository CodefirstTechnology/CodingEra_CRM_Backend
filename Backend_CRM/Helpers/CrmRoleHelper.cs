using CRM.DATA;
using Microsoft.EntityFrameworkCore;

namespace CRM.Helpers
{
    /// <summary>Role checks for CRM data visibility (admin and super-admin style roles).</summary>
    public static class CrmRoleHelper
    {
        public const int AdminRoleId = AdminUserValidation.AdminRoleId;

        public static async Task<bool> CanViewAllRecordsAsync(TaskDbcontext db, int userId)
        {
            var row = await db.Users.AsNoTracking()
                .Where(u => u.Id == userId && u.IsActive)
                .Select(u => new { u.RoleId, RoleName = u.Role != null ? u.Role.Name : string.Empty })
                .FirstOrDefaultAsync();

            if (row == null)
            {
                return false;
            }

            if (row.RoleId == AdminRoleId)
            {
                return true;
            }

            return IsSuperAdminRoleName(row.RoleName);
        }

        public static bool IsSuperAdminRoleName(string? roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return false;
            }

            var n = roleName.Trim().ToLowerInvariant();
            return n.Contains("super admin")
                || n.Contains("superadmin")
                || n == "super_admin"
                || n == "super-admin";
        }
    }
}
