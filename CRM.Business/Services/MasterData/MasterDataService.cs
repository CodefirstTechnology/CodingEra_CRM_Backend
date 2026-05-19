using CRM.Business.Common;
using CRM.DATA;
using CRM.DTO;
using CRM.models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Business.Services.MasterData;

public sealed class MasterDataService<TEntity>(
    TaskDbcontext db,
    IAuditUserService auditUserService,
    Func<TaskDbcontext, DbSet<TEntity>> dbSet,
    string entityLabel) : IMasterDataService<TEntity>
    where TEntity : class, INamedMasterEntity, new()
{
    public async Task<IReadOnlyList<TEntity>> GetAllAsync(bool activeOnly)
    {
        IQueryable<TEntity> q = dbSet(db).AsNoTracking();
        if (activeOnly)
        {
            q = q.Where(e => e.IsActive);
        }

        return await q.OrderBy(e => e.Name).ToListAsync();
    }

    public async Task<TEntity?> GetByIdAsync(int id) =>
        await dbSet(db).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

    public async Task<ServiceResult<TEntity>> CreateAsync(int userId, MasterDataUpsertDto dto)
    {
        if (dto == null)
        {
            return ServiceResult<TEntity>.Fail(ServiceStatus.BadRequest);
        }

        var auditResult = await auditUserService.ValidateAndSetAuditUserAsync(userId);
        if (auditResult.Status != ServiceStatus.Success)
        {
            return ServiceResult<TEntity>.Fail(auditResult.Status, auditResult.Message);
        }

        var name = (dto.Name ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(name))
        {
            return ServiceResult<TEntity>.Fail(ServiceStatus.BadRequest, "Name is required.");
        }

        if (await dbSet(db).AnyAsync(x => x.Name == name))
        {
            return ServiceResult<TEntity>.Fail(
                ServiceStatus.Conflict,
                $"A {entityLabel} with this name already exists.");
        }

        var entity = new TEntity
        {
            Id = 0,
            Name = name,
            Description = dto.Description?.Trim() ?? string.Empty,
            IsActive = dto.IsActive,
        };
        await dbSet(db).AddAsync(entity);
        await db.SaveChangesAsync();
        return ServiceResult<TEntity>.Ok(entity);
    }

    public async Task<ServiceResult<TEntity>> UpdateAsync(int id, int userId, MasterDataUpsertDto dto)
    {
        if (dto == null)
        {
            return ServiceResult<TEntity>.Fail(ServiceStatus.BadRequest);
        }

        var auditResult = await auditUserService.ValidateAndSetAuditUserAsync(userId);
        if (auditResult.Status != ServiceStatus.Success)
        {
            return ServiceResult<TEntity>.Fail(auditResult.Status, auditResult.Message);
        }

        if (dto.Id != 0 && dto.Id != id)
        {
            return ServiceResult<TEntity>.Fail(
                ServiceStatus.BadRequest,
                "Route id and body id must match when the body includes an id.");
        }

        var name = (dto.Name ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(name))
        {
            return ServiceResult<TEntity>.Fail(ServiceStatus.BadRequest, "Name is required.");
        }

        var existing = await dbSet(db).FindAsync(id);
        if (existing == null)
        {
            return ServiceResult<TEntity>.Fail(ServiceStatus.NotFound);
        }

        if (await dbSet(db).AnyAsync(x => x.Name == name && x.Id != id))
        {
            return ServiceResult<TEntity>.Fail(
                ServiceStatus.Conflict,
                $"A {entityLabel} with this name already exists.");
        }

        existing.Name = name;
        existing.Description = dto.Description?.Trim() ?? string.Empty;
        existing.IsActive = dto.IsActive;
        await db.SaveChangesAsync();
        return ServiceResult<TEntity>.Ok(existing);
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var entity = await dbSet(db).FindAsync(id);
        if (entity == null)
        {
            return ServiceResult.Fail(ServiceStatus.NotFound);
        }

        dbSet(db).Remove(entity);
        await db.SaveChangesAsync();
        return ServiceResult.Ok();
    }
}
