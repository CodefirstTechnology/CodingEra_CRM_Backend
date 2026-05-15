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

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? leadSource = null, [FromQuery] string? status = null)
        {
            IQueryable<Lead> q = _context.Leads.AsNoTracking().Include(l => l.Organization);
            if (!string.IsNullOrWhiteSpace(leadSource))
            {
                q = q.Where(l => l.LeadSource == leadSource);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                q = q.Where(l => l.Status == status);
            }

            return Ok(await q.OrderByDescending(l => l.UpdatedAt).AsSplitQuery().ToListAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var l = await _context.Leads.AsNoTracking()
                .Include(l => l.Organization)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (l == null)
            {
                return NotFound();
            }

            return Ok(l);
        }

        /// <summary>Resolve by IndiaMART <c>externalRef</c> when duplicate checking imports.</summary>
        [HttpGet("by-external-ref/{externalRef}")]
        public async Task<IActionResult> GetByExternalRef(string externalRef)
        {
            var l = await _context.Leads.AsNoTracking()
                .Include(l => l.Organization)
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
            await _context.Leads.AsNoTracking()
                .Include(l => l.Organization)
                .FirstAsync(l => l.Id == id);

        private static void ApplyDtoToLeadScalars(LeadUpsertDto from, Lead to)
        {
            to.Name = from.Name;
            to.FirstName = from.FirstName;
            to.LastName = from.LastName;
            to.Salutation = from.Salutation;
            to.Gender = from.Gender;
            to.Mobile = from.Mobile;
            to.Email = from.Email;
            to.JobTitle = from.JobTitle;
            to.Status = from.Status;
            to.RequestType = from.RequestType;
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

        /// <summary>Applies <paramref name="dto"/>.<c>Organization</c> / <c>OrganizationId</c> to <paramref name="lead"/>.</summary>
        /// <returns><c>null</c> if OK, or an <see cref="IActionResult"/> error.</returns>
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
                lead.Organization = new Organization
                {
                    Id = 0,
                    Name = o.Name.Trim(),
                    Website = o.Website?.Trim() ?? string.Empty,
                    Industry = o.Industry?.Trim() ?? string.Empty,
                    AnnualRevenue = o.AnnualRevenue,
                    Employees = o.Employees?.Trim() ?? string.Empty,
                    Territory = o.Territory?.Trim() ?? string.Empty,
                    Address = o.Address?.Trim() ?? string.Empty,
                };
                lead.OrganizationId = null;
                return null;
            }

            lead.OrganizationId = dto.OrganizationId;
            return null;
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
