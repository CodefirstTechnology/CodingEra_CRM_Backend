using CRM.Business.Common;
using CRM.DTO;
using CRM.models;

namespace CRM.Business.Services;

public interface ICallLogService
{
    Task<ServiceResult<CallLog>> AddAsync(int userId, CallLogUpsertDto dto);
    Task<IReadOnlyList<CallLog>> GetAllAsync();
    Task<ServiceResult<CallLog>> UpdateAsync(int id, int userId, CallLogUpsertDto dto);
    Task<ServiceResult> DeleteAsync(int id);
}
