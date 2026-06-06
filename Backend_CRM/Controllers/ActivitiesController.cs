using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers
{
    [Route("api/activities")]
    [ApiController]
    public class ActivitiesController : ControllerBase
    {
        private readonly TaskDbcontext _context;

        private static readonly HashSet<string> AllowedActionTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            ActivityActionTypes.Created,
            ActivityActionTypes.Updated,
            ActivityActionTypes.StatusChanged,
            ActivityActionTypes.FieldUpdated,
            ActivityActionTypes.NoteAdded,
            ActivityActionTypes.CommentAdded,
            ActivityActionTypes.TaskAdded,
            ActivityActionTypes.CallLogged,
            ActivityActionTypes.EmailSent,
            ActivityActionTypes.AttachmentAdded,
            ActivityActionTypes.Deleted,
        };

        public ActivitiesController(TaskDbcontext context)
        {
            _context = context;
        }

        /// <summary>Activity feed for a lead detail page.</summary>
        [HttpGet("leads/{leadId:int}")]
        public async Task<IActionResult> GetForLead(int leadId, [FromQuery] int userId)
        {
            _ = userId;
            if (!await _context.Leads.AsNoTracking().AnyAsync(l => l.Id == leadId))
            {
                return NotFound();
            }

            return Ok(await QueryActivitiesAsync(ActivityEntityTypes.Lead, leadId));
        }

        /// <summary>Manually log an activity on a lead timeline.</summary>
        [HttpPost("leads/{leadId:int}")]
        public async Task<IActionResult> CreateForLead(
            int leadId,
            [FromQuery] int userId,
            [FromBody] CreateActivityDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            if (!await _context.Leads.AsNoTracking().AnyAsync(l => l.Id == leadId))
            {
                return NotFound();
            }

            return await CreateActivityAsync(ActivityEntityTypes.Lead, leadId, userId, dto);
        }

        [HttpGet("deals/{dealId:int}")]
        public async Task<IActionResult> GetForDeal(int dealId, [FromQuery] int userId)
        {
            _ = userId;
            if (!await _context.Deals.AsNoTracking().AnyAsync(d => d.Id == dealId))
            {
                return NotFound();
            }

            return Ok(await QueryActivitiesAsync(ActivityEntityTypes.Deal, dealId));
        }

        [HttpGet("contacts/{contactId:int}")]
        public async Task<IActionResult> GetForContact(int contactId, [FromQuery] int userId)
        {
            _ = userId;
            if (!await _context.Contacts.AsNoTracking().AnyAsync(c => c.Id == contactId))
            {
                return NotFound();
            }

            return Ok(await QueryActivitiesAsync(ActivityEntityTypes.Contact, contactId));
        }

        [HttpGet("organizations/{organizationId:int}")]
        public async Task<IActionResult> GetForOrganization(int organizationId, [FromQuery] int userId)
        {
            _ = userId;
            if (!await _context.Organizations.AsNoTracking().AnyAsync(o => o.Id == organizationId))
            {
                return NotFound();
            }

            return Ok(await QueryActivitiesAsync(ActivityEntityTypes.Organization, organizationId));
        }

        /// <summary>Recent lead/deal activities for admin and user dashboards.</summary>
        [HttpGet("recent")]
        public async Task<IActionResult> GetRecent([FromQuery] int userId, [FromQuery] int limit = 50)
        {
            _ = userId;
            var take = Math.Clamp(limit, 1, 100);

            var items = await _context.ActivityLogs.AsNoTracking()
                .Where(a =>
                    a.EntityType == ActivityEntityTypes.Lead ||
                    a.EntityType == ActivityEntityTypes.Deal)
                .OrderByDescending(a => a.CreatedAt)
                .ThenByDescending(a => a.Id)
                .Take(take)
                .Select(a => new ActivityLogDto
                {
                    Id = a.Id,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    ActionType = a.ActionType,
                    ActorUserId = a.ActorUserId,
                    ActorName = a.ActorName,
                    Message = a.Message,
                    FieldName = a.FieldName,
                    OldValue = a.OldValue,
                    NewValue = a.NewValue,
                    RelatedRecordType = a.RelatedRecordType,
                    RelatedRecordId = a.RelatedRecordId,
                    CreatedAt = a.CreatedAt,
                })
                .ToListAsync();

            return Ok(items);
        }

        /// <summary>Generic feed: <c>?entityType=lead&amp;entityId=1</c></summary>
        [HttpGet]
        public async Task<IActionResult> Get(
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
            if (type is not (ActivityEntityTypes.Lead or ActivityEntityTypes.Deal
                or ActivityEntityTypes.Contact or ActivityEntityTypes.Organization))
            {
                return BadRequest("entityType must be lead, deal, contact, or organization.");
            }

            return Ok(await QueryActivitiesAsync(type, entityId));
        }

        /// <summary>Manually log an activity on a deal timeline.</summary>
        [HttpPost("deals/{dealId:int}")]
        public async Task<IActionResult> CreateForDeal(
            int dealId,
            [FromQuery] int userId,
            [FromBody] CreateActivityDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            if (!await _context.Deals.AsNoTracking().AnyAsync(d => d.Id == dealId))
            {
                return NotFound();
            }

            return await CreateActivityAsync(ActivityEntityTypes.Deal, dealId, userId, dto);
        }

        /// <summary>Manually log an activity: <c>?entityType=deal&amp;entityId=1</c></summary>
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromQuery] int userId,
            [FromQuery] string entityType,
            [FromQuery] int entityId,
            [FromBody] CreateActivityDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            if (string.IsNullOrWhiteSpace(entityType) || entityId <= 0)
            {
                return BadRequest("entityType and entityId are required.");
            }

            var type = entityType.Trim().ToLowerInvariant();
            if (type is not (ActivityEntityTypes.Lead or ActivityEntityTypes.Deal
                or ActivityEntityTypes.Contact or ActivityEntityTypes.Organization))
            {
                return BadRequest("entityType must be lead, deal, contact, or organization.");
            }

            if (!await EntityExistsAsync(type, entityId))
            {
                return NotFound();
            }

            return await CreateActivityAsync(type, entityId, userId, dto);
        }

        private async Task<IActionResult> CreateActivityAsync(
            string entityType,
            int entityId,
            int userId,
            CreateActivityDto dto)
        {
            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(_context, userId);
            if (auditErr != null)
            {
                return auditErr;
            }

            var message = (dto.Message ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(message))
            {
                return BadRequest("message is required.");
            }

            var actionType = (dto.ActionType ?? ActivityActionTypes.Updated).Trim().ToLowerInvariant();
            if (!AllowedActionTypes.Contains(actionType))
            {
                return BadRequest("Invalid actionType.");
            }

            var actor = await _context.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync();
            actor = string.IsNullOrWhiteSpace(actor) ? "User" : actor;

            var activity = new ActivityLog
            {
                EntityType = entityType,
                EntityId = entityId,
                ActionType = actionType,
                ActorUserId = userId,
                ActorName = actor,
                Message = message,
                FieldName = string.IsNullOrWhiteSpace(dto.FieldName) ? null : dto.FieldName.Trim(),
                OldValue = dto.OldValue,
                NewValue = dto.NewValue,
                RelatedRecordType = string.IsNullOrWhiteSpace(dto.RelatedRecordType)
                    ? null
                    : dto.RelatedRecordType.Trim().ToLowerInvariant(),
                RelatedRecordId = dto.RelatedRecordId is > 0 ? dto.RelatedRecordId : null,
                CreatedAt = DateTime.UtcNow,
            };

            await _context.ActivityLogs.AddAsync(activity);
            await _context.SaveChangesAsync();

            return Ok(new ActivityLogDto
            {
                Id = activity.Id,
                EntityType = activity.EntityType,
                EntityId = activity.EntityId,
                ActionType = activity.ActionType,
                ActorUserId = activity.ActorUserId,
                ActorName = activity.ActorName,
                Message = activity.Message,
                FieldName = activity.FieldName,
                OldValue = activity.OldValue,
                NewValue = activity.NewValue,
                RelatedRecordType = activity.RelatedRecordType,
                RelatedRecordId = activity.RelatedRecordId,
                CreatedAt = activity.CreatedAt,
            });
        }

        private async Task<bool> EntityExistsAsync(string entityType, int entityId) => entityType switch
        {
            ActivityEntityTypes.Lead => await _context.Leads.AsNoTracking().AnyAsync(e => e.Id == entityId),
            ActivityEntityTypes.Deal => await _context.Deals.AsNoTracking().AnyAsync(e => e.Id == entityId),
            ActivityEntityTypes.Contact => await _context.Contacts.AsNoTracking().AnyAsync(e => e.Id == entityId),
            ActivityEntityTypes.Organization => await _context.Organizations.AsNoTracking().AnyAsync(e => e.Id == entityId),
            _ => false,
        };

        private async Task<List<ActivityLogDto>> QueryActivitiesAsync(string entityType, int entityId) =>
            await _context.ActivityLogs.AsNoTracking()
                .Where(a => a.EntityType == entityType && a.EntityId == entityId)
                .OrderByDescending(a => a.CreatedAt)
                .ThenByDescending(a => a.Id)
                .Select(a => new ActivityLogDto
                {
                    Id = a.Id,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    ActionType = a.ActionType,
                    ActorUserId = a.ActorUserId,
                    ActorName = a.ActorName,
                    Message = a.Message,
                    FieldName = a.FieldName,
                    OldValue = a.OldValue,
                    NewValue = a.NewValue,
                    RelatedRecordType = a.RelatedRecordType,
                    RelatedRecordId = a.RelatedRecordId,
                    CreatedAt = a.CreatedAt,
                })
                .ToListAsync();
    }
}
