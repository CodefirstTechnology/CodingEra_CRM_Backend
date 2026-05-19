using CRM.Business.Common;
using CRM.DTO;
using CRM.models;

namespace CRM.Business.Services;

public interface ILeadService
{
    Task<IReadOnlyList<Lead>> GetAllAsync(string? leadSource, string? status);
    Task<Lead?> GetByIdAsync(int id);
    Task<ServiceResult<IReadOnlyList<LeadHistory>>> GetHistoryAsync(int id);
    Task<ServiceResult<Lead>> CreateAsync(int userId, LeadUpsertDto dto);
    Task<ServiceResult<Lead>> UpdateAsync(int id, int userId, LeadUpsertDto dto);
    Task<ServiceResult> DeleteAsync(int id);
}
