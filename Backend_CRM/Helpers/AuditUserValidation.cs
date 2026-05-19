using CRM.DATA;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Helpers
{
    public static class AuditUserValidation
    {
        public static async Task<IActionResult?> ValidateAuditUserAsync(TaskDbcontext db, int userId)
        {
            if (userId <= 0)
            {
                return new BadRequestObjectResult("userId must be a positive integer.");
            }

            if (!await db.Users.AsNoTracking().AnyAsync(u => u.Id == userId && u.IsActive))
            {
                return new BadRequestObjectResult($"User id {userId} does not exist or is inactive.");
            }

            return null;
        }

        public static void SetAuditUser(TaskDbcontext db, int userId) => db.AuditUserId = userId;
    }
}
