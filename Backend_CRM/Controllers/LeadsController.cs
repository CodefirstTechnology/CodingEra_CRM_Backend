using CRM.DATA;
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
            var now = DateTime.UtcNow;
            entity.UpdatedAt = now;
            if (entity.CreatedAt == null && string.Equals(entity.LeadSource, "IndiaMART", StringComparison.OrdinalIgnoreCase))
            {
                entity.CreatedAt = now;
            }

            await _context.Leads.AddAsync(entity);
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Lead updated)
        {
            if (updated == null || id != updated.Id)
            {
                return BadRequest();
            }

            var existing = await _context.Leads.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.Name = updated.Name;
            existing.FirstName = updated.FirstName;
            existing.LastName = updated.LastName;
            existing.Salutation = updated.Salutation;
            existing.Gender = updated.Gender;
            existing.Mobile = updated.Mobile;
            existing.Email = updated.Email;
            existing.Organization = updated.Organization;
            existing.OrganizationId = updated.OrganizationId;
            existing.Employees = updated.Employees;
            existing.AnnualRevenue = updated.AnnualRevenue;
            existing.Website = updated.Website;
            existing.Territory = updated.Territory;
            existing.Industry = updated.Industry;
            existing.JobTitle = updated.JobTitle;
            existing.Status = updated.Status;
            existing.RequestType = updated.RequestType;
            existing.Notes = updated.Notes;
            existing.Source = updated.Source;
            existing.LeadOwnerName = updated.LeadOwnerName;
            existing.Owner = updated.Owner;
            existing.LeadOwnerId = updated.LeadOwnerId;
            existing.LeadSource = updated.LeadSource;
            existing.SortTimestamp = updated.SortTimestamp;
            existing.ExternalRef = updated.ExternalRef;
            existing.Product = updated.Product;
            existing.Quantity = updated.Quantity;
            existing.Message = updated.Message;
            existing.City = updated.City;
            existing.CreatedAt = updated.CreatedAt;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(existing);
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
