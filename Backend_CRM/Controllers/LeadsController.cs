using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        public async Task<IActionResult> GetAll([FromQuery] int userId, [FromQuery] string? leadSource = null, [FromQuery] string? status = null)
        {
            _ = userId;
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
        public async Task<IActionResult> GetById(int id, [FromQuery] int userId)
        {
            _ = userId;
            var l = await QueryWithMasters(_context.Leads.AsNoTracking())
                .FirstOrDefaultAsync(x => x.Id == id);
            if (l == null)
            {
                return NotFound();
            }

            return Ok(l);
        }

        /// <summary>Prior versions of this lead (newest first). Each row is the full scalar snapshot before an update.</summary>
        [HttpGet("{id:int}/history")]
        public async Task<IActionResult> GetHistory(int id, [FromQuery] int userId)
        {
            _ = userId;
            if (!await _context.Leads.AsNoTracking().AnyAsync(l => l.Id == id))
            {
                return NotFound();
            }

            var rows = await _context.LeadHistories.AsNoTracking()
                .Where(h => h.LeadId == id)
                .OrderByDescending(h => h.ArchivedAt)
                .ThenByDescending(h => h.Id)
                .ToListAsync();
            return Ok(rows);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromQuery] int userId, [FromBody] LeadUpsertDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(_context, userId);
            if (auditErr != null)
            {
                return auditErr;
            }

            AuditUserValidation.SetAuditUser(_context, userId);

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

            await _context.Leads.AddAsync(entity);
            await _context.SaveChangesAsync();
            return Ok(await ReloadLeadAsync(entity.Id));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromQuery] int userId, [FromBody] LeadUpsertDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(_context, userId);
            if (auditErr != null)
            {
                return auditErr;
            }

            AuditUserValidation.SetAuditUser(_context, userId);

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
                lead.SalutationId = null;
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
                lead.RequestTypeId = null;
            }

            return null;
        }

        private async Task<IActionResult?> ApplyOrganizationFromDtoAsync(LeadUpsertDto dto, Lead lead)
        {
            lead.Organization = null;

            if (dto.OrganizationId is int oid && oid > 0)
            {
                var exists = await _context.Organizations.AnyAsync(o => o.Id == oid);
                if (!exists)
                {
                    return BadRequest($"Organization id {oid} does not exist.");
                }

                lead.OrganizationId = oid;
                return null;
            }

            if (!dto.OrganizationId.HasValue)
            {
                var linkedByName = await TryLinkOrganizationByNameAsync(dto.OrganizationName, lead);
                if (linkedByName != null)
                {
                    return linkedByName;
                }

                // Partial PUT often omits organizationId; keep the existing FK when no name to resolve.
                return null;
            }

            lead.OrganizationId = null;
            return null;
        }

        private async Task<IActionResult?> TryLinkOrganizationByNameAsync(string? organizationName, Lead lead)
        {
            if (string.IsNullOrWhiteSpace(organizationName))
            {
                return null;
            }

            var orgId = await ResolveOrganizationIdByNameAsync(organizationName);
            if (orgId is not > 0)
            {
                return null;
            }

            lead.OrganizationId = orgId;
            return null;
        }

        private async Task<int?> ResolveOrganizationIdByNameAsync(string name)
        {
            var trimmed = name.Trim();
            if (trimmed.Length == 0)
            {
                return null;
            }

            var tl = trimmed.ToLowerInvariant();
            return await _context.Organizations
                .AsNoTracking()
                .Where(o => o.Name.ToLower() == tl)
                .OrderBy(o => o.Id)
                .Select(o => (int?)o.Id)
                .FirstOrDefaultAsync();
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
