using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.DATA;
using CRM.DTO;
using CRM.Hubs;
using CRM.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FollowUpController : ControllerBase
    {
        private readonly TaskDbcontext _context;
        private readonly IHubContext<UserStatusHub> _hubContext;

        public FollowUpController(TaskDbcontext context, IHubContext<UserStatusHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Marks a follow-up task as completed, optionally logs a CallLog entry,
        /// and immediately broadcasts real-time dismissal events to Executive and Admins.
        /// </summary>
        [HttpPost("tasks/{id}/complete-followup")]
        public async Task<IActionResult> CompleteFollowUp(int id, [FromBody] CompleteFollowUpRequestDto req)
        {
            if (req == null)
            {
                return BadRequest("Invalid request payload.");
            }

            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.TaskId == id && t.IsActive);
            if (task == null)
            {
                return NotFound($"Task #{id} was not found.");
            }

            var nowUtc = DateTime.UtcNow;

            // 1. Log call to CallLogs table if requested
            int? createdCallId = null;
            if (req.IsCallLogged && task.RelatedLeadId.HasValue)
            {
                var durSec = Math.Max(10, req.CallDurationSeconds);
                var callLog = new CallLog
                {
                    RelatedLeadId = task.RelatedLeadId.Value,
                    RelatedTaskId = task.TaskId,
                    Direction = "Outbound",
                    DurationSeconds = durSec,
                    DurationMinutes = Math.Max(1, durSec / 60),
                    Outcome = "Completed",
                    CallSummary = string.IsNullOrWhiteSpace(req.Notes) ? "Follow-up call completed" : req.Notes.Trim(),
                    CallStarted = nowUtc.AddSeconds(-durSec),
                    CreatedAt = nowUtc,
                    UpdatedAt = nowUtc,
                    CreatedBy = task.AssigneeUserId,
                    UpdatedBy = task.AssigneeUserId,
                    IsActive = true
                };

                await _context.CallLogs.AddAsync(callLog);
                await _context.SaveChangesAsync();
                createdCallId = callLog.CallId;
            }

            // 2. Mark task complete
            task.TaskStatus = "Completed";
            task.UpdatedAt = nowUtc;
            task.LastModified = nowUtc;
            if (createdCallId.HasValue)
            {
                task.CallLogId = createdCallId.Value;
            }

            // 3. Log activity log
            if (task.RelatedLeadId.HasValue)
            {
                var activity = new ActivityLog
                {
                    EntityType = "lead",
                    EntityId = task.RelatedLeadId.Value,
                    ActorUserId = task.AssigneeUserId,
                    ActionType = "call_logged",
                    Message = $"Follow-up task '{task.TaskTitle}' marked as Completed. {(string.IsNullOrWhiteSpace(req.Notes) ? "" : "Notes: " + req.Notes)}",
                    CreatedAt = nowUtc
                };
                await _context.ActivityLogs.AddAsync(activity);
            }

            await _context.SaveChangesAsync();

            // 4. Real-time dismissal broadcasts via SignalR
            var dismissPopup = new DismissFollowUpPopupDto
            {
                TaskId = task.TaskId,
                LeadId = task.RelatedLeadId ?? 0,
                Reason = "COMPLETED"
            };

            if (task.AssigneeUserId.HasValue)
            {
                await _hubContext.Clients.Group($"User_{task.AssigneeUserId.Value}")
                    .SendAsync("DismissFollowUpPopup", dismissPopup);
            }

            if (task.IsEscalatedToAdmin)
            {
                var dismissEscalation = new DismissAdminEscalationDto
                {
                    TaskId = task.TaskId,
                    LeadId = task.RelatedLeadId ?? 0,
                    ResolutionNotes = $"Completed by Executive #{task.AssigneeUserId}"
                };

                await _hubContext.Clients.Group(UserStatusHub.AdminsGroupName)
                    .SendAsync("DismissAdminEscalation", dismissEscalation);
            }

            return Ok(new { success = true, taskId = task.TaskId, status = "Completed" });
        }

        /// <summary>
        /// Reschedules a follow-up task with a mandatory reason, resets SLA alerts,
        /// and dismisses active persistent popups and admin escalations.
        /// </summary>
        [HttpPost("tasks/{id}/reschedule-followup")]
        public async Task<IActionResult> RescheduleFollowUp(int id, [FromBody] RescheduleFollowUpRequestDto req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.RescheduleReason))
            {
                return BadRequest("A valid reschedule reason is mandatory.");
            }

            if (req.NewDueDate <= DateTime.UtcNow)
            {
                return BadRequest("New due date must be set in the future.");
            }

            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.TaskId == id && t.IsActive);
            if (task == null)
            {
                return NotFound($"Task #{id} was not found.");
            }

            var nowUtc = DateTime.UtcNow;
            var oldDueDate = task.TaskDueDate;

            task.TaskDueDate = DateTime.SpecifyKind(req.NewDueDate, DateTimeKind.Utc);
            task.TaskStatus = "Pending";
            task.UpdatedAt = nowUtc;
            task.LastModified = nowUtc;

            // Reset SLA tracking flags
            task.FirstDueAlertSentAt = null;
            task.LastRepetitionAlertSentAt = null;
            bool wasEscalated = task.IsEscalatedToAdmin;
            task.IsEscalatedToAdmin = false;
            task.EscalatedToAdminAt = null;

            // Log activity log
            if (task.RelatedLeadId.HasValue)
            {
                var activity = new ActivityLog
                {
                    EntityType = "lead",
                    EntityId = task.RelatedLeadId.Value,
                    ActorUserId = task.AssigneeUserId,
                    ActionType = "task_updated",
                    Message = $"Follow-up task '{task.TaskTitle}' rescheduled from {oldDueDate:dd-MMM HH:mm} to {task.TaskDueDate:dd-MMM HH:mm}. Reason: {req.RescheduleReason.Trim()}",
                    CreatedAt = nowUtc
                };
                await _context.ActivityLogs.AddAsync(activity);
            }

            await _context.SaveChangesAsync();

            // Real-time dismissal broadcasts
            var dismissPopup = new DismissFollowUpPopupDto
            {
                TaskId = task.TaskId,
                LeadId = task.RelatedLeadId ?? 0,
                Reason = "RESCHEDULED"
            };

            if (task.AssigneeUserId.HasValue)
            {
                await _hubContext.Clients.Group($"User_{task.AssigneeUserId.Value}")
                    .SendAsync("DismissFollowUpPopup", dismissPopup);
            }

            if (wasEscalated)
            {
                var dismissEscalation = new DismissAdminEscalationDto
                {
                    TaskId = task.TaskId,
                    LeadId = task.RelatedLeadId ?? 0,
                    ResolutionNotes = $"Rescheduled to {task.TaskDueDate:yyyy-MM-dd HH:mm}. Reason: {req.RescheduleReason}"
                };

                await _hubContext.Clients.Group(UserStatusHub.AdminsGroupName)
                    .SendAsync("DismissAdminEscalation", dismissEscalation);
            }

            return Ok(new { success = true, taskId = task.TaskId, newDueDate = task.TaskDueDate });
        }

        /// <summary>
        /// Returns all currently active overdue follow-ups across ALL users for initial Admin dashboard hydration.
        /// </summary>
        [HttpGet("admin/escalations")]
        public async Task<IActionResult> GetActiveAdminEscalations()
        {
            var nowUtc = DateTime.UtcNow;

            var overdueTasks = await _context.Tasks.AsNoTracking()
                .Where(t => t.IsActive &&
                            t.RelatedLeadId.HasValue &&
                            t.TaskStatus != "Completed" &&
                            t.TaskStatus != "Done" &&
                            t.TaskStatus != "Canceled" &&
                            t.TaskDueDate <= nowUtc)
                .ToListAsync();

            if (overdueTasks.Count == 0)
            {
                return Ok(new List<FollowUpAdminEscalationDto>());
            }

            var leadIds = overdueTasks.Select(t => t.RelatedLeadId!.Value).Distinct().ToList();
            var userIds = overdueTasks.Where(t => t.AssigneeUserId.HasValue)
                .Select(t => t.AssigneeUserId!.Value).Distinct().ToList();

            var leads = await _context.Leads.AsNoTracking()
                .Include(l => l.Organization)
                .Where(l => leadIds.Contains(l.Id))
                .ToDictionaryAsync(l => l.Id, l => new
                {
                    l.Id,
                    CustomerName = l.Organization != null && !string.IsNullOrWhiteSpace(l.Organization.Name)
                        ? l.Organization.Name
                        : (l.FirstName + " " + l.LastName).Trim()
                });

            var users = await _context.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.Email);

            var list = overdueTasks.Select(t =>
            {
                var overdueMins = (int)Math.Max(0, Math.Floor((nowUtc - t.TaskDueDate).TotalMinutes));
                leads.TryGetValue(t.RelatedLeadId!.Value, out var lead);
                string execName = t.AssigneeUserId.HasValue && users.TryGetValue(t.AssigneeUserId.Value, out var uName)
                    ? uName
                    : "Sales Representative";

                return new FollowUpAdminEscalationDto
                {
                    TaskId = t.TaskId,
                    LeadId = t.RelatedLeadId.Value,
                    LeadFormattedId = $"L{t.RelatedLeadId.Value:D4}",
                    CustomerName = lead?.CustomerName ?? "Customer",
                    SalesExecutiveName = execName,
                    SalesExecutiveUserId = t.AssigneeUserId ?? 0,
                    TaskTitle = t.TaskTitle,
                    DueDate = t.TaskDueDate,
                    MinutesOverdue = overdueMins,
                    Severity = overdueMins >= 60 ? "CRITICAL" : "OVERDUE",
                    EscalatedAt = t.EscalatedToAdminAt ?? nowUtc
                };
            })
            .OrderByDescending(e => e.MinutesOverdue)
            .ToList();

            return Ok(list);
        }

        /// <summary>
        /// Returns active overdue follow-ups assigned specifically to the requested user for User dashboard hydration.
        /// </summary>
        [HttpGet("user/{userId}/overdue")]
        public async Task<IActionResult> GetUserOverdueFollowUps(int userId)
        {
            var nowUtc = DateTime.UtcNow;

            var userOverdueTasks = await _context.Tasks.AsNoTracking()
                .Where(t => t.IsActive &&
                            t.RelatedLeadId.HasValue &&
                            t.AssigneeUserId == userId &&
                            t.TaskStatus != "Completed" &&
                            t.TaskStatus != "Done" &&
                            t.TaskStatus != "Canceled" &&
                            t.TaskDueDate <= nowUtc)
                .ToListAsync();

            if (userOverdueTasks.Count == 0)
            {
                return Ok(new List<FollowUpPopupEventDto>());
            }

            var leadIds = userOverdueTasks.Select(t => t.RelatedLeadId!.Value).Distinct().ToList();

            var leads = await _context.Leads.AsNoTracking()
                .Include(l => l.Organization)
                .Where(l => leadIds.Contains(l.Id))
                .ToDictionaryAsync(l => l.Id, l => new
                {
                    l.Id,
                    CustomerName = l.Organization != null && !string.IsNullOrWhiteSpace(l.Organization.Name)
                        ? l.Organization.Name
                        : (l.FirstName + " " + l.LastName).Trim(),
                    ContactPerson = (l.FirstName + " " + l.LastName).Trim(),
                    Mobile = l.Mobile ?? string.Empty
                });

            var list = userOverdueTasks.Select(t =>
            {
                var overdueMins = (int)Math.Max(0, Math.Floor((nowUtc - t.TaskDueDate).TotalMinutes));
                leads.TryGetValue(t.RelatedLeadId!.Value, out var lead);

                return new FollowUpPopupEventDto
                {
                    TaskId = t.TaskId,
                    LeadId = t.RelatedLeadId.Value,
                    LeadFormattedId = $"L{t.RelatedLeadId.Value:D4}",
                    CustomerName = lead?.CustomerName ?? "Customer",
                    ContactPerson = lead?.ContactPerson ?? "Contact",
                    Mobile = lead?.Mobile ?? string.Empty,
                    TaskTitle = t.TaskTitle,
                    DueDate = t.TaskDueDate,
                    MinutesOverdue = overdueMins,
                    IsInitialDue = t.FirstDueAlertSentAt == null
                };
            })
            .OrderByDescending(e => e.MinutesOverdue)
            .ToList();

            return Ok(list);
        }
    }
}
