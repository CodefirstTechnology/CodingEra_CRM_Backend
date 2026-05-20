using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers
{
    [Route("api/comments")]
    [ApiController]
    public class CommentsController : ControllerBase
    {
        private readonly TaskDbcontext _context;

        public CommentsController(TaskDbcontext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int userId,
            [FromQuery] string entityType,
            [FromQuery] int entityId)
        {
            _ = userId;
            if (string.IsNullOrWhiteSpace(entityType) || entityId <= 0)
            {
                return BadRequest("entityType and entityId are required.");
            }

            var type = entityType.Trim().ToLowerInvariant();
            var data = await _context.Comments.AsNoTracking()
                .Where(c => c.EntityType == type && c.EntityId == entityId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromQuery] int userId, [FromBody] CommentUpsertDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Body))
            {
                return BadRequest("Comment body is required.");
            }

            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(_context, userId);
            if (auditErr != null)
            {
                return auditErr;
            }

            AuditUserValidation.SetAuditUser(_context, userId);

            var entityType = (dto.EntityType ?? ActivityEntityTypes.Lead).Trim().ToLowerInvariant();
            if (entityType is not (ActivityEntityTypes.Lead or ActivityEntityTypes.Deal
                or ActivityEntityTypes.Contact or ActivityEntityTypes.Organization))
            {
                return BadRequest("entityType must be lead, deal, contact, or organization.");
            }

            if (dto.EntityId <= 0)
            {
                return BadRequest("entityId must be a positive integer.");
            }

            var comment = new Comment
            {
                EntityType = entityType,
                EntityId = dto.EntityId,
                AuthorId = dto.AuthorId is > 0 ? dto.AuthorId : userId,
                Body = dto.Body.Trim(),
            };

            await _context.Comments.AddAsync(comment);
            await _context.SaveChangesAsync();
            return Ok(comment);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromQuery] int userId, [FromBody] CommentUpsertDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Body))
            {
                return BadRequest("Comment body is required.");
            }

            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(_context, userId);
            if (auditErr != null)
            {
                return auditErr;
            }

            AuditUserValidation.SetAuditUser(_context, userId);

            var existing = await _context.Comments.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.Body = dto.Body.Trim();
            if (dto.AuthorId is > 0)
            {
                existing.AuthorId = dto.AuthorId;
            }

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, [FromQuery] int userId)
        {
            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(_context, userId);
            if (auditErr != null)
            {
                return auditErr;
            }

            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
            {
                return NotFound();
            }

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            return Ok(new { deleted = true });
        }
    }
}
