using CRM.DTO;
using CRM.models;

namespace CRM.Services
{
    public interface IRbacService
    {
        Task<IReadOnlyList<UserPermissionDto>> GetUserPermissionsAsync(int userId);

        Task<bool> HasPermissionAsync(int userId, string permissionCode);

        Task<AccessScope?> GetModuleAccessScopeAsync(int userId, string module);

        Task<bool> CanViewAllRecordsAsync(int userId);

        Task<bool> CanManageRbacAsync(int userId);

        Task<bool> IsAdminUserAsync(int userId);

        Task<RbacAccessDiagnosticDto> GetAccessDiagnosticAsync(int userId);
    }
}
