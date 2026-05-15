using CRM.DATA;
using CRM.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers.Masters
{
    [Route("api/MasterData/request-types")]
    [ApiController]
    public class RequestTypesController : ControllerBase
    {
        private readonly TaskDbcontext _context;

        public RequestTypesController(TaskDbcontext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false)
        {
            IQueryable<RequestType> q = _context.RequestTypes.AsNoTracking();
            if (activeOnly)
            {
                q = q.Where(r => r.IsActive);
            }

            return Ok(await q.OrderBy(r => r.Name).ToListAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var r = await _context.RequestTypes.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            return r == null ? NotFound() : Ok(r);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RequestType entity)
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

            if (await _context.RequestTypes.AnyAsync(x => x.Name == name))
            {
                return Conflict("A request type with this name already exists.");
            }

            entity.Id = 0;
            entity.Name = name;
            entity.Description = entity.Description?.Trim() ?? string.Empty;
            await _context.RequestTypes.AddAsync(entity);
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] RequestType updated)
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

            var existing = await _context.RequestTypes.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            if (await _context.RequestTypes.AnyAsync(x => x.Name == name && x.Id != id))
            {
                return Conflict("A request type with this name already exists.");
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
            var entity = await _context.RequestTypes.FindAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            _context.RequestTypes.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok(new { deleted = true });
        }
    }
}
