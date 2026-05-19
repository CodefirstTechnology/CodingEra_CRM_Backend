using CRM.Business.Common;
using CRM.DATA;
using CRM.DTO;
using CRM.models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Business.Services;

public sealed class ContactService(TaskDbcontext db, IAuditUserService auditUserService) : IContactService
{
    public async Task<IReadOnlyList<Contact>> GetAllAsync(int? organizationId)
    {
        var q = db.Contacts.AsNoTracking();
        if (organizationId.HasValue)
        {
            q = q.Where(c => c.OrganizationId == organizationId);
        }

        return await q.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToListAsync();
    }

    public async Task<Contact?> GetByIdAsync(int id) =>
        await db.Contacts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

    public async Task<ServiceResult<Contact>> CreateAsync(int userId, ContactUpsertDto dto)
    {
        if (dto == null)
        {
            return ServiceResult<Contact>.Fail(ServiceStatus.BadRequest);
        }

        var auditResult = await auditUserService.ValidateAndSetAuditUserAsync(userId);
        if (auditResult.Status != ServiceStatus.Success)
        {
            return ServiceResult<Contact>.Fail(auditResult.Status, auditResult.Message);
        }

        var entity = CrmWriteMappings.ToContact(dto, 0);
        entity.Id = 0;
        await db.Contacts.AddAsync(entity);
        await db.SaveChangesAsync();
        return ServiceResult<Contact>.Ok(entity);
    }

    public async Task<ServiceResult<Contact>> UpdateAsync(int id, int userId, ContactUpsertDto dto)
    {
        if (dto == null)
        {
            return ServiceResult<Contact>.Fail(ServiceStatus.BadRequest);
        }

        var auditResult = await auditUserService.ValidateAndSetAuditUserAsync(userId);
        if (auditResult.Status != ServiceStatus.Success)
        {
            return ServiceResult<Contact>.Fail(auditResult.Status, auditResult.Message);
        }

        if (dto.Id != 0 && dto.Id != id)
        {
            return ServiceResult<Contact>.Fail(
                ServiceStatus.BadRequest,
                "Route id and body id must match when the body includes an id.");
        }

        var existing = await db.Contacts.FindAsync(id);
        if (existing == null)
        {
            return ServiceResult<Contact>.Fail(ServiceStatus.NotFound);
        }

        CrmWriteMappings.Apply(existing, dto);
        await db.SaveChangesAsync();
        return ServiceResult<Contact>.Ok(existing);
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var entity = await db.Contacts.FindAsync(id);
        if (entity == null)
        {
            return ServiceResult.Fail(ServiceStatus.NotFound);
        }

        db.Contacts.Remove(entity);
        await db.SaveChangesAsync();
        return ServiceResult.Ok();
    }
}
