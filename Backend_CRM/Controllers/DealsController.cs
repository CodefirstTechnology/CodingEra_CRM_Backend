using CRM.DATA;
using CRM.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers
{
    [Route("api/deals")]
    [ApiController]
    public class DealsController : ControllerBase
    {
        private readonly TaskDbcontext _context;

        public DealsController(TaskDbcontext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? status = null)
        {
            var q = _context.Deals.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(status))
            {
                q = q.Where(d => d.Status == status);
            }

            return Ok(await q.OrderByDescending(d => d.LastModified).ToListAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var d = await _context.Deals.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (d == null)
            {
                return NotFound();
            }

            return Ok(d);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Deal entity)
        {
            if (entity == null)
            {
                return BadRequest();
            }

            entity.Id = 0;
            await _context.Deals.AddAsync(entity);
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Deal updated)
        {
            if (updated == null)
            {
                return BadRequest();
            }

            if (updated.Id != 0 && updated.Id != id)
            {
                return BadRequest("Route id and body id must match when the body includes an id.");
            }

            var existing = await _context.Deals.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.OrganizationId = updated.OrganizationId;
            existing.ContactId = updated.ContactId;
            existing.OrganizationName = updated.OrganizationName;
            existing.Salutation = updated.Salutation;
            existing.FirstName = updated.FirstName;
            existing.LastName = updated.LastName;
            existing.Email = updated.Email;
            existing.Mobile = updated.Mobile;
            existing.Gender = updated.Gender;
            existing.AnnualRevenue = updated.AnnualRevenue;
            existing.Employees = updated.Employees;
            existing.Website = updated.Website;
            existing.Territory = updated.Territory;
            existing.Industry = updated.Industry;
            existing.Status = updated.Status;
            existing.DealOwnerId = updated.DealOwnerId;
            existing.AssignedToUserId = updated.AssignedToUserId;
            existing.AssignedInitials = updated.AssignedInitials;
            existing.RelatedContactId = updated.RelatedContactId;
            existing.RelatedOrganizationId = updated.RelatedOrganizationId;
            existing.ProbabilityPercent = updated.ProbabilityPercent;
            existing.NextStep = updated.NextStep;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.Deals.FindAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            _context.Deals.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok(new { deleted = true });
        }
    }
}
