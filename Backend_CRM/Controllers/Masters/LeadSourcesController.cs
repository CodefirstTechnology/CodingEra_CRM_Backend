using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers.Masters
{
    [Route("api/MasterData/sources")]
    [Route("api/MasterData/lead-sources")]
    [ApiController]
    public class LeadSourcesController : ControllerBase
    {
        private readonly TaskDbcontext _context;

        public LeadSourcesController(TaskDbcontext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int userId, [FromQuery] bool activeOnly = false)
        {
            _ = userId;
            IQueryable<LeadSource> q = _context.LeadSources.AsNoTracking();
            if (activeOnly)
            {
                q = q.Where(s => s.IsActive);
            }

            return Ok(await q.OrderBy(s => s.SortOrder).ThenBy(s => s.Name).ToListAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, [FromQuery] int userId)
        {
            _ = userId;
            var s = await _context.LeadSources.AsNoTracking()
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

            if (await _context.LeadSources.AnyAsync(x => x.Name == name))
            {
                return Conflict("A source with this name already exists.");
            }

            var maxSort = await _context.LeadSources.MaxAsync(x => (int?)x.SortOrder) ?? 0;
            var entity = new LeadSource
            {
                Id = 0,
                Name = name,
                Description = dto.Description?.Trim() ?? string.Empty,
                SortOrder = dto.SortOrder.HasValue && dto.SortOrder > 0 ? dto.SortOrder.Value : maxSort + 1,
                IsActive = dto.IsActive,
            };
            await _context.LeadSources.AddAsync(entity);
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

            var existing = await _context.LeadSources.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            if (await _context.LeadSources.AnyAsync(x => x.Name == name && x.Id != id))
            {
                return Conflict("A source with this name already exists.");
            }

            existing.Name = name;
            existing.Description = dto.Description?.Trim() ?? string.Empty;
            existing.IsActive = dto.IsActive;
            if (dto.SortOrder.HasValue && dto.SortOrder > 0)
            {
                existing.SortOrder = dto.SortOrder.Value;
            }
            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.LeadSources.FindAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            var name = entity.Name.ToLower();
            var inLeads = await _context.Leads.AnyAsync(l => l.LeadSource.ToLower() == name);
            if (inLeads)
            {
                return Conflict(new { message = "Cannot delete: This source is assigned to existing leads. Please disable it instead or reassign records first." });
            }

            _context.LeadSources.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok(new { deleted = true });
        }
    }
}
