using CRM.Business.Common;
using CRM.DATA;
using CRM.DTO;
using CRM.models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Business.Services;

public sealed class OrganizationService(TaskDbcontext db, IAuditUserService auditUserService) : IOrganizationService
{
    private static IQueryable<Organization> QueryWithMasters(IQueryable<Organization> q) =>
        q.Include(o => o.Industry).Include(o => o.EmployeeCount).Include(o => o.Territory);

    public async Task<IReadOnlyList<Organization>> GetAllAsync() =>
        await QueryWithMasters(db.Organizations.AsNoTracking()).OrderBy(o => o.Name).ToListAsync();

    public async Task<Organization?> GetByIdAsync(int id) =>
        await QueryWithMasters(db.Organizations.AsNoTracking()).FirstOrDefaultAsync(x => x.Id == id);

    public async Task<ServiceResult<Organization>> CreateAsync(int userId, OrganizationUpsertDto dto)
    {
        if (dto == null)
        {
            return ServiceResult<Organization>.Fail(ServiceStatus.BadRequest);
        }

        var auditResult = await auditUserService.ValidateAndSetAuditUserAsync(userId);
        if (auditResult.Status != ServiceStatus.Success)
        {
            return ServiceResult<Organization>.Fail(auditResult.Status, auditResult.Message);
        }

        var entity = MapDtoToNewOrganization(dto);
        if (string.IsNullOrWhiteSpace(entity.Name))
        {
            return ServiceResult<Organization>.Fail(ServiceStatus.BadRequest, "Name is required.");
        }

        var err = await ValidateOrganizationMasterIdsAsync(
            entity.IndustryId,
            entity.EmployeeCountId,
            entity.TerritoryId);
        if (err != null)
        {
            return err;
        }

        await db.Organizations.AddAsync(entity);
        await db.SaveChangesAsync();
        var created = await QueryWithMasters(db.Organizations.AsNoTracking()).FirstAsync(o => o.Id == entity.Id);
        return ServiceResult<Organization>.Ok(created);
    }

    public async Task<ServiceResult<Organization>> UpdateAsync(int id, int userId, OrganizationUpsertDto dto)
    {
        if (dto == null)
        {
            return ServiceResult<Organization>.Fail(ServiceStatus.BadRequest);
        }

        var auditResult = await auditUserService.ValidateAndSetAuditUserAsync(userId);
        if (auditResult.Status != ServiceStatus.Success)
        {
            return ServiceResult<Organization>.Fail(auditResult.Status, auditResult.Message);
        }

        if (dto.Id != 0 && dto.Id != id)
        {
            return ServiceResult<Organization>.Fail(
                ServiceStatus.BadRequest,
                "Route id and body id must match when the body includes an id.");
        }

        var existing = await db.Organizations.FindAsync(id);
        if (existing == null)
        {
            return ServiceResult<Organization>.Fail(ServiceStatus.NotFound);
        }

        ApplyDtoToOrganization(dto, existing);
        if (string.IsNullOrWhiteSpace(existing.Name))
        {
            return ServiceResult<Organization>.Fail(ServiceStatus.BadRequest, "Name is required.");
        }

        var err = await ValidateOrganizationMasterIdsAsync(
            existing.IndustryId,
            existing.EmployeeCountId,
            existing.TerritoryId);
        if (err != null)
        {
            return err;
        }

        await db.SaveChangesAsync();
        var updated = await QueryWithMasters(db.Organizations.AsNoTracking()).FirstAsync(o => o.Id == id);
        return ServiceResult<Organization>.Ok(updated);
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var entity = await db.Organizations.FindAsync(id);
        if (entity == null)
        {
            return ServiceResult.Fail(ServiceStatus.NotFound);
        }

        db.Organizations.Remove(entity);
        await db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    private static Organization MapDtoToNewOrganization(OrganizationUpsertDto dto)
    {
        var o = new Organization { Id = 0 };
        ApplyDtoToOrganization(dto, o);
        return o;
    }

    private static void ApplyDtoToOrganization(OrganizationUpsertDto dto, Organization o)
    {
        o.Name = (dto.Name ?? string.Empty).Trim();
        o.Website = (dto.Website ?? string.Empty).Trim();
        o.AnnualRevenue = dto.AnnualRevenue;
        o.IndustryId = NormalizeFk(dto.IndustryId);
        o.EmployeeCountId = NormalizeFk(dto.EmployeeCountId);
        o.TerritoryId = NormalizeFk(dto.TerritoryId);
    }

    private static int? NormalizeFk(int? id) => id is > 0 ? id : null;

    private async Task<ServiceResult<Organization>?> ValidateOrganizationMasterIdsAsync(
        int? industryId,
        int? employeeCountId,
        int? territoryId)
    {
        if (industryId is int iid
            && !await db.Industries.AnyAsync(i => i.Id == iid && i.IsActive))
        {
            return ServiceResult<Organization>.Fail(
                ServiceStatus.BadRequest,
                $"Industry id {iid} does not exist or is inactive.");
        }

        if (employeeCountId is int eid
            && !await db.EmployeeCounts.AnyAsync(e => e.Id == eid && e.IsActive))
        {
            return ServiceResult<Organization>.Fail(
                ServiceStatus.BadRequest,
                $"Employee count id {eid} does not exist or is inactive.");
        }

        if (territoryId is int tid
            && !await db.Territories.AnyAsync(t => t.Id == tid && t.IsActive))
        {
            return ServiceResult<Organization>.Fail(
                ServiceStatus.BadRequest,
                $"Territory id {tid} does not exist or is inactive.");
        }

        return null;
    }
}
