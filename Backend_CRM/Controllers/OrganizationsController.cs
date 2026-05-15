using CRM.DATA;
using CRM.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers
{
    [Route("api/organizations")]
    [ApiController]
    public class OrganizationsController : ControllerBase
    {
        private readonly TaskDbcontext _context;

        public OrganizationsController(TaskDbcontext context)
        {
            _context = context;
        }

        private static IQueryable<Organization> QueryWithMasters(IQueryable<Organization> q) =>
            q.Include(o => o.Industry).Include(o => o.EmployeeCount).Include(o => o.Territory);

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await QueryWithMasters(_context.Organizations.AsNoTracking()).OrderBy(o => o.Name).ToListAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var o = await QueryWithMasters(_context.Organizations.AsNoTracking()).FirstOrDefaultAsync(x => x.Id == id);
            if (o == null)
            {
                return NotFound();
            }

            return Ok(o);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Organization entity)
        {
            if (entity == null)
            {
                return BadRequest();
            }

            entity.Id = 0;
            var err = await ValidateOrganizationMastersAsync(entity);
            if (err != null)
            {
                return err;
            }

            await _context.Organizations.AddAsync(entity);
            await _context.SaveChangesAsync();
            return Ok(await QueryWithMasters(_context.Organizations.AsNoTracking()).FirstAsync(o => o.Id == entity.Id));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Organization updated)
        {
            if (updated == null)
            {
                return BadRequest();
            }

            if (updated.Id != 0 && updated.Id != id)
            {
                return BadRequest("Route id and body id must match when the body includes an id.");
            }

            var existing = await _context.Organizations.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            var err = await ValidateOrganizationMastersAsync(updated);
            if (err != null)
            {
                return err;
            }

            existing.Name = updated.Name;
            existing.Website = updated.Website;
            existing.AnnualRevenue = updated.AnnualRevenue;
            existing.Address = updated.Address;
            existing.IndustryId = updated.IndustryId;
            existing.EmployeeCountId = updated.EmployeeCountId;
            existing.TerritoryId = updated.TerritoryId;
            await _context.SaveChangesAsync();
            return Ok(await QueryWithMasters(_context.Organizations.AsNoTracking()).FirstAsync(o => o.Id == id));
        }

        private async Task<IActionResult?> ValidateOrganizationMastersAsync(Organization o)
        {
            if (o.IndustryId is int iid && iid > 0
                && !await _context.Industries.AnyAsync(i => i.Id == iid && i.IsActive))
            {
                return BadRequest($"Industry id {iid} does not exist or is inactive.");
            }

            if (o.EmployeeCountId is int eid && eid > 0
                && !await _context.EmployeeCounts.AnyAsync(e => e.Id == eid && e.IsActive))
            {
                return BadRequest($"Employee count id {eid} does not exist or is inactive.");
            }

            if (o.TerritoryId is int tid && tid > 0
                && !await _context.Territories.AnyAsync(t => t.Id == tid && t.IsActive))
            {
                return BadRequest($"Territory id {tid} does not exist or is inactive.");
            }

            return null;
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.Organizations.FindAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            _context.Organizations.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok(new { deleted = true });
        }
    }
}
