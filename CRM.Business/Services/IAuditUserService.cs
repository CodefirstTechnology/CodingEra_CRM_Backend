using CRM.Business.Common;

namespace CRM.Business.Services;

public interface IAuditUserService
{
    Task<ServiceResult> ValidateAndSetAuditUserAsync(int userId);
}
