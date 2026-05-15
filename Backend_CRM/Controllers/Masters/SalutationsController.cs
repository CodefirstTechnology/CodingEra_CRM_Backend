using CRM.DATA;
using CRM.DTO;
using CRM.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers.Masters
{
    /// <summary>CRUD for salutation master data (name + description).</summary>
    [Route("api/MasterData/salutations")]
    [ApiController]
    public class SalutationsController : ControllerBase
    {
        private readonly TaskDbcontext _context;

        public SalutationsController(TaskDbcontext context)
        {
            _context = context;
        }

        /// <summary>List salutations; use <paramref name="activeOnly"/> <c>true</c> for dropdowns.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false)
        {
            var q = _context.Salutations.AsNoTracking();
            if (activeOnly)
            {
                q = q.Where(s => s.IsActive);
            }

            return Ok(await q.OrderBy(s => s.Name).ToListAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var s = await _context.Salutations.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (s == null)
            {
                return NotFound();
            }

            return Ok(s);
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

            if (await _context.Salutations.AnyAsync(s => s.Name == name))
            {
                return Conflict("A salutation with this name already exists.");
            }

            var entity = new Salutation
            {
                Id = 0,
                Name = name,
                Description = dto.Description?.Trim() ?? string.Empty,
                IsActive = dto.IsActive,
            };
            await _context.Salutations.AddAsync(entity);
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

            var existing = await _context.Salutations.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            if (await _context.Salutations.AnyAsync(s => s.Name == name && s.Id != id))
            {
                return Conflict("A salutation with this name already exists.");
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
            var entity = await _context.Salutations.FindAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            _context.Salutations.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok(new { deleted = true });
        }
    }
}
