using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.DATA;
using CRM.DTO;
using CRM.Hubs;
using CRM.models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CRM.Services
{
    /// <summary>
    /// Background Hosted Service running every 60 seconds to evaluate:
    /// 1. 30-minute persistent repetition popup reminders for assigned Sales Executives.
    /// 2. 60-minute critical SLA breach escalation to Administrators and Managers.
    /// </summary>
    public class FollowUpSlaWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<FollowUpSlaWorker> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(60);

        public FollowUpSlaWorker(IServiceScopeFactory scopeFactory, ILogger<FollowUpSlaWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[SLA Worker] Follow-up SLA Escalation & Persistent Alert Worker started.");

            using var timer = new PeriodicTimer(_checkInterval);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessFollowUpSlaAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[SLA Worker] Error occurred while processing follow-up SLA worker cycle.");
                }

                if (!await timer.WaitForNextTickAsync(stoppingToken))
                {
                    break;
                }
            }

            _logger.LogInformation("[SLA Worker] Follow-up SLA Escalation Worker stopped.");
        }

        private async Task ProcessFollowUpSlaAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TaskDbcontext>();
            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<UserStatusHub>>();

            var nowUtc = DateTime.UtcNow;

            // Fetch uncompleted overdue or due tasks linked to active leads
            var dueTasks = await context.Tasks
                .Where(t => t.IsActive &&
                            t.RelatedLeadId.HasValue &&
                            t.AssigneeUserId.HasValue &&
                            t.TaskStatus != "Completed" &&
                            t.TaskStatus != "Done" &&
                            t.TaskStatus != "Canceled" &&
                            t.TaskDueDate <= nowUtc)
                .ToListAsync(cancellationToken);

            if (dueTasks.Count == 0)
            {
                return;
            }

            var leadIds = dueTasks.Select(t => t.RelatedLeadId!.Value).Distinct().ToList();
            var userIds = dueTasks.Select(t => t.AssigneeUserId!.Value).Distinct().ToList();

            var leads = await context.Leads.AsNoTracking()
                .Include(l => l.Organization)
                .Where(l => leadIds.Contains(l.Id))
                .Select(l => new
                {
                    l.Id,
                    CustomerName = l.Organization != null && !string.IsNullOrWhiteSpace(l.Organization.Name)
                        ? l.Organization.Name
                        : (l.FirstName + " " + l.LastName).Trim(),
                    ContactPerson = (l.FirstName + " " + l.LastName).Trim(),
                    Mobile = l.Mobile ?? string.Empty
                })
                .ToDictionaryAsync(l => l.Id, cancellationToken);

            var users = await context.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FullName, u.Email })
                .ToDictionaryAsync(u => u.Id, cancellationToken);

            bool stateChanged = false;

            foreach (var task in dueTasks)
            {
                if (!leads.TryGetValue(task.RelatedLeadId!.Value, out var lead))
                {
                    continue;
                }

                users.TryGetValue(task.AssigneeUserId!.Value, out var assignee);

                var overdueMinutes = (int)Math.Max(0, Math.Floor((nowUtc - task.TaskDueDate).TotalMinutes));
                var leadFormattedId = $"L{task.RelatedLeadId.Value:D4}";

                // -----------------------------------------------------------------------------------------
                // RULE 1: Executive Alert (Sent specifically to this assigned user's group)
                // -----------------------------------------------------------------------------------------
                bool isInitial = task.FirstDueAlertSentAt == null;
                bool needsRepetition = task.LastRepetitionAlertSentAt == null ||
                                       (nowUtc - task.LastRepetitionAlertSentAt.Value).TotalMinutes >= 30;

                if (isInitial || needsRepetition)
                {
                    var popupPayload = new FollowUpPopupEventDto
                    {
                        TaskId = task.TaskId,
                        LeadId = task.RelatedLeadId.Value,
                        LeadFormattedId = leadFormattedId,
                        CustomerName = lead.CustomerName,
                        ContactPerson = lead.ContactPerson,
                        Mobile = lead.Mobile,
                        TaskTitle = task.TaskTitle,
                        DueDate = task.TaskDueDate,
                        MinutesOverdue = overdueMinutes,
                        IsInitialDue = isInitial
                    };

                    // Broadcast to executive's individual SignalR channel
                    await hubContext.Clients.Group($"User_{task.AssigneeUserId.Value}")
                        .SendAsync("FollowUpReminderPopup", popupPayload, cancellationToken);

                    task.FirstDueAlertSentAt ??= nowUtc;
                    task.LastRepetitionAlertSentAt = nowUtc;
                    stateChanged = true;
                }

                // -----------------------------------------------------------------------------------------
                // RULE 2: Admin Dashboard Alert (Sent to AdminsGroup for ALL overdue follow-ups)
                // -----------------------------------------------------------------------------------------
                var adminPayload = new FollowUpAdminEscalationDto
                {
                    TaskId = task.TaskId,
                    LeadId = task.RelatedLeadId.Value,
                    LeadFormattedId = leadFormattedId,
                    CustomerName = lead.CustomerName,
                    SalesExecutiveName = assignee?.FullName ?? assignee?.Email ?? "Sales Representative",
                    SalesExecutiveUserId = task.AssigneeUserId.Value,
                    TaskTitle = task.TaskTitle,
                    DueDate = task.TaskDueDate,
                    MinutesOverdue = overdueMinutes,
                    Severity = overdueMinutes >= 60 ? "CRITICAL" : "OVERDUE",
                    EscalatedAt = task.EscalatedToAdminAt ?? nowUtc
                };

                // Broadcast ALL overdue follow-ups to AdminsGroup
                await hubContext.Clients.Group(UserStatusHub.AdminsGroupName)
                    .SendAsync("FollowUpAdminEscalation", adminPayload, cancellationToken);

                if (!task.IsEscalatedToAdmin)
                {
                    task.IsEscalatedToAdmin = true;
                    task.EscalatedToAdminAt = nowUtc;
                    stateChanged = true;

                    _logger.LogInformation("[SLA Alert] Overdue task #{TaskId} for Lead #{LeadId} broadcast to Admin Dashboard. Overdue: {Mins} mins.",
                        task.TaskId, task.RelatedLeadId.Value, overdueMinutes);
                }
            }

            if (stateChanged)
            {
                await context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
