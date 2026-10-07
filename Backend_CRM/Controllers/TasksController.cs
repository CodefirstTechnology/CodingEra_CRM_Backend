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

            var entity = CrmWriteMappings.ToTask(dto, 0);
            entity.TaskId = 0;
            await _context.Tasks.AddAsync(entity);

            if (dto.RelatedLeadId.HasValue && dto.RelatedLeadId.Value > 0)
            {
                var leadId = dto.RelatedLeadId.Value;
                var relatedLead = await _context.Leads.FindAsync(leadId);
                if (relatedLead != null)
                {
                    var followUpStatus = await _context.LeadStatuses
                        .AsNoTracking()
                        .Where(s => s.IsActive && (s.Name.ToLower() == "follow-up" || s.Name.ToLower() == "follow up"))
                        .FirstOrDefaultAsync();

                    if (followUpStatus != null)
                    {
                        relatedLead.LeadStatusId = followUpStatus.Id;
                        relatedLead.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

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

        [HttpPost("{id:int}/complete-followup")]
        [HttpPost("complete-followup")]
        public async Task<IActionResult> CompleteFollowUp(int id, [FromQuery] int userId, [FromBody] CompleteFollowUpDto dto)
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

            var taskId = id > 0 ? id : dto.TaskId;
            var task = await _context.Tasks.FindAsync(taskId);
            if (task == null)
            {
                return NotFound("Task not found.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Mark task as Completed / Done and save discussion notes
                task.TaskStatus = "Done";
                task.LastModified = DateTime.UtcNow;
                task.UpdatedAt = DateTime.UtcNow;
                task.UpdatedBy = userId;

                var notes = dto.DiscussionNotes?.Trim() ?? string.Empty;
                if (!string.IsNullOrEmpty(notes))
                {
                    task.TaskDescription = string.IsNullOrWhiteSpace(task.TaskDescription)
                        ? $"Outcome / Notes: {notes}"
                        : $"{task.TaskDescription}\n\nOutcome / Notes: {notes}";
                }

                int? callLogId = null;

                // 2. If LogAsCallDone == true: Insert into CallLogs and link task <-> call log
                if (dto.LogAsCallDone)
                {
                    var outcome = string.IsNullOrWhiteSpace(dto.CallOutcome) ? "Connected" : dto.CallOutcome;
                    var callLog = new CallLog
                    {
                        Direction = "Outbound",
                        PhoneNumber = "",
                        ContactName = "",
                        ContactCompany = "",
                        CallStarted = DateTime.UtcNow,
                        DurationMinutes = (dto.DurationSeconds ?? 60) / 60,
                        DurationSeconds = dto.DurationSeconds ?? 60,
                        Outcome = outcome,
                        CallSummary = notes,
                        RelatedLeadId = task.RelatedLeadId,
                        RelatedDealId = task.RelatedDealId,
                        RelatedTaskId = task.TaskId,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        CreatedBy = userId,
                        UpdatedBy = userId,
                        LastModified = DateTime.UtcNow
                    };

                    if (task.RelatedLeadId.HasValue && task.RelatedLeadId.Value > 0)
                    {
                        var lead = await _context.Leads
                            .Include(l => l.Organization)
                            .AsNoTracking()
                            .FirstOrDefaultAsync(l => l.Id == task.RelatedLeadId.Value);

                        if (lead != null)
                        {
                            callLog.ContactName = $"{lead.FirstName} {lead.LastName}".Trim();
                            callLog.PhoneNumber = lead.Mobile ?? "";
                            callLog.ContactCompany = lead.Organization?.Name ?? "";
                        }
                    }

                    await _context.CallLogs.AddAsync(callLog);
                    await _context.SaveChangesAsync();

                    callLogId = callLog.CallId;
                    task.CallLogId = callLogId;
                }

                // 3. Insert ActivityLogs entry indicating follow-up completed via call
                if (task.RelatedLeadId.HasValue && task.RelatedLeadId.Value > 0)
                {
                    var outcome = string.IsNullOrWhiteSpace(dto.CallOutcome) ? "Connected" : dto.CallOutcome;
                    var notesSuffix = string.IsNullOrEmpty(notes) ? "" : $": {notes}";
                    var actor = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                    var actorName = actor != null ? actor.FullName : "System";

                    var activity = new ActivityLog
                    {
                        EntityType = "lead",
                        EntityId = task.RelatedLeadId.Value,
                        ActionType = ActivityActionTypes.CallLogged,
                        ActorUserId = userId,
                        ActorName = actorName,
                        Message = $"Follow-up Completed via Call (Outcome: {outcome}){notesSuffix}",
                        RelatedRecordType = "call",
                        RelatedRecordId = callLogId,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _context.ActivityLogs.AddAsync(activity);
                }

                // 4. Optionally schedule next follow-up if requested
                if (dto.ScheduleNext && dto.NextDueDate.HasValue)
                {
                    var nextTask = new TaskTable
                    {
                        TaskTitle = task.TaskTitle.StartsWith("Follow up:") ? task.TaskTitle : $"Follow up: {task.TaskTitle}",
                        TaskDescription = dto.NextMessage?.Trim() ?? "",
                        TaskStatus = "Todo",
                        TaskPriority = "Medium",
                        TaskDueDate = dto.NextDueDate.Value,
                        TaskAssignee = task.TaskAssignee,
                        AssigneeUserId = task.AssigneeUserId ?? userId,
                        RelatedLeadId = task.RelatedLeadId,
                        RelatedDealId = task.RelatedDealId,
                        TaskType = "Task",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        CreatedBy = userId,
                        UpdatedBy = userId,
                        LastModified = DateTime.UtcNow
                    };

                    await _context.Tasks.AddAsync(nextTask);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(task);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
