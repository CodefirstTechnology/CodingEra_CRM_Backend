using CRM.DATA;
using CRM.DTO;
using CRM.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers
{
    [Route("api/contacts")]
    [ApiController]
    public class ContactsController : ControllerBase
    {
        private readonly TaskDbcontext _context;

        public ContactsController(TaskDbcontext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? organizationId = null)
        {
            var q = _context.Contacts.AsNoTracking();
            if (organizationId.HasValue)
            {
                q = q.Where(c => c.OrganizationId == organizationId);
            }

            return Ok(await q.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToListAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var c = await _context.Contacts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (c == null)
            {
                return NotFound();
            }

            return Ok(c);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ContactUpsertDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            var entity = CrmWriteMappings.ToContact(dto, 0);
            entity.Id = 0;
            await _context.Contacts.AddAsync(entity);
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ContactUpsertDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            if (dto.Id != 0 && dto.Id != id)
            {
                return BadRequest("Route id and body id must match when the body includes an id.");
            }

            var existing = await _context.Contacts.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            CrmWriteMappings.Apply(existing, dto);
            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.Contacts.FindAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            _context.Contacts.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok(new { deleted = true });
        }
    }
}
