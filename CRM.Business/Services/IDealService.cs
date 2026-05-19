using CRM.Business.Common;
using CRM.DTO;
using CRM.models;

namespace CRM.Business.Services;

public interface IDealService
{
    Task<IReadOnlyList<Deal>> GetAllAsync(string? status);
    Task<Deal?> GetByIdAsync(int id);
    Task<ServiceResult<Deal>> CreateAsync(int userId, DealUpsertDto dto);
    Task<ServiceResult<Deal>> UpdateAsync(int id, int userId, DealUpsertDto dto);
    Task<ServiceResult> DeleteAsync(int id);
}
