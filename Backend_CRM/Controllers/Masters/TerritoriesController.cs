using CRM.DATA;
using CRM.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers.Masters
{
    [Route("api/MasterData/territories")]
    [ApiController]
    public class TerritoriesController : ControllerBase
    {
        private readonly TaskDbcontext _context;

        public TerritoriesController(TaskDbcontext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false)
        {
            IQueryable<Territory> q = _context.Territories.AsNoTracking();
            if (activeOnly)
            {
                q = q.Where(t => t.IsActive);
            }

            return Ok(await q.OrderBy(t => t.Name).ToListAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var t = await _context.Territories.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            return t == null ? NotFound() : Ok(t);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Territory entity)
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

            if (await _context.Territories.AnyAsync(x => x.Name == name))
            {
                return Conflict("A territory with this name already exists.");
            }

            entity.Id = 0;
            entity.Name = name;
            entity.Description = entity.Description?.Trim() ?? string.Empty;
            await _context.Territories.AddAsync(entity);
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Territory updated)
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

            var existing = await _context.Territories.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            if (await _context.Territories.AnyAsync(x => x.Name == name && x.Id != id))
            {
                return Conflict("A territory with this name already exists.");
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
            var entity = await _context.Territories.FindAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            _context.Territories.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok(new { deleted = true });
        }
    }
}
