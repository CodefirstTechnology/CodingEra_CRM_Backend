using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers
{
    [Route("api/tasks")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly TaskDbcontext _context;

        public TasksController(TaskDbcontext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int userId, [FromQuery] int? relatedLeadId = null, [FromQuery] int? relatedDealId = null)
        {
            _ = userId;
            var q = _context.Tasks.AsNoTracking();
            if (relatedLeadId.HasValue)
            {
                q = q.Where(t => t.RelatedLeadId == relatedLeadId);
            }

            if (relatedDealId.HasValue)
            {
                q = q.Where(t => t.RelatedDealId == relatedDealId);
            }

            return Ok(await q.OrderByDescending(t => t.LastModified).ToListAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, [FromQuery] int userId)
        {
            _ = userId;
            var t = await _context.Tasks.AsNoTracking().FirstOrDefaultAsync(x => x.TaskId == id);
            if (t == null)
            {
                return NotFound();
            }

            return Ok(t);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromQuery] int userId, [FromBody] TaskUpsertDto dto)
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

            await RelatedRecordOwnership.ApplyTaskAssigneeFromRelatedRecordAsync(_context, dto);

            var entity = CrmWriteMappings.ToTask(dto, 0);
            entity.TaskId = 0;
            await _context.Tasks.AddAsync(entity);
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromQuery] int userId, [FromBody] TaskUpsertDto dto)
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

            await RelatedRecordOwnership.ApplyTaskAssigneeFromRelatedRecordAsync(_context, dto);

            if (dto.TaskId != 0 && dto.TaskId != id)
            {
                return BadRequest("Route id and body taskId must match when the body includes a task id.");
            }

            var existing = await _context.Tasks.FindAsync(id);
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
            var entity = await _context.Tasks.FindAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            _context.Tasks.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok(new { deleted = true });
        }
    }
}
