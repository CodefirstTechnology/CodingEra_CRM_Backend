using CRM.DATA;
using CRM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Helpers
{
    public static class AdminUserValidation
    {
        public const int AdminRoleId = 2;

        public static async Task<IActionResult?> ValidateAdminUserAsync(
            TaskDbcontext db,
            int userId,
            IRbacService? rbac = null)
        {
            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(db, userId);
            if (auditErr != null)
            {
                return auditErr;
            }

            if (rbac != null && await rbac.IsAdminUserAsync(userId))
            {
                return null;
            }

            var roleRow = await db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.RoleId, RoleName = u.Role != null ? u.Role.Name : string.Empty })
                .FirstOrDefaultAsync();

            if (RbacAdminHelper.IsAdminRole(roleRow?.RoleId, roleRow?.RoleName))
            {
                return null;
            }

            return ApiForbiddenResult.Create();
        }
    }
}
