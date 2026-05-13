using CRM.DATA;
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
            var q = _context.Leads.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(leadSource))
            {
                q = q.Where(l => l.LeadSource == leadSource);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                q = q.Where(l => l.Status == status);
            }

            return Ok(await q.OrderByDescending(l => l.UpdatedAt).ToListAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var l = await _context.Leads.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
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
                .FirstOrDefaultAsync(x => x.ExternalRef == externalRef);
            if (l == null)
            {
                return NotFound();
            }

            return Ok(l);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Lead entity)
        {
            if (entity == null)
            {
                return BadRequest();
            }

            entity.Id = 0;
            if (string.IsNullOrWhiteSpace(entity.ExternalRef))
            {
                entity.ExternalRef = null;
            }
            else
            {
                entity.ExternalRef = entity.ExternalRef.Trim();
                var existingByRef = await _context.Leads
                    .FirstOrDefaultAsync(l => l.ExternalRef == entity.ExternalRef);
                if (existingByRef != null)
                {
                    ApplyLeadScalars(entity, existingByRef);
                    if (entity.CreatedAt.HasValue)
                    {
                        existingByRef.CreatedAt = entity.CreatedAt;
                    }

                    existingByRef.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return Ok(existingByRef);
                }
            }

            var now = DateTime.UtcNow;
            entity.UpdatedAt = now;
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
                    .FirstOrDefaultAsync(l => l.ExternalRef == entity.ExternalRef);
                if (recover == null)
                {
                    throw;
                }

                ApplyLeadScalars(entity, recover);
                if (entity.CreatedAt.HasValue)
                {
                    recover.CreatedAt = entity.CreatedAt;
                }

                recover.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(recover);
            }

            return Ok(entity);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Lead updated)
        {
            if (updated == null)
            {
                return BadRequest();
            }

            if (updated.Id != 0 && updated.Id != id)
            {
                return BadRequest("Route id and body id must match when the body includes an id.");
            }

            var existing = await _context.Leads.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            ApplyLeadScalars(updated, existing);
            existing.CreatedAt = updated.CreatedAt;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        /// <summary>Copies mutable fields from <paramref name="from"/> onto <paramref name="to"/> (does not set timestamps or <c>CreatedAt</c>).</summary>
        private static void ApplyLeadScalars(Lead from, Lead to)
        {
            to.Name = from.Name;
            to.FirstName = from.FirstName;
            to.LastName = from.LastName;
            to.Salutation = from.Salutation;
            to.Gender = from.Gender;
            to.Mobile = from.Mobile;
            to.Email = from.Email;
            to.Organization = from.Organization;
            to.OrganizationId = from.OrganizationId;
            to.Employees = from.Employees;
            to.AnnualRevenue = from.AnnualRevenue;
            to.Website = from.Website;
            to.Territory = from.Territory;
            to.Industry = from.Industry;
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
            to.ExternalRef = from.ExternalRef;
            to.Product = from.Product;
            to.Quantity = from.Quantity;
            to.Message = from.Message;
            to.City = from.City;
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
