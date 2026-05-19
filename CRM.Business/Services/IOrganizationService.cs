using CRM.Business.Common;
using CRM.DTO;
using CRM.models;

namespace CRM.Business.Services;

public interface IOrganizationService
{
    Task<IReadOnlyList<Organization>> GetAllAsync();
    Task<Organization?> GetByIdAsync(int id);
    Task<ServiceResult<Organization>> CreateAsync(int userId, OrganizationUpsertDto dto);
    Task<ServiceResult<Organization>> UpdateAsync(int id, int userId, OrganizationUpsertDto dto);
    Task<ServiceResult> DeleteAsync(int id);
}
