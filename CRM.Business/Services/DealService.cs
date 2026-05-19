using CRM.Business.Common;
using CRM.DATA;
using CRM.DTO;
using CRM.models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Business.Services;

public sealed class DealService(TaskDbcontext db, IAuditUserService auditUserService) : IDealService
{
    public async Task<IReadOnlyList<Deal>> GetAllAsync(string? status)
    {
        var q = db.Deals.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            q = q.Where(d => d.Status == status);
        }

        return await q.OrderByDescending(d => d.LastModified).ToListAsync();
    }

    public async Task<Deal?> GetByIdAsync(int id) =>
        await db.Deals.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

    public async Task<ServiceResult<Deal>> CreateAsync(int userId, DealUpsertDto dto)
    {
        if (dto == null)
        {
            return ServiceResult<Deal>.Fail(ServiceStatus.BadRequest);
        }

        var auditResult = await auditUserService.ValidateAndSetAuditUserAsync(userId);
        if (auditResult.Status != ServiceStatus.Success)
        {
            return ServiceResult<Deal>.Fail(auditResult.Status, auditResult.Message);
        }

        var entity = CrmWriteMappings.ToDeal(dto, 0);
        entity.Id = 0;
        await db.Deals.AddAsync(entity);
        await db.SaveChangesAsync();
        return ServiceResult<Deal>.Ok(entity);
    }

    public async Task<ServiceResult<Deal>> UpdateAsync(int id, int userId, DealUpsertDto dto)
    {
        if (dto == null)
        {
            return ServiceResult<Deal>.Fail(ServiceStatus.BadRequest);
        }

        var auditResult = await auditUserService.ValidateAndSetAuditUserAsync(userId);
        if (auditResult.Status != ServiceStatus.Success)
        {
            return ServiceResult<Deal>.Fail(auditResult.Status, auditResult.Message);
        }

        if (dto.Id != 0 && dto.Id != id)
        {
            return ServiceResult<Deal>.Fail(
                ServiceStatus.BadRequest,
                "Route id and body id must match when the body includes an id.");
        }

        var existing = await db.Deals.FindAsync(id);
        if (existing == null)
        {
            return ServiceResult<Deal>.Fail(ServiceStatus.NotFound);
        }

        CrmWriteMappings.Apply(existing, dto);
        await db.SaveChangesAsync();
        return ServiceResult<Deal>.Ok(existing);
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var entity = await db.Deals.FindAsync(id);
        if (entity == null)
        {
            return ServiceResult.Fail(ServiceStatus.NotFound);
        }

        db.Deals.Remove(entity);
        await db.SaveChangesAsync();
        return ServiceResult.Ok();
    }
}
