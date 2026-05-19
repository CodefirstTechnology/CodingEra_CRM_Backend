using CRM.Business.Common;
using CRM.DATA;
using CRM.DTO;
using CRM.models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Business.Services;

public sealed class LeadService(TaskDbcontext db, IAuditUserService auditUserService) : ILeadService
{
    private static IQueryable<Lead> QueryWithMasters(IQueryable<Lead> q) =>
        q.Include(l => l.Salutation)
            .Include(l => l.LeadStatus)
            .Include(l => l.RequestType)
            .Include(l => l.Organization)
            .ThenInclude(o => o!.Industry)
            .Include(l => l.Organization)
            .ThenInclude(o => o!.EmployeeCount)
            .Include(l => l.Organization)
            .ThenInclude(o => o!.Territory);

    public async Task<IReadOnlyList<Lead>> GetAllAsync(string? leadSource, string? status)
    {
        IQueryable<Lead> q = QueryWithMasters(db.Leads.AsNoTracking());
        if (!string.IsNullOrWhiteSpace(leadSource))
        {
            q = q.Where(l => l.LeadSource == leadSource);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (int.TryParse(status, out var statusId))
            {
                q = q.Where(l => l.LeadStatusId == statusId);
            }
            else
            {
                var st = status.Trim();
                q = q.Where(l =>
                    db.LeadStatuses.Any(ls =>
                        ls.Id == l.LeadStatusId && ls.Name.ToLower() == st.ToLower()));
            }
        }

        return await q.OrderByDescending(l => l.UpdatedAt).AsSplitQuery().ToListAsync();
    }

    public async Task<Lead?> GetByIdAsync(int id) =>
        await QueryWithMasters(db.Leads.AsNoTracking()).FirstOrDefaultAsync(x => x.Id == id);

    public async Task<ServiceResult<IReadOnlyList<LeadHistory>>> GetHistoryAsync(int id)
    {
        if (!await db.Leads.AsNoTracking().AnyAsync(l => l.Id == id))
        {
            return ServiceResult<IReadOnlyList<LeadHistory>>.Fail(ServiceStatus.NotFound);
        }

        var rows = await db.LeadHistories.AsNoTracking()
            .Where(h => h.LeadId == id)
            .OrderByDescending(h => h.ArchivedAt)
            .ThenByDescending(h => h.Id)
            .ToListAsync();
        return ServiceResult<IReadOnlyList<LeadHistory>>.Ok(rows);
    }

    public async Task<ServiceResult<Lead>> CreateAsync(int userId, LeadUpsertDto dto)
    {
        if (dto == null)
        {
            return ServiceResult<Lead>.Fail(ServiceStatus.BadRequest);
        }

        var auditResult = await auditUserService.ValidateAndSetAuditUserAsync(userId);
        if (auditResult.Status != ServiceStatus.Success)
        {
            return ServiceResult<Lead>.Fail(auditResult.Status, auditResult.Message);
        }

        var entity = new Lead();
        ApplyDtoToLeadScalars(dto, entity);
        var masterErr = await ApplyLeadMastersFromDtoAsync(dto, entity);
        if (masterErr != null)
        {
            return masterErr;
        }

        var orgError = await ApplyOrganizationFromDtoAsync(dto, entity);
        if (orgError != null)
        {
            return orgError;
        }

        entity.Id = 0;
        var now = DateTime.UtcNow;
        if (entity.CreatedAt == null && string.Equals(entity.LeadSource, "IndiaMART", StringComparison.OrdinalIgnoreCase))
        {
            entity.CreatedAt = now;
        }

        await db.Leads.AddAsync(entity);
        await db.SaveChangesAsync();
        return ServiceResult<Lead>.Ok(await ReloadLeadAsync(entity.Id));
    }

    public async Task<ServiceResult<Lead>> UpdateAsync(int id, int userId, LeadUpsertDto dto)
    {
        if (dto == null)
        {
            return ServiceResult<Lead>.Fail(ServiceStatus.BadRequest);
        }

        var auditResult = await auditUserService.ValidateAndSetAuditUserAsync(userId);
        if (auditResult.Status != ServiceStatus.Success)
        {
            return ServiceResult<Lead>.Fail(auditResult.Status, auditResult.Message);
        }

        if (dto.Id != 0 && dto.Id != id)
        {
            return ServiceResult<Lead>.Fail(
                ServiceStatus.BadRequest,
                "Route id and body id must match when the body includes an id.");
        }

        var existing = await db.Leads
            .Include(l => l.Organization)
            .FirstOrDefaultAsync(l => l.Id == id);
        if (existing == null)
        {
            return ServiceResult<Lead>.Fail(ServiceStatus.NotFound);
        }

        ApplyDtoToLeadScalars(dto, existing);
        var masterErr = await ApplyLeadMastersFromDtoAsync(dto, existing);
        if (masterErr != null)
        {
            return masterErr;
        }

        var orgError = await ApplyOrganizationFromDtoAsync(dto, existing);
        if (orgError != null)
        {
            return orgError;
        }

        existing.CreatedAt = dto.CreatedAt;
        await db.SaveChangesAsync();
        return ServiceResult<Lead>.Ok(await ReloadLeadAsync(existing.Id));
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var entity = await db.Leads.FindAsync(id);
        if (entity == null)
        {
            return ServiceResult.Fail(ServiceStatus.NotFound);
        }

        db.Leads.Remove(entity);
        await db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    private async Task<Lead> ReloadLeadAsync(int id) =>
        await QueryWithMasters(db.Leads.AsNoTracking()).FirstAsync(l => l.Id == id);

    private static void ApplyDtoToLeadScalars(LeadUpsertDto from, Lead to)
    {
        to.FirstName = from.FirstName;
        to.LastName = from.LastName;
        to.Gender = from.Gender;
        to.Mobile = from.Mobile;
        to.Email = from.Email;
        to.Notes = from.Notes;
        to.LeadOwnerId = from.LeadOwnerId;
        to.LeadSource = from.LeadSource;
        to.CreatedAt = from.CreatedAt;
    }

    private async Task<ServiceResult<Lead>?> ApplyLeadMastersFromDtoAsync(LeadUpsertDto dto, Lead lead)
    {
        lead.Salutation = null;
        lead.LeadStatus = null;
        lead.RequestType = null;

        if (dto.SalutationId is int sid && sid > 0)
        {
            if (!await db.Salutations.AnyAsync(s => s.Id == sid && s.IsActive))
            {
                return ServiceResult<Lead>.Fail(
                    ServiceStatus.BadRequest,
                    $"Salutation id {sid} does not exist or is inactive.");
            }

            lead.SalutationId = sid;
        }
        else
        {
            lead.SalutationId = null;
        }

        if (dto.LeadStatusId is int lstid && lstid > 0)
        {
            if (!await db.LeadStatuses.AnyAsync(s => s.Id == lstid && s.IsActive))
            {
                return ServiceResult<Lead>.Fail(
                    ServiceStatus.BadRequest,
                    $"Lead status id {lstid} does not exist or is inactive.");
            }

            lead.LeadStatusId = lstid;
        }
        else
        {
            lead.LeadStatusId = await ResolveNameToIdAsync(db.LeadStatuses, dto.Status, requireActive: true);
        }

        if (dto.RequestTypeId is int rtid && rtid > 0)
        {
            if (!await db.RequestTypes.AnyAsync(r => r.Id == rtid && r.IsActive))
            {
                return ServiceResult<Lead>.Fail(
                    ServiceStatus.BadRequest,
                    $"Request type id {rtid} does not exist or is inactive.");
            }

            lead.RequestTypeId = rtid;
        }
        else
        {
            lead.RequestTypeId = null;
        }

        return null;
    }

    private async Task<ServiceResult<Lead>?> ApplyOrganizationFromDtoAsync(LeadUpsertDto dto, Lead lead)
    {
        lead.Organization = null;

        if (dto.OrganizationId is int oid && oid > 0)
        {
            var exists = await db.Organizations.AnyAsync(o => o.Id == oid);
            if (!exists)
            {
                return ServiceResult<Lead>.Fail(
                    ServiceStatus.BadRequest,
                    $"Organization id {oid} does not exist.");
            }

            lead.OrganizationId = oid;
            return null;
        }

        lead.OrganizationId = null;
        return null;
    }

    private static async Task<int?> ResolveNameToIdAsync<TEntity>(
        DbSet<TEntity> set,
        string? name,
        bool requireActive)
        where TEntity : class
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var trimmed = name.Trim();
        var tl = trimmed.ToLowerInvariant();
        var q = set.AsNoTracking().Where(e => EF.Property<string>(e, "Name").ToLower() == tl);
        if (requireActive)
        {
            q = q.Where(e => EF.Property<bool>(e, "IsActive"));
        }

        return await q.Select(e => (int?)EF.Property<int>(e, "Id")).FirstOrDefaultAsync();
    }
}
