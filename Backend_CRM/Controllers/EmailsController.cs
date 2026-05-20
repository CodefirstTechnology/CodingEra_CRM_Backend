using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.models;
using CRM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers
{
    [Route("api/emails")]
    [ApiController]
    public class EmailsController : ControllerBase
    {
        private readonly TaskDbcontext _context;
        private readonly IEmailService _emailService;

        public EmailsController(TaskDbcontext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
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
            if (type is not (ActivityEntityTypes.Lead or ActivityEntityTypes.Deal
                or ActivityEntityTypes.Contact or ActivityEntityTypes.Organization))
            {
                return BadRequest("entityType must be lead, deal, contact, or organization.");
            }

            var data = await _context.Emails.AsNoTracking()
                .Where(e => e.EntityType == type && e.EntityId == entityId)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => ToResponse(e))
                .ToListAsync();

            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> Send([FromQuery] int userId, [FromBody] SendEmailDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            if (string.IsNullOrWhiteSpace(dto.ToEmail)
                || string.IsNullOrWhiteSpace(dto.Subject)
                || string.IsNullOrWhiteSpace(dto.Body))
            {
                return BadRequest("toEmail, subject, and body are required.");
            }

            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(_context, userId);
            if (auditErr != null)
            {
                return auditErr;
            }

            var entityType = (dto.EntityType ?? ActivityEntityTypes.Lead).Trim().ToLowerInvariant();
            if (entityType is not (ActivityEntityTypes.Lead or ActivityEntityTypes.Deal
                or ActivityEntityTypes.Contact))
            {
                return BadRequest("entityType must be lead, deal, or contact.");
            }

            if (dto.EntityId <= 0)
            {
                return BadRequest("entityId must be a positive integer.");
            }

            if (!await EntityExistsAsync(entityType, dto.EntityId))
            {
                return NotFound();
            }

            AuditUserValidation.SetAuditUser(_context, userId);

            var sendResult = await _emailService.SendAsync(
                dto.ToEmail.Trim(),
                dto.Subject.Trim(),
                dto.Body.Trim(),
                dto.IsHtml);

            var email = new Email
            {
                EntityType = entityType,
                EntityId = dto.EntityId,
                ToEmail = dto.ToEmail.Trim(),
                Subject = dto.Subject.Trim(),
                Body = dto.Body.Trim(),
                Status = sendResult.Success ? Email.StatusSent : Email.StatusFailed,
                FailureMessage = sendResult.Success ? null : sendResult.ErrorMessage,
                SentBy = dto.SentBy is > 0 ? dto.SentBy : userId,
            };

            await _context.Emails.AddAsync(email);
            await _context.SaveChangesAsync();

            if (sendResult.Success)
            {
                await LogEmailSentActivityAsync(entityType, dto.EntityId, userId, email);
            }

            return Ok(ToResponse(email));
        }

        private static EmailResponseDto ToResponse(Email e) => new()
        {
            Id = e.Id,
            EntityType = e.EntityType,
            EntityId = e.EntityId,
            ToEmail = e.ToEmail,
            Subject = e.Subject,
            Body = e.Body,
            Status = e.Status,
            FailureMessage = e.FailureMessage,
            SentBy = e.SentBy,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt,
        };

        private async Task<bool> EntityExistsAsync(string entityType, int entityId) =>
            entityType switch
            {
                ActivityEntityTypes.Lead => await _context.Leads.AsNoTracking().AnyAsync(l => l.Id == entityId),
                ActivityEntityTypes.Deal => await _context.Deals.AsNoTracking().AnyAsync(d => d.Id == entityId),
                ActivityEntityTypes.Contact => await _context.Contacts.AsNoTracking().AnyAsync(c => c.Id == entityId),
                _ => false,
            };

        private async Task LogEmailSentActivityAsync(
            string entityType,
            int entityId,
            int userId,
            Email email)
        {
            var actorName = await _context.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync();

            actorName = string.IsNullOrWhiteSpace(actorName) ? "User" : actorName;
            var preview = Truncate(email.Subject, 80);

            _context.ActivityLogs.Add(new ActivityLog
            {
                EntityType = entityType,
                EntityId = entityId,
                ActionType = ActivityActionTypes.EmailSent,
                ActorUserId = userId,
                ActorName = actorName,
                RelatedRecordType = "email",
                RelatedRecordId = email.Id,
                Message = $"{actorName} sent email: {preview}",
                CreatedAt = DateTime.UtcNow,
            });

            await _context.SaveChangesAsync();
        }

        private static string Truncate(string? text, int max)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "(no subject)";
            }

            var t = text.Trim();
            return t.Length <= max ? t : t[..max] + "…";
        }
    }
}
