using CRM.Business.Common;
using CRM.DATA;
using Microsoft.EntityFrameworkCore;

namespace CRM.Business.Services;

public sealed class AuditUserService(TaskDbcontext db) : IAuditUserService
{
    public async Task<ServiceResult> ValidateAndSetAuditUserAsync(int userId)
    {
        if (userId <= 0)
        {
            return ServiceResult.Fail(ServiceStatus.BadRequest, "userId must be a positive integer.");
        }

        if (!await db.Users.AsNoTracking().AnyAsync(u => u.Id == userId && u.IsActive))
        {
            return ServiceResult.Fail(ServiceStatus.BadRequest, $"User id {userId} does not exist or is inactive.");
        }

        db.AuditUserId = userId;
        return ServiceResult.Ok();
    }
}
