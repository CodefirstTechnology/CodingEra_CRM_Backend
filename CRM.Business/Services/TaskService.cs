using CRM.Business.Common;
using CRM.DATA;
using CRM.DTO;
using CRM.models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Business.Services;

public sealed class TaskService(TaskDbcontext db, IAuditUserService auditUserService) : ITaskService
{
    public async Task<IReadOnlyList<TaskTable>> GetAllAsync(int? relatedLeadId, int? relatedDealId)
    {
        var q = db.Tasks.AsNoTracking();
        if (relatedLeadId.HasValue)
        {
            q = q.Where(t => t.RelatedLeadId == relatedLeadId);
        }

        if (relatedDealId.HasValue)
        {
            q = q.Where(t => t.RelatedDealId == relatedDealId);
        }

        return await q.OrderByDescending(t => t.LastModified).ToListAsync();
    }

    public async Task<TaskTable?> GetByIdAsync(int id) =>
        await db.Tasks.AsNoTracking().FirstOrDefaultAsync(x => x.TaskId == id);

    public async Task<ServiceResult<TaskTable>> CreateAsync(int userId, TaskUpsertDto dto)
    {
        if (dto == null)
        {
            return ServiceResult<TaskTable>.Fail(ServiceStatus.BadRequest);
        }

        var auditResult = await auditUserService.ValidateAndSetAuditUserAsync(userId);
        if (auditResult.Status != ServiceStatus.Success)
        {
            return ServiceResult<TaskTable>.Fail(auditResult.Status, auditResult.Message);
        }

        var entity = CrmWriteMappings.ToTask(dto, 0);
        entity.TaskId = 0;
        await db.Tasks.AddAsync(entity);
        await db.SaveChangesAsync();
        return ServiceResult<TaskTable>.Ok(entity);
    }

    public async Task<ServiceResult<TaskTable>> UpdateAsync(int id, int userId, TaskUpsertDto dto)
    {
        if (dto == null)
        {
            return ServiceResult<TaskTable>.Fail(ServiceStatus.BadRequest);
        }

        var auditResult = await auditUserService.ValidateAndSetAuditUserAsync(userId);
        if (auditResult.Status != ServiceStatus.Success)
        {
            return ServiceResult<TaskTable>.Fail(auditResult.Status, auditResult.Message);
        }

        if (dto.TaskId != 0 && dto.TaskId != id)
        {
            return ServiceResult<TaskTable>.Fail(
                ServiceStatus.BadRequest,
                "Route id and body taskId must match when the body includes a task id.");
        }

        var existing = await db.Tasks.FindAsync(id);
        if (existing == null)
        {
            return ServiceResult<TaskTable>.Fail(ServiceStatus.NotFound);
        }

        CrmWriteMappings.Apply(existing, dto);
        await db.SaveChangesAsync();
        return ServiceResult<TaskTable>.Ok(existing);
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var entity = await db.Tasks.FindAsync(id);
        if (entity == null)
        {
            return ServiceResult.Fail(ServiceStatus.NotFound);
        }

        db.Tasks.Remove(entity);
        await db.SaveChangesAsync();
        return ServiceResult.Ok();
    }
}
