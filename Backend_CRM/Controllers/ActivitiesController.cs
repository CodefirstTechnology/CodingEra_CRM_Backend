using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers
{
    [Route("api/activities")]
    [ApiController]
    public class ActivitiesController : ControllerBase
    {
        private readonly TaskDbcontext _context;

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
