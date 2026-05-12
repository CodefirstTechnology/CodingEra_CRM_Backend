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

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _context.Organizations.AsNoTracking().OrderBy(o => o.Name).ToListAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var o = await _context.Organizations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
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
            entity.LastModified = DateTime.UtcNow;
            await _context.Organizations.AddAsync(entity);
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Organization updated)
        {
            if (updated == null || id != updated.Id)
            {
                return BadRequest();
            }

            var existing = await _context.Organizations.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.Name = updated.Name;
            existing.Website = updated.Website;
            existing.Industry = updated.Industry;
            existing.AnnualRevenue = updated.AnnualRevenue;
            existing.Employees = updated.Employees;
            existing.Territory = updated.Territory;
            existing.Address = updated.Address;
            existing.LastModified = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(existing);
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
