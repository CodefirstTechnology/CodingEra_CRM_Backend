using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
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
        public async Task<IActionResult> GetAll([FromQuery] int userId, [FromQuery] bool activeOnly = false)
        {
            _ = userId;
            IQueryable<RequestType> q = _context.RequestTypes.AsNoTracking();
            if (activeOnly)
            {
                q = q.Where(r => r.IsActive);
            }

            return Ok(await q.OrderBy(r => r.Name).ToListAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, [FromQuery] int userId)
        {
            _ = userId;
            var r = await _context.RequestTypes.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            return r == null ? NotFound() : Ok(r);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromQuery] int userId, [FromBody] MasterDataUpsertDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(_context, userId);
            if (auditErr != null)
            {
                return auditErr;
            }

            AuditUserValidation.SetAuditUser(_context, userId);

            var name = (dto.Name ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name))
            {
                return BadRequest("Name is required.");
            }

            if (await _context.RequestTypes.AnyAsync(x => x.Name == name))
            {
                return Conflict("A request type with this name already exists.");
            }

            var entity = new RequestType
            {
                Id = 0,
                Name = name,
                Description = dto.Description?.Trim() ?? string.Empty,
                IsActive = dto.IsActive,
            };
            await _context.RequestTypes.AddAsync(entity);
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromQuery] int userId, [FromBody] MasterDataUpsertDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(_context, userId);
            if (auditErr != null)
            {
                return auditErr;
            }

            AuditUserValidation.SetAuditUser(_context, userId);

            if (dto.Id != 0 && dto.Id != id)
            {
                return BadRequest("Route id and body id must match when the body includes an id.");
            }

            var name = (dto.Name ?? string.Empty).Trim();
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
            existing.Description = dto.Description?.Trim() ?? string.Empty;
            existing.IsActive = dto.IsActive;
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
