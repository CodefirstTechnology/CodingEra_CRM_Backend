using CRM.DATA;
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
        public async Task<IActionResult> GetAll([FromQuery] int? relatedLeadId = null, [FromQuery] int? relatedDealId = null)
        {
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
        public async Task<IActionResult> GetById(int id)
        {
            var t = await _context.Tasks.AsNoTracking().FirstOrDefaultAsync(x => x.TaskId == id);
            if (t == null)
            {
                return NotFound();
            }

            return Ok(t);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TaskTable entity)
        {
            if (entity == null)
            {
                return BadRequest();
            }

            entity.TaskId = 0;
            var now = DateTime.UtcNow;
            entity.LastModified = now;
            await _context.Tasks.AddAsync(entity);
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] TaskTable updated)
        {
            if (updated == null || id != updated.TaskId)
            {
                return BadRequest();
            }

            var existing = await _context.Tasks.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.TaskTitle = updated.TaskTitle;
            existing.TaskDescription = updated.TaskDescription;
            existing.TaskStatus = updated.TaskStatus;
            existing.TaskAssignee = updated.TaskAssignee;
            existing.TaskDueDate = updated.TaskDueDate;
            existing.TaskPriority = updated.TaskPriority;
            existing.AssigneeUserId = updated.AssigneeUserId;
            existing.RelatedLeadId = updated.RelatedLeadId;
            existing.RelatedDealId = updated.RelatedDealId;
            existing.LastModified = DateTime.UtcNow;
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
