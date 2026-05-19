using CRM.Business.Common;
using CRM.DTO;
using CRM.models;

namespace CRM.Business.Services;

public interface IContactService
{
    Task<IReadOnlyList<Contact>> GetAllAsync(int? organizationId);
    Task<Contact?> GetByIdAsync(int id);
    Task<ServiceResult<Contact>> CreateAsync(int userId, ContactUpsertDto dto);
    Task<ServiceResult<Contact>> UpdateAsync(int id, int userId, ContactUpsertDto dto);
    Task<ServiceResult> DeleteAsync(int id);
}
