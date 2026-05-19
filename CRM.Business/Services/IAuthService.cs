using CRM.Business.Common;
using CRM.DTO;

namespace CRM.Business.Services;

public interface IAuthService
{
    Task<ServiceResult<UserSessionDto>> RegisterAsync(int? auditUserId, RegisterRequest request);
    Task<ServiceResult<UserSessionDto>> LoginAsync(LoginRequest request);
    Task<IReadOnlyList<UserListItemDto>> GetAllUsersAsync();
    Task<ServiceResult<UserSessionDto>> GetUserAsync(int id);
}
