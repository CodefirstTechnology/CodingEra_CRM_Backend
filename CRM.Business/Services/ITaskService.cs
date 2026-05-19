using CRM.Business.Common;
using CRM.DTO;
using CRM.models;

namespace CRM.Business.Services;

public interface ITaskService
{
    Task<IReadOnlyList<TaskTable>> GetAllAsync(int? relatedLeadId, int? relatedDealId);
    Task<TaskTable?> GetByIdAsync(int id);
    Task<ServiceResult<TaskTable>> CreateAsync(int userId, TaskUpsertDto dto);
    Task<ServiceResult<TaskTable>> UpdateAsync(int id, int userId, TaskUpsertDto dto);
    Task<ServiceResult> DeleteAsync(int id);
}
