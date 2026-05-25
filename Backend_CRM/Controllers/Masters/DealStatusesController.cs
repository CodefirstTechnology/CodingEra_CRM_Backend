using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers.Masters
{
    [Route("api/MasterData/deal-statuses")]
    [ApiController]
    public class DealStatusesController : ControllerBase
    {
        private readonly TaskDbcontext _context;

        public DealStatusesController(TaskDbcontext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int userId, [FromQuery] bool activeOnly = false)
        {
            _ = userId;
            IQueryable<DealStatus> q = _context.DealStatuses.AsNoTracking();
            if (activeOnly)
            {
                q = q.Where(s => s.IsActive);
            }

            return Ok(await q.OrderBy(s => s.Id).ToListAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, [FromQuery] int userId)
        {
            _ = userId;
            var s = await _context.DealStatuses.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            return s == null ? NotFound() : Ok(s);
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

            if (await _context.DealStatuses.AnyAsync(x => x.Name == name))
            {
                return Conflict("A deal status with this name already exists.");
            }

            var entity = new DealStatus
            {
                Id = 0,
                Name = name,
                Description = dto.Description?.Trim() ?? string.Empty,
                IsActive = dto.IsActive,
            };
            await _context.DealStatuses.AddAsync(entity);
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

            var existing = await _context.DealStatuses.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            if (await _context.DealStatuses.AnyAsync(x => x.Name == name && x.Id != id))
            {
                return Conflict("A deal status with this name already exists.");
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
            var entity = await _context.DealStatuses.FindAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            _context.DealStatuses.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok(new { deleted = true });
        }
    }
}
