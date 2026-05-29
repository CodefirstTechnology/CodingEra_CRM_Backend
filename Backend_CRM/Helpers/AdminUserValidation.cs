using CRM.DATA;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Helpers
{
    public static class AdminUserValidation
    {
        public const int AdminRoleId = 2;

        public static async Task<IActionResult?> ValidateAdminUserAsync(TaskDbcontext db, int userId)
        {
            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(db, userId);
            if (auditErr != null)
            {
                return auditErr;
            }

            var roleId = await db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.RoleId)
                .FirstOrDefaultAsync();

            if (roleId != AdminRoleId)
            {
                return new ForbidResult();
            }

            return null;
        }
    }
}
