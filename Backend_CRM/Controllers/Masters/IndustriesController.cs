using CRM.DATA;
using CRM.DTO;
using CRM.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers.Masters
{
    [Route("api/MasterData/industries")]
    [ApiController]
    public class IndustriesController : ControllerBase
    {
        private readonly TaskDbcontext _context;

        public IndustriesController(TaskDbcontext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false)
        {
            IQueryable<Industry> q = _context.Industries.AsNoTracking();
            if (activeOnly)
            {
                q = q.Where(i => i.IsActive);
            }

            return Ok(await q.OrderBy(i => i.Name).ToListAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var i = await _context.Industries.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            return i == null ? NotFound() : Ok(i);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] MasterDataUpsertDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            var name = (dto.Name ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name))
            {
                return BadRequest("Name is required.");
            }

            if (await _context.Industries.AnyAsync(x => x.Name == name))
            {
                return Conflict("An industry with this name already exists.");
            }

            var entity = new Industry
            {
                Id = 0,
                Name = name,
                Description = dto.Description?.Trim() ?? string.Empty,
                IsActive = dto.IsActive,
            };
            await _context.Industries.AddAsync(entity);
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] MasterDataUpsertDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            if (dto.Id != 0 && dto.Id != id)
            {
                return BadRequest("Route id and body id must match when the body includes an id.");
            }

            var name = (dto.Name ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name))
            {
                return BadRequest("Name is required.");
            }

            var existing = await _context.Industries.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            if (await _context.Industries.AnyAsync(x => x.Name == name && x.Id != id))
            {
                return Conflict("An industry with this name already exists.");
            }

            existing.Name = name;
            existing.Description = dto.Description?.Trim() ?? string.Empty;
            existing.IsActive = dto.IsActive;
            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.Industries.FindAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            _context.Industries.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok(new { deleted = true });
        }
    }
}
