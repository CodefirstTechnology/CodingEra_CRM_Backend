using CRM.DATA;
using CRM.DTO;
using CRM.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CRM.Helpers
{
    /// <summary>
    /// Tasks and notes linked to a lead or deal are owned by that record's assignee (lead owner / deal assignee).
    /// </summary>
    public static class RelatedRecordOwnership
    {
        public static async Task<int?> ResolveLeadOwnerUserIdAsync(TaskDbcontext db, int? leadId)
        {
            if (leadId is not > 0)
            {
                return null;
            }

            return await db.Leads.AsNoTracking()
                .Where(l => l.Id == leadId)
                .Select(l => l.LeadOwnerId)
                .FirstOrDefaultAsync();
        }

        public static async Task<int?> ResolveDealOwnerUserIdAsync(TaskDbcontext db, int? dealId)
        {
            if (dealId is not > 0)
            {
                return null;
            }

            var row = await db.Deals.AsNoTracking()
                .Where(d => d.Id == dealId)
                .Select(d => new { d.AssignedToUserId, d.DealOwnerId })
                .FirstOrDefaultAsync();

            if (row == null)
            {
                return null;
            }

            return row.AssignedToUserId ?? row.DealOwnerId;
        }

        private static async Task<int?> ResolveOwnerUserIdAsync(TaskDbcontext db, int? relatedLeadId, int? relatedDealId)
        {
            if (relatedLeadId is > 0)
            {
                return await ResolveLeadOwnerUserIdAsync(db, relatedLeadId);
            }

            if (relatedDealId is > 0)
            {
                return await ResolveDealOwnerUserIdAsync(db, relatedDealId);
            }

            return null;
        }

        public static async Task<string?> ResolveUserDisplayNameAsync(TaskDbcontext db, int userId)
        {
            var row = await db.Users.AsNoTracking()
                .Where(u => u.Id == userId && u.IsActive)
                .Select(u => new { u.FullName, u.Email })
                .FirstOrDefaultAsync();

            if (row == null)
            {
                return null;
            }

            var name = row.FullName?.Trim();
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            var email = row.Email?.Trim();
            return !string.IsNullOrEmpty(email) ? email : $"User #{userId}";
        }

        public static async Task ApplyTaskAssigneeFromRelatedRecordAsync(TaskDbcontext db, TaskUpsertDto dto)
        {
            var ownerId = await ResolveOwnerUserIdAsync(db, dto.RelatedLeadId, dto.RelatedDealId);
            if (ownerId is not > 0)
            {
                return;
            }

            dto.AssigneeUserId = ownerId;
            var display = await ResolveUserDisplayNameAsync(db, ownerId.Value);
            if (!string.IsNullOrWhiteSpace(display))
            {
                dto.TaskAssignee = display;
            }
        }

        public static async Task ApplyNoteAuthorFromRelatedRecordAsync(TaskDbcontext db, NoteUpsertDto dto)
        {
            var ownerId = await ResolveOwnerUserIdAsync(db, dto.RelatedLeadId, dto.RelatedDealId);
            if (ownerId is not > 0)
            {
                return;
            }

            dto.AuthorId = ownerId;
            var display = await ResolveUserDisplayNameAsync(db, ownerId.Value);
            if (!string.IsNullOrWhiteSpace(display))
            {
                dto.Name = display;
            }
        }

        /// <summary>
        /// When a lead is reassigned to a new owner, automatically updates all active/pending follow-up tasks
        /// linked to that lead to the new owner, resets alert tracking, and emits real-time SignalR events.
        /// </summary>
        public static async Task SyncActiveTasksAssigneeForLeadAsync(
            TaskDbcontext db,
            IHubContext<UserStatusHub> hubContext,
            int leadId,
            int newOwnerId)
        {
            var activeTasks = await db.Tasks
                .Where(t => t.RelatedLeadId == leadId &&
                            t.IsActive &&
                            t.TaskStatus != "Completed" &&
                            t.TaskStatus != "Done" &&
                            t.TaskStatus != "Canceled")
                .ToListAsync();

            if (activeTasks.Count == 0)
            {
                return;
            }

            var newOwnerName = await ResolveUserDisplayNameAsync(db, newOwnerId) ?? $"User #{newOwnerId}";

            var lead = await db.Leads.AsNoTracking()
                .Include(l => l.Organization)
                .FirstOrDefaultAsync(l => l.Id == leadId);

            string customerName = lead?.Organization != null && !string.IsNullOrWhiteSpace(lead.Organization.Name)
                ? lead.Organization.Name
                : (lead != null ? (lead.FirstName + " " + lead.LastName).Trim() : "Customer");
            string contactPerson = lead != null ? (lead.FirstName + " " + lead.LastName).Trim() : string.Empty;
            string mobile = lead?.Mobile ?? string.Empty;

            var nowUtc = DateTime.UtcNow;

            foreach (var task in activeTasks)
            {
                var oldAssigneeId = task.AssigneeUserId;
                if (oldAssigneeId == newOwnerId)
                {
                    continue;
                }

                task.AssigneeUserId = newOwnerId;
                task.TaskAssignee = newOwnerName;
                task.UpdatedAt = nowUtc;
                task.LastModified = nowUtc;

                // Reset SLA alert tracking timestamps for the new assignee
                task.FirstDueAlertSentAt = null;
                task.LastRepetitionAlertSentAt = null;

                // 1. Dismiss active popup from old assignee's SignalR channel
                if (oldAssigneeId.HasValue)
                {
                    var dismissEvent = new DismissFollowUpPopupDto
                    {
                        TaskId = task.TaskId,
                        LeadId = leadId,
                        Reason = "REASSIGNED"
                    };
                    await hubContext.Clients.Group($"User_{oldAssigneeId.Value}")
                        .SendAsync("DismissFollowUpPopup", dismissEvent);
                }

                // 2. If the task is overdue, push alert to new assignee and admin dashboard immediately
                if (task.TaskDueDate <= nowUtc)
                {
                    var overdueMins = (int)Math.Max(0, Math.Floor((nowUtc - task.TaskDueDate).TotalMinutes));
                    var popupPayload = new FollowUpPopupEventDto
                    {
                        TaskId = task.TaskId,
                        LeadId = leadId,
                        LeadFormattedId = $"L{leadId:D4}",
                        CustomerName = customerName,
                        ContactPerson = contactPerson,
                        Mobile = mobile,
                        TaskTitle = task.TaskTitle,
                        DueDate = task.TaskDueDate,
                        MinutesOverdue = overdueMins,
                        IsInitialDue = true
                    };

                    await hubContext.Clients.Group($"User_{newOwnerId}")
                        .SendAsync("FollowUpReminderPopup", popupPayload);

                    var adminPayload = new FollowUpAdminEscalationDto
                    {
                        TaskId = task.TaskId,
                        LeadId = leadId,
                        LeadFormattedId = $"L{leadId:D4}",
                        CustomerName = customerName,
                        SalesExecutiveName = newOwnerName,
                        SalesExecutiveUserId = newOwnerId,
                        TaskTitle = task.TaskTitle,
                        DueDate = task.TaskDueDate,
                        MinutesOverdue = overdueMins,
                        Severity = overdueMins >= 60 ? "CRITICAL" : "OVERDUE",
                        EscalatedAt = task.EscalatedToAdminAt ?? nowUtc
                    };

                    await hubContext.Clients.Group(UserStatusHub.AdminsGroupName)
                        .SendAsync("FollowUpAdminEscalation", adminPayload);
                }
            }
        }
    }
}
