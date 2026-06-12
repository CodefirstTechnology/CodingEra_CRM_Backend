using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.models;
using CRM.Services;
using Microsoft.EntityFrameworkCore;

namespace CRM.Services
{
    public interface IDashboardService
    {
        Task<UserDashboardPreferenceDto> GetPreferencesAsync(int userId, CancellationToken cancellationToken = default);
        Task<UserDashboardPreferenceDto> UpdatePreferencesAsync(
            int userId,
            UserDashboardPreferenceUpdateDto dto,
            CancellationToken cancellationToken = default);
        Task MarkBriefingPlayedAsync(int userId, CancellationToken cancellationToken = default);
        Task<DashboardUserSummaryDto> BuildAdminBusinessSummaryAsync(
            int userId,
            CancellationToken cancellationToken = default);
        Task<MorningBriefingResponseDto> GenerateMorningBriefingAsync(
            int userId,
            DailyBriefingMetricsDto metrics,
            bool forceRegenerate = false,
            CancellationToken cancellationToken = default);

        Task<MorningBriefingResponseDto?> GetCachedMorningBriefingAsync(
            int userId,
            CancellationToken cancellationToken = default);
        Task ResetBriefingForTestingAsync(int userId, CancellationToken cancellationToken = default);
    }

    public class DashboardService : IDashboardService
    {
        private static readonly HashSet<string> HighPriorityLeadStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "Qualified",
            "Converted",
        };

        private readonly TaskDbcontext _db;
        private readonly IRbacService _rbac;
        private readonly IUserTargetService _userTargets;
        private readonly IMorningBriefingAiService _briefingAi;

        public DashboardService(
            TaskDbcontext db,
            IRbacService rbac,
            IUserTargetService userTargets,
            IMorningBriefingAiService briefingAi)
        {
            _db = db;
            _rbac = rbac;
            _userTargets = userTargets;
            _briefingAi = briefingAi;
        }

        public async Task<UserDashboardPreferenceDto> GetPreferencesAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            var row = await EnsurePreferenceRowAsync(userId, cancellationToken);
            return MapPreference(row);
        }

        public async Task<UserDashboardPreferenceDto> UpdatePreferencesAsync(
            int userId,
            UserDashboardPreferenceUpdateDto dto,
            CancellationToken cancellationToken = default)
        {
            var row = await EnsurePreferenceRowAsync(userId, cancellationToken);
            row.MorningBriefingEnabled = dto.MorningBriefingEnabled;
            row.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return MapPreference(row);
        }

        public async Task MarkBriefingPlayedAsync(int userId, CancellationToken cancellationToken = default)
        {
            var row = await EnsurePreferenceRowAsync(userId, cancellationToken);
            row.LastBriefingPlayedDate = DateOnly.FromDateTime(DateTime.UtcNow);
            row.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<DashboardUserSummaryDto> BuildAdminBusinessSummaryAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            if (!await _rbac.IsAdminUserAsync(userId))
            {
                throw new UnauthorizedAccessException("Daily business summary is available to administrators only.");
            }

            var user = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken)
                ?? throw new InvalidOperationException("User not found.");

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var todayStart = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var tomorrowStart = today.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var dealStatuses = DealStageValidationHelper.OrderPipeline(
                await _db.DealStatuses.AsNoTracking().ToListAsync(cancellationToken));

            var leads = await _db.Leads.AsNoTracking()
                .Include(l => l.LeadStatus)
                .Where(l => l.IsActive)
                .ToListAsync(cancellationToken);

            var deals = await _db.Deals.AsNoTracking()
                .Where(d => d.IsActive)
                .ToListAsync(cancellationToken);

            var tasks = await _db.Tasks.AsNoTracking()
                .Where(t => t.IsActive)
                .ToListAsync(cancellationToken);

            var pendingTasks = tasks.Where(t => !IsTaskComplete(t.TaskStatus)).ToList();
            var followUpsToday = 0;
            var meetingsToday = 0;
            var overdueFollowUps = 0;
            var pendingFollowUps = 0;
            var tasksDueToday = 0;
            var overdueLeadIds = new HashSet<int>();

            foreach (var task in pendingTasks)
            {
                var dueDate = DateOnly.FromDateTime(task.TaskDueDate.ToUniversalTime());
                var isMeeting = IsMeetingTask(task.TaskTitle);

                if (dueDate == today)
                {
                    tasksDueToday++;
                }

                if (isMeeting)
                {
                    if (dueDate <= today)
                    {
                        meetingsToday++;
                    }

                    continue;
                }

                pendingFollowUps++;

                if (dueDate < today)
                {
                    overdueFollowUps++;
                    if (task.RelatedLeadId is > 0)
                    {
                        overdueLeadIds.Add(task.RelatedLeadId.Value);
                    }
                }
                else if (dueDate == today)
                {
                    followUpsToday++;
                }
            }

            var activeDeals = deals.Where(d => !DealStageValidationHelper.IsClosed(d.Status, dealStatuses)).ToList();
            var closedWonDeals = deals.Where(d => DealStageValidationHelper.IsClosedWon(d.Status, dealStatuses)).ToList();
            var closedLostDeals = deals.Where(d => DealStageValidationHelper.IsClosedLost(d.Status, dealStatuses)).ToList();

            var newLeadsToday = leads.Count(l =>
                l.CreatedAt.HasValue && l.CreatedAt.Value.ToUniversalTime() >= todayStart);

            var newDealsToday = deals.Count(d => d.CreatedAt.ToUniversalTime() >= todayStart);

            var dealsPendingClosure = activeDeals.Count(d => IsDealExpectedToCloseToday(d, today));

            var dealsWonToday = closedWonDeals.Count(d =>
                d.LastModified.ToUniversalTime() >= todayStart && d.LastModified.ToUniversalTime() < tomorrowStart);

            var dealsLostToday = closedLostDeals.Count(d =>
                d.LastModified.ToUniversalTime() >= todayStart && d.LastModified.ToUniversalTime() < tomorrowStart);

            var wonTodayDeals = closedWonDeals.Where(d =>
                d.LastModified.ToUniversalTime() >= todayStart && d.LastModified.ToUniversalTime() < tomorrowStart);

            var revenueToday = wonTodayDeals.Sum(d => d.AnnualRevenue ?? d.DealAmount ?? 0m);
            decimal? revenueTodayValue = revenueToday > 0 ? revenueToday : null;

            var monthlyClosedWon = closedWonDeals.Where(d => d.LastModified.ToUniversalTime() >= monthStart).ToList();
            var monthlyRevenueUsd = monthlyClosedWon.Sum(d => d.AnnualRevenue ?? d.DealAmount ?? 0m);
            var monthlyRevenueInr = AdminDashboardBriefingMetrics.ToInr(monthlyRevenueUsd);
            var pipelineRevenueInr = AdminDashboardBriefingMetrics.ToInr(
                activeDeals.Sum(d => d.AnnualRevenue ?? d.DealAmount ?? 0m));
            var monthlyTargetInr = AdminDashboardBriefingMetrics.ToInr(AdminDashboardBriefingMetrics.MonthlyTargetUsd);
            var monthlyTargetAchievedPct = monthlyTargetInr > 0
                ? Math.Min(100m, Math.Round(monthlyRevenueInr / monthlyTargetInr * 100m, 0))
                : 0m;

            var qualifiedLeads = AdminDashboardBriefingMetrics.CountByStatus(leads, "Qualified");
            var convertedLeads = AdminDashboardBriefingMetrics.CountByStatus(leads, "Converted");
            var conversionPct = AdminDashboardBriefingMetrics.ConversionRatePct(leads);
            var nowUtc = DateTime.UtcNow;
            var newLeadsThisMonth = leads.Count(l =>
                AdminDashboardBriefingMetrics.IsLeadInCurrentMonth(l, monthStart, nowUtc));
            var qualifiedLeadsThisMonth = leads.Count(l =>
                string.Equals(l.LeadStatus?.Name, "Qualified", StringComparison.OrdinalIgnoreCase)
                && AdminDashboardBriefingMetrics.IsLeadInCurrentMonth(l, monthStart, nowUtc));
            var stuckDealsCount = AdminDashboardBriefingMetrics.CountStuckDeals(activeDeals, nowUtc);
            var pipelineSegments = AdminDashboardBriefingMetrics.BuildPipelineSegments(activeDeals);

            var highPriorityLeads = leads.Count(l =>
                HighPriorityLeadStatuses.Contains(l.LeadStatus?.Name ?? string.Empty)
                && overdueLeadIds.Contains(l.Id));

            var targetWidgets = await _userTargets.ListMyWidgetsAsync(userId, cancellationToken);
            var targetProgress = targetWidgets.Select(w => new DashboardTargetProgressDto
            {
                TargetTypeName = w.TargetTypeName,
                AchievementPercent = w.AchievementPercent,
                TargetAmount = w.TargetAmount,
                AchievedAmount = w.AchievedAmount,
            }).ToList();

            decimal? targetProgressPct = null;
            if (targetProgress.Count > 0)
            {
                var maxPct = targetProgress.Max(t => t.AchievementPercent);
                if (maxPct > 0)
                {
                    targetProgressPct = maxPct;
                }
            }

            return new DashboardUserSummaryDto
            {
                UserName = user.FullName.Trim(),
                TotalLeads = leads.Count,
                QualifiedLeads = qualifiedLeads,
                ConvertedLeads = convertedLeads,
                ConversionRatePct = conversionPct,
                NewLeadsThisMonth = newLeadsThisMonth,
                NewLeadsToday = newLeadsToday,
                PendingFollowUps = pendingFollowUps,
                FollowUpsToday = followUpsToday,
                OverdueFollowUps = overdueFollowUps,
                ActiveDeals = activeDeals.Count,
                PipelineRevenueInr = pipelineRevenueInr,
                NewDealsToday = newDealsToday,
                DealsPendingClosure = dealsPendingClosure,
                DealsWonToday = dealsWonToday,
                DealsLostToday = dealsLostToday,
                MeetingsToday = meetingsToday,
                TasksDueToday = tasksDueToday,
                HighPriorityLeads = highPriorityLeads,
                StuckDealsCount = stuckDealsCount,
                QualifiedLeadsThisMonth = qualifiedLeadsThisMonth,
                MonthlyRevenueInr = monthlyRevenueInr,
                MonthlyTargetInr = monthlyTargetInr,
                MonthlyTargetAchievedPct = monthlyTargetAchievedPct,
                RevenueToday = revenueTodayValue,
                TargetProgressPercent = targetProgressPct,
                TargetProgress = targetProgress,
                PipelineSegments = pipelineSegments,
                PendingTasks = pendingTasks.Count,
                MonthlyRevenue = monthlyRevenueUsd,
                CompletedTasks = tasks.Count(t =>
                    string.Equals(t.TaskStatus, "Done", StringComparison.OrdinalIgnoreCase)),
                ConversionPercent = conversionPct,
                ClosedDeals = closedWonDeals.Count,
            };
        }

        public async Task ResetBriefingForTestingAsync(int userId, CancellationToken cancellationToken = default)
        {
            if (!await _rbac.IsAdminUserAsync(userId))
            {
                throw new UnauthorizedAccessException("Daily business summary is available to administrators only.");
            }

            var pref = await EnsurePreferenceRowAsync(userId, cancellationToken);
            pref.LastBriefingPlayedDate = null;
            pref.CachedBriefingDate = null;
            pref.CachedBriefingMessage = null;
            pref.CachedBriefingSource = null;
            pref.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<MorningBriefingResponseDto?> GetCachedMorningBriefingAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            if (!await _rbac.IsAdminUserAsync(userId))
            {
                throw new UnauthorizedAccessException("Daily business summary is available to administrators only.");
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var pref = await EnsurePreferenceRowAsync(userId, cancellationToken);

            if (pref.CachedBriefingDate != today
                || string.IsNullOrWhiteSpace(pref.CachedBriefingMessage))
            {
                return null;
            }

            return new MorningBriefingResponseDto
            {
                Message = pref.CachedBriefingMessage.Trim(),
                Source = pref.CachedBriefingSource ?? "cached",
                Cached = true,
                Metrics = new DailyBriefingMetricsDto(),
            };
        }

        public async Task<MorningBriefingResponseDto> GenerateMorningBriefingAsync(
            int userId,
            DailyBriefingMetricsDto metrics,
            bool forceRegenerate = false,
            CancellationToken cancellationToken = default)
        {
            if (!await _rbac.IsAdminUserAsync(userId))
            {
                throw new UnauthorizedAccessException("Daily business summary is available to administrators only.");
            }

            if (metrics == null)
            {
                throw new ArgumentNullException(nameof(metrics));
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var pref = await EnsurePreferenceRowAsync(userId, cancellationToken);

            if (!forceRegenerate
                && pref.CachedBriefingDate == today
                && !string.IsNullOrWhiteSpace(pref.CachedBriefingMessage))
            {
                return new MorningBriefingResponseDto
                {
                    Message = pref.CachedBriefingMessage.Trim(),
                    Source = pref.CachedBriefingSource ?? "cached",
                    Cached = true,
                    Metrics = metrics,
                };
            }

            var (message, source) = await _briefingAi.GenerateBriefingAsync(metrics, cancellationToken);

            pref.CachedBriefingDate = today;
            pref.CachedBriefingMessage = message;
            pref.CachedBriefingSource = source;
            pref.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            return new MorningBriefingResponseDto
            {
                Message = message,
                Source = source,
                Cached = false,
                Metrics = metrics,
            };
        }

        private static bool IsDealExpectedToCloseToday(Deal deal, DateOnly today)
        {
            if (deal.NextFollowUpDate is not { } followUp)
            {
                return false;
            }

            return DateOnly.FromDateTime(followUp.ToUniversalTime()) == today;
        }

        private async Task<UserDashboardPreference> EnsurePreferenceRowAsync(
            int userId,
            CancellationToken cancellationToken)
        {
            var row = await _db.UserDashboardPreferences.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
            if (row != null)
            {
                return row;
            }

            row = new UserDashboardPreference
            {
                UserId = userId,
                MorningBriefingEnabled = true,
                UpdatedAt = DateTime.UtcNow,
            };
            _db.UserDashboardPreferences.Add(row);
            await _db.SaveChangesAsync(cancellationToken);
            return row;
        }

        private static UserDashboardPreferenceDto MapPreference(UserDashboardPreference row) =>
            new()
            {
                MorningBriefingEnabled = row.MorningBriefingEnabled,
                LastBriefingPlayedDate = row.LastBriefingPlayedDate?.ToString("yyyy-MM-dd"),
            };

        private static bool IsTaskComplete(string status) =>
            string.Equals(status, "Done", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Canceled", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase);

        private static bool IsMeetingTask(string title) =>
            title.Contains("meeting", StringComparison.OrdinalIgnoreCase)
            || title.Contains("review", StringComparison.OrdinalIgnoreCase)
            || title.Contains("demo", StringComparison.OrdinalIgnoreCase);
    }
}
