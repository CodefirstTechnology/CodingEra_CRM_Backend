using CRM.DATA;
using CRM.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers.Masters
{
    [Route("api/MasterData/employee-counts")]
    [ApiController]
    public class EmployeeCountsController : ControllerBase
    {
        private readonly TaskDbcontext _context;

        public EmployeeCountsController(TaskDbcontext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false)
        {
            IQueryable<EmployeeCount> q = _context.EmployeeCounts.AsNoTracking();
            if (activeOnly)
            {
                q = q.Where(e => e.IsActive);
            }

            return Ok(await q.OrderBy(e => e.Name).ToListAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var e = await _context.EmployeeCounts.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            return e == null ? NotFound() : Ok(e);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] EmployeeCount entity)
        {
            if (entity == null)
            {
                return BadRequest();
            }

            var name = (entity.Name ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name))
            {
                return BadRequest("Name is required.");
            }

            if (await _context.EmployeeCounts.AnyAsync(x => x.Name == name))
            {
                return Conflict("An employee-count option with this name already exists.");
            }

            entity.Id = 0;
            entity.Name = name;
            entity.Description = entity.Description?.Trim() ?? string.Empty;
            await _context.EmployeeCounts.AddAsync(entity);
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] EmployeeCount updated)
        {
            if (updated == null)
            {
                return BadRequest();
            }

            if (updated.Id != 0 && updated.Id != id)
            {
                return BadRequest("Route id and body id must match when the body includes an id.");
            }

            var name = (updated.Name ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name))
            {
                return BadRequest("Name is required.");
            }

            var existing = await _context.EmployeeCounts.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            if (await _context.EmployeeCounts.AnyAsync(x => x.Name == name && x.Id != id))
            {
                return Conflict("An employee-count option with this name already exists.");
            }

            existing.Name = name;
            existing.Description = updated.Description?.Trim() ?? string.Empty;
            existing.IsActive = updated.IsActive;
            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.EmployeeCounts.FindAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            _context.EmployeeCounts.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok(new { deleted = true });
        }
    }
}
