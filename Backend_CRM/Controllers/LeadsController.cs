using CRM.DATA;
using CRM.DTO;
using CRM.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CRM.Controllers
{
    [Route("api/leads")]
    [ApiController]
    public class LeadsController : ControllerBase
    {
        private readonly TaskDbcontext _context;

        public LeadsController(TaskDbcontext context)
        {
            _context = context;
        }

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

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? leadSource = null, [FromQuery] string? status = null)
        {
            IQueryable<Lead> q = QueryWithMasters(_context.Leads.AsNoTracking());
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
                        _context.LeadStatuses.Any(ls =>
                            ls.Id == l.LeadStatusId && ls.Name.ToLower() == st.ToLower()));
                }
            }

            return Ok(await q.OrderByDescending(l => l.UpdatedAt).AsSplitQuery().ToListAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var l = await QueryWithMasters(_context.Leads.AsNoTracking())
                .FirstOrDefaultAsync(x => x.Id == id);
            if (l == null)
            {
                return NotFound();
            }

            return Ok(l);
        }

        [HttpGet("by-external-ref/{externalRef}")]
        public async Task<IActionResult> GetByExternalRef(string externalRef)
        {
            var l = await QueryWithMasters(_context.Leads.AsNoTracking())
                .FirstOrDefaultAsync(x => x.ExternalRef == externalRef);
            if (l == null)
            {
                return NotFound();
            }

            return Ok(l);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] LeadUpsertDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
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
            if (string.IsNullOrWhiteSpace(dto.ExternalRef))
            {
                entity.ExternalRef = null;
            }
            else
            {
                entity.ExternalRef = dto.ExternalRef.Trim();
                var existingByRef = await _context.Leads
                    .Include(l => l.Organization)
                    .FirstOrDefaultAsync(l => l.ExternalRef == entity.ExternalRef);
                if (existingByRef != null)
                {
                    ApplyDtoToLeadScalars(dto, existingByRef);
                    var mErr = await ApplyLeadMastersFromDtoAsync(dto, existingByRef);
                    if (mErr != null)
                    {
                        return mErr;
                    }

                    var err = await ApplyOrganizationFromDtoAsync(dto, existingByRef);
                    if (err != null)
                    {
                        return err;
                    }

                    if (dto.CreatedAt.HasValue)
                    {
                        existingByRef.CreatedAt = dto.CreatedAt;
                    }

                    await _context.SaveChangesAsync();
                    return Ok(await ReloadLeadAsync(existingByRef.Id));
                }
            }

            var now = DateTime.UtcNow;
            if (entity.CreatedAt == null && string.Equals(entity.LeadSource, "IndiaMART", StringComparison.OrdinalIgnoreCase))
            {
                entity.CreatedAt = now;
            }

            await _context.Leads.AddAsync(entity);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg
                                                 && pg.SqlState == PostgresErrorCodes.UniqueViolation
                                                 && string.Equals(pg.ConstraintName, "IX_leads_external_ref", StringComparison.Ordinal)
                                                 && !string.IsNullOrWhiteSpace(entity.ExternalRef))
            {
                _context.Entry(entity).State = EntityState.Detached;
                var recover = await _context.Leads
                    .Include(l => l.Organization)
                    .FirstOrDefaultAsync(l => l.ExternalRef == entity.ExternalRef);
                if (recover == null)
                {
                    throw;
                }

                ApplyDtoToLeadScalars(dto, recover);
                var mErr = await ApplyLeadMastersFromDtoAsync(dto, recover);
                if (mErr != null)
                {
                    return mErr;
                }

                var err = await ApplyOrganizationFromDtoAsync(dto, recover);
                if (err != null)
                {
                    return err;
                }

                if (dto.CreatedAt.HasValue)
                {
                    recover.CreatedAt = dto.CreatedAt;
                }

                await _context.SaveChangesAsync();
                return Ok(await ReloadLeadAsync(recover.Id));
            }

            return Ok(await ReloadLeadAsync(entity.Id));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] LeadUpsertDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            if (dto.Id != 0 && dto.Id != id)
            {
                return BadRequest("Route id and body id must match when the body includes an id.");
            }

            var existing = await _context.Leads
                .Include(l => l.Organization)
                .FirstOrDefaultAsync(l => l.Id == id);
            if (existing == null)
            {
                return NotFound();
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

            await _context.SaveChangesAsync();
            return Ok(await ReloadLeadAsync(existing.Id));
        }

        private async Task<Lead> ReloadLeadAsync(int id) =>
            await QueryWithMasters(_context.Leads.AsNoTracking()).FirstAsync(l => l.Id == id);

        private static void ApplyDtoToLeadScalars(LeadUpsertDto from, Lead to)
        {
            to.Name = from.Name;
            to.FirstName = from.FirstName;
            to.LastName = from.LastName;
            to.Gender = from.Gender;
            to.Mobile = from.Mobile;
            to.Email = from.Email;
            to.JobTitle = from.JobTitle;
            to.Notes = from.Notes;
            to.Source = from.Source;
            to.LeadOwnerName = from.LeadOwnerName;
            to.Owner = from.Owner;
            to.LeadOwnerId = from.LeadOwnerId;
            to.LeadSource = from.LeadSource;
            to.SortTimestamp = from.SortTimestamp;
            to.ExternalRef = string.IsNullOrWhiteSpace(from.ExternalRef) ? null : from.ExternalRef.Trim();
            to.Product = from.Product;
            to.Quantity = from.Quantity;
            to.Message = from.Message;
            to.City = from.City;
        }

        /// <summary>Maps DTO master fields to lead FKs (IDs preferred; legacy names resolved against master tables).</summary>
        private async Task<IActionResult?> ApplyLeadMastersFromDtoAsync(LeadUpsertDto dto, Lead lead)
        {
            lead.Salutation = null;
            lead.LeadStatus = null;
            lead.RequestType = null;

            if (dto.SalutationId is int sid && sid > 0)
            {
                if (!await _context.Salutations.AnyAsync(s => s.Id == sid && s.IsActive))
                {
                    return BadRequest($"Salutation id {sid} does not exist or is inactive.");
                }

                lead.SalutationId = sid;
            }
            else
            {
                lead.SalutationId = await ResolveNameToIdAsync(_context.Salutations, dto.Salutation, requireActive: true);
            }

            if (dto.LeadStatusId is int lstid && lstid > 0)
            {
                if (!await _context.LeadStatuses.AnyAsync(s => s.Id == lstid && s.IsActive))
                {
                    return BadRequest($"Lead status id {lstid} does not exist or is inactive.");
                }

                lead.LeadStatusId = lstid;
            }
            else
            {
                lead.LeadStatusId = await ResolveNameToIdAsync(_context.LeadStatuses, dto.Status, requireActive: true);
            }

            if (dto.RequestTypeId is int rtid && rtid > 0)
            {
                if (!await _context.RequestTypes.AnyAsync(r => r.Id == rtid && r.IsActive))
                {
                    return BadRequest($"Request type id {rtid} does not exist or is inactive.");
                }

                lead.RequestTypeId = rtid;
            }
            else
            {
                lead.RequestTypeId = await ResolveNameToIdAsync(_context.RequestTypes, dto.RequestType, requireActive: true);
            }

            return null;
        }

        private async Task<IActionResult?> ApplyOrganizationFromDtoAsync(LeadUpsertDto dto, Lead lead)
        {
            lead.Organization = null;

            if (dto.Organization != null && dto.Organization.Id is int linkId && linkId > 0)
            {
                var exists = await _context.Organizations.AnyAsync(o => o.Id == linkId);
                if (!exists)
                {
                    return BadRequest($"Organization id {linkId} does not exist.");
                }

                lead.OrganizationId = linkId;
                return null;
            }

            if (dto.Organization != null && !string.IsNullOrWhiteSpace(dto.Organization.Name))
            {
                var o = dto.Organization;
                var org = new Organization
                {
                    Id = 0,
                    Name = o.Name.Trim(),
                    Website = o.Website?.Trim() ?? string.Empty,
                    AnnualRevenue = o.AnnualRevenue,
                    Address = o.Address?.Trim() ?? string.Empty,
                };

                if (o.IndustryId is int iid && iid > 0)
                {
                    if (!await _context.Industries.AnyAsync(i => i.Id == iid && i.IsActive))
                    {
                        return BadRequest($"Industry id {iid} does not exist or is inactive.");
                    }

                    org.IndustryId = iid;
                }
                else
                {
                    org.IndustryId = await ResolveNameToIdAsync(_context.Industries, o.Industry, requireActive: true);
                }

                if (o.EmployeeCountId is int ecid && ecid > 0)
                {
                    if (!await _context.EmployeeCounts.AnyAsync(e => e.Id == ecid && e.IsActive))
                    {
                        return BadRequest($"Employee count id {ecid} does not exist or is inactive.");
                    }

                    org.EmployeeCountId = ecid;
                }
                else
                {
                    org.EmployeeCountId = await ResolveNameToIdAsync(_context.EmployeeCounts, o.Employees, requireActive: true);
                }

                if (o.TerritoryId is int tid && tid > 0)
                {
                    if (!await _context.Territories.AnyAsync(t => t.Id == tid && t.IsActive))
                    {
                        return BadRequest($"Territory id {tid} does not exist or is inactive.");
                    }

                    org.TerritoryId = tid;
                }
                else
                {
                    org.TerritoryId = await ResolveNameToIdAsync(_context.Territories, o.Territory, requireActive: true);
                }

                lead.Organization = org;
                lead.OrganizationId = null;
                return null;
            }

            lead.OrganizationId = dto.OrganizationId;
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

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.Leads.FindAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            _context.Leads.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok(new { deleted = true });
        }
    }
}
