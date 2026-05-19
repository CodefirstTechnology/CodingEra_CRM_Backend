using CRM.Business.Common;
using CRM.DTO;
using CRM.models;

namespace CRM.Business.Services.MasterData;

public interface IMasterDataService<TEntity>
    where TEntity : class, INamedMasterEntity, new()
{
    Task<IReadOnlyList<TEntity>> GetAllAsync(bool activeOnly);
    Task<TEntity?> GetByIdAsync(int id);
    Task<ServiceResult<TEntity>> CreateAsync(int userId, MasterDataUpsertDto dto);
    Task<ServiceResult<TEntity>> UpdateAsync(int id, int userId, MasterDataUpsertDto dto);
    Task<ServiceResult> DeleteAsync(int id);
}
