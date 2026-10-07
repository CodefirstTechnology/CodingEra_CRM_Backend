using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.models;
using CRM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers
{
    [Route("api/dashboard")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly TaskDbcontext _context;
        private readonly IRbacService _rbac;

        public DashboardController(TaskDbcontext context, IRbacService rbac)
        {
            _context = context;
            _rbac = rbac;
        }

        [HttpGet("stuck-pipeline")]
        public async Task<IActionResult> GetStuckPipeline([FromQuery] int? userId = null)
        {
            var now = DateTime.UtcNow;
            var dealThreshold = now.AddHours(-24);
            var leadThreshold = now.AddHours(-48);

            // 1. Fetch Open Deals
            var dealsQuery = _context.Deals.AsNoTracking()
                .Include(d => d.DealStatus)
                .Include(d => d.AssignedToUser)
                .Include(d => d.DealOwner)
                .Where(d => d.IsActive);

            if (userId.HasValue && userId.Value > 0)
            {
                var uid = userId.Value;
                if (!await _rbac.IsAdminUserAsync(uid))
                {
                    dealsQuery = dealsQuery.Where(d =>
                        d.DealOwnerId == uid ||
                        d.AssignedToUserId == uid ||
                        d.CreatedBy == uid);
                }
            }

            var allDeals = await dealsQuery.ToListAsync();

            // Exclude closed deals
            var openDeals = allDeals.Where(d =>
            {
                var st = d.Status?.Trim() ?? "";
                if (string.Equals(st, "Closed Won", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(st, "Closed Lost", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(st, "Lead Closed - Won", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(st, "Lead Closed - Lost", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                return true;
            }).ToList();

            // Apply latest quotation values if deal amount is zero or null
            await DealAmountHelper.ApplyLatestQuotationAmountsAsync(_context, openDeals);

            var openDealIds = openDeals.Select(d => d.Id).ToList();

            // Fetch latest activity timestamps for these open deals
            var dealActivities = new Dictionary<int, DateTime>();
            if (openDealIds.Count > 0)
            {
                dealActivities = await _context.ActivityLogs.AsNoTracking()
                    .Where(a => a.EntityType == ActivityEntityTypes.Deal && openDealIds.Contains(a.EntityId))
                    .GroupBy(a => a.EntityId)
                    .Select(g => new { DealId = g.Key, MaxActivity = g.Max(a => a.CreatedAt) })
                    .ToDictionaryAsync(x => x.DealId, x => x.MaxActivity);
            }

            // Identify Stuck Deals (> 24 hours inactive)
            var stuckDealsList = new List<StuckDealItemDto>();
            foreach (var deal in openDeals)
            {
                var recordDate = deal.UpdatedAt > DateTime.MinValue
                    ? deal.UpdatedAt
                    : (deal.LastModified > DateTime.MinValue ? deal.LastModified : deal.CreatedAt);

                if (dealActivities.TryGetValue(deal.Id, out var actDate) && actDate > recordDate)
                {
                    recordDate = actDate;
                }

                if (recordDate < dealThreshold)
                {
                    var idleHours = Math.Max(24, (int)Math.Floor((now - recordDate).TotalHours));
                    var idleDurationStr = FormatIdleDuration(idleHours);

                    var dealTitle = !string.IsNullOrWhiteSpace(deal.OrganizationName)
                        ? deal.OrganizationName
                        : (!string.IsNullOrWhiteSpace(deal.FirstName) || !string.IsNullOrWhiteSpace(deal.LastName)
                            ? $"{deal.FirstName} {deal.LastName}".Trim()
                            : $"Deal #{deal.Id}");

                    var contactName = $"{deal.FirstName} {deal.LastName}".Trim();
                    if (string.IsNullOrWhiteSpace(contactName)) contactName = deal.OrganizationName;

                    stuckDealsList.Add(new StuckDealItemDto
                    {
                        DealId = deal.Id,
                        DealTitle = dealTitle,
                        OrganizationName = deal.OrganizationName ?? "",
                        ContactName = contactName ?? "",
                        Stage = !string.IsNullOrWhiteSpace(deal.DealStatus?.Name) ? deal.DealStatus.Name : deal.Status,
                        DealAmount = deal.DealAmount.GetValueOrDefault(0),
                        LastActivityAt = recordDate,
                        IdleHours = idleHours,
                        IdleDurationFormatted = idleDurationStr,
                        OwnerId = deal.DealOwnerId ?? deal.AssignedToUserId ?? deal.CreatedBy,
                        OwnerName = deal.DealOwner?.FullName ?? deal.AssignedToUser?.FullName ?? "Unassigned"
                    });
                }
            }

            stuckDealsList = stuckDealsList.OrderByDescending(d => d.IdleHours).ToList();

            // 2. Fetch Idle Leads (status = 'New' with no interaction for > 48h)
            var leadsQuery = _context.Leads.AsNoTracking()
                .Include(l => l.LeadStatus)
                .Include(l => l.Organization)
                .Include(l => l.LeadOwner)
                .Where(l => l.IsActive);

            if (userId.HasValue && userId.Value > 0)
            {
                var uid = userId.Value;
                if (!await _rbac.IsAdminUserAsync(uid))
                {
                    leadsQuery = leadsQuery.Where(l => l.LeadOwnerId == uid || l.CreatedBy == uid);
                }
            }

            var allLeads = await leadsQuery.ToListAsync();

            var newLeads = allLeads.Where(l =>
            {
                var st = l.LeadStatus?.Name?.Trim() ?? "New";
                return string.Equals(st, "New", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(st, "Open", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(st, "Uncontacted", StringComparison.OrdinalIgnoreCase);
            }).ToList();

            var newLeadIds = newLeads.Select(l => l.Id).ToList();
            var leadActivities = new Dictionary<int, DateTime>();
            if (newLeadIds.Count > 0)
            {
                leadActivities = await _context.ActivityLogs.AsNoTracking()
                    .Where(a => a.EntityType == ActivityEntityTypes.Lead && newLeadIds.Contains(a.EntityId))
                    .GroupBy(a => a.EntityId)
                    .Select(g => new { LeadId = g.Key, MaxActivity = g.Max(a => a.CreatedAt) })
                    .ToDictionaryAsync(x => x.LeadId, x => x.MaxActivity);
            }

            var idleLeadsList = new List<IdleLeadItemDto>();
            foreach (var lead in newLeads)
            {
                DateTime? lastAct = null;
                if (leadActivities.TryGetValue(lead.Id, out var actDate))
                {
                    lastAct = actDate;
                }
                else
                {
                    lastAct = lead.CreatedAt ?? lead.LeadDate ?? lead.UpdatedAt;
                }

                if (lastAct == null || lastAct.Value < leadThreshold)
                {
                    var effectiveDate = lastAct ?? lead.UpdatedAt;
                    var idleHours = Math.Max(48, (int)Math.Floor((now - effectiveDate).TotalHours));
                    var idleDurationStr = FormatIdleDuration(idleHours);

                    var fullName = $"{lead.FirstName} {lead.LastName}".Trim();
                    if (string.IsNullOrWhiteSpace(fullName)) fullName = lead.Organization?.Name ?? $"Lead #{lead.Id}";

                    idleLeadsList.Add(new IdleLeadItemDto
                    {
                        LeadId = lead.Id,
                        LeadName = fullName,
                        OrganizationName = lead.Organization?.Name ?? "",
                        Status = lead.LeadStatus?.Name ?? "New",
                        CreatedAt = lead.CreatedAt ?? lead.LeadDate,
                        LastActivityAt = lastAct,
                        IdleHours = idleHours,
                        IdleDurationFormatted = idleDurationStr,
                        OwnerId = lead.LeadOwnerId ?? lead.CreatedBy,
                        OwnerName = lead.LeadOwner?.FullName ?? "Unassigned",
                        Mobile = lead.Mobile ?? "",
                        Email = lead.Email ?? ""
                    });
                }
            }

            idleLeadsList = idleLeadsList.OrderByDescending(l => l.IdleHours).ToList();

            // 3. Compute Aggregated Summary
            var totalStuckValue = stuckDealsList.Sum(d => d.DealAmount);
            var avgHours = stuckDealsList.Count > 0 ? stuckDealsList.Average(d => d.IdleHours) : 0;
            var avgDays = Math.Round(avgHours / 24.0, 1);
            var avgIdleStr = avgDays >= 1.0 ? $"{avgDays} Days" : $"{Math.Round(avgHours)} Hours";

            var response = new StuckPipelineResponseDto
            {
                Summary = new StuckPipelineSummaryDto
                {
                    StuckValue = totalStuckValue,
                    StuckDealsCount = stuckDealsList.Count,
                    AvgIdleHours = avgHours,
                    AvgIdleTimeFormatted = avgIdleStr,
                    IdleLeadsCount = idleLeadsList.Count
                },
                StuckDeals = stuckDealsList,
                IdleLeads = idleLeadsList
            };

            return Ok(response);
        }

        private static string FormatIdleDuration(int hours)
        {
            if (hours >= 48)
            {
                var days = hours / 24;
                return $"{days}d Inactive";
            }
            return $"{hours}h Inactive";
        }

        [HttpGet("sales-executive-performance-report")]
        public async Task<IActionResult> GetSalesExecutivePerformanceReport([FromQuery] SalesExecutiveReportQueryDto query)
        {
            var offset = TimeSpan.FromMinutes(-query.TimeZoneOffsetMinutes);
            var localStart = (query.StartDate ?? DateTime.UtcNow.Add(offset).AddDays(-30)).Date;
            var localEnd = (query.EndDate ?? localStart).Date;
            var startUtc = DateTime.SpecifyKind(localStart - offset, DateTimeKind.Utc);
            var endUtc = DateTime.SpecifyKind(localEnd.AddDays(1).AddTicks(-1) - offset, DateTimeKind.Utc);

            var usersQuery = _context.Users.AsNoTracking()
                .Include(u => u.Role)
                .AsQueryable();

            if (!query.IncludeInactiveUsers)
            {
                usersQuery = usersQuery.Where(u => u.IsActive);
            }
            if (query.RoleId.HasValue && query.RoleId.Value > 0)
            {
                usersQuery = usersQuery.Where(u => u.RoleId == query.RoleId.Value);
            }
            else
            {
                usersQuery = usersQuery.Where(u => u.Role != null && u.Role.Name.ToLower().Contains("sales"));
            }

            var users = await usersQuery.ToListAsync();
            var userIds = users.Select(u => u.Id).ToList();

            if (userIds.Count == 0)
            {
                return Ok(new SalesExecutiveReportResponseDto
                {
                    StartDate = localStart,
                    EndDate = localEnd,
                    Summary = new SalesExecutiveReportSummaryDto(),
                    Rows = new List<SalesExecutiveReportRowDto>()
                });
            }

            // 1. Leads in period
            var leadsInPeriod = await _context.Leads.AsNoTracking()
                .Where(l => l.IsActive && l.LeadOwnerId.HasValue && userIds.Contains(l.LeadOwnerId.Value) &&
                            l.CreatedAt >= startUtc && l.CreatedAt <= endUtc)
                .Select(l => new { l.Id, l.LeadOwnerId, l.LeadStatusId, l.DealAmount, l.Notes, l.Gender, l.Mobile, l.Email })
                .ToListAsync();

            // Positive status IDs
            var positiveStatusIds = await _context.LeadStatuses.AsNoTracking()
                .Where(s => s.IsActive && s.IsPositive)
                .Select(s => s.Id)
                .ToListAsync();

            // 2. Call logs for contacted count
            var callLogLeadPairs = await _context.CallLogs.AsNoTracking()
                .Where(c => c.IsActive && c.RelatedLeadId.HasValue && c.CreatedAt >= startUtc && c.CreatedAt <= endUtc &&
                            c.CreatedBy.HasValue && userIds.Contains(c.CreatedBy.Value))
                .Select(c => new { OwnerId = c.CreatedBy!.Value, LeadId = c.RelatedLeadId!.Value })
                .ToListAsync();

            // 3. Activity logs for lead interactions
            var activityLeadPairs = await _context.ActivityLogs.AsNoTracking()
                .Where(a => a.EntityType == ActivityEntityTypes.Lead && a.CreatedAt >= startUtc && a.CreatedAt <= endUtc &&
                            a.ActorUserId.HasValue && userIds.Contains(a.ActorUserId.Value))
                .Select(a => new { OwnerId = a.ActorUserId!.Value, LeadId = a.EntityId })
                .ToListAsync();

            // 4. Meetings in period
            var meetingsInPeriod = await _context.Tasks.AsNoTracking()
                .Where(t => t.IsActive && t.AssigneeUserId.HasValue && userIds.Contains(t.AssigneeUserId.Value) &&
                            t.CreatedAt >= startUtc && t.CreatedAt <= endUtc &&
                            (t.TaskType == "Meeting" ||
                             t.TaskTitle.ToLower().Contains("meeting") || t.TaskTitle.ToLower().Contains("demo") ||
                             t.TaskDescription.ToLower().Contains("meeting") || t.TaskDescription.ToLower().Contains("demo")))
                .Select(t => t.AssigneeUserId!.Value)
                .ToListAsync();

            // 5. Quotations in period
            var quotationsInPeriod = await _context.Quotations.AsNoTracking()
                .Where(q => q.CreatedBy.HasValue && userIds.Contains(q.CreatedBy.Value) &&
                            q.CreatedAt >= startUtc && q.CreatedAt <= endUtc &&
                            q.Status != "Draft" && q.Status != "Cancelled")
                .Select(q => new { CreatorId = q.CreatedBy!.Value, q.GrandTotal })
                .ToListAsync();

            // 6. Deals won in period
            var dealsWonInPeriod = await _context.Deals.AsNoTracking()
                .Where(d => d.IsActive &&
                            ((d.DealOwnerId.HasValue && userIds.Contains(d.DealOwnerId.Value)) ||
                             (d.CreatedBy.HasValue && userIds.Contains(d.CreatedBy.Value))) &&
                            d.UpdatedAt >= startUtc && d.UpdatedAt <= endUtc &&
                            (d.Status == "Closed Won" || d.Status == "Lead Closed - Won"))
                .Select(d => new { OwnerId = d.DealOwnerId ?? d.CreatedBy ?? 0, d.DealAmount })
                .ToListAsync();

            // 7. Open Leads (Point-in-time Snapshot)
            // 7. Open Leads created in period
            var openLeadsSnapshot = await _context.Leads.AsNoTracking()
                .Include(l => l.LeadStatus)
                .Where(l => l.IsActive && l.LeadOwnerId.HasValue && userIds.Contains(l.LeadOwnerId.Value) &&
                            l.CreatedAt >= startUtc && l.CreatedAt <= endUtc &&
                            (l.LeadStatus == null || (!l.LeadStatus.IsConversionStatus &&
                             !new[] { "closed won", "closed lost", "converted", "junk", "lead closed - won", "lead closed - lost" }
                                .Contains((l.LeadStatus.Name ?? "").Trim().ToLower()))))
                .Select(l => l.LeadOwnerId!.Value)
                .ToListAsync();

            // 8. Overdue Follow-ups in period (due in period but not completed)
            var overdueFollowupsSnapshot = await _context.Tasks.AsNoTracking()
                .Where(t => t.IsActive && t.AssigneeUserId.HasValue && userIds.Contains(t.AssigneeUserId.Value) &&
                            t.TaskDueDate >= startUtc && t.TaskDueDate <= endUtc &&
                            t.TaskStatus != "Completed")
                .Select(t => t.AssigneeUserId!.Value)
                .ToListAsync();

            // 9. Compliance Tasks due in period
            var tasksDueInPeriod = await _context.Tasks.AsNoTracking()
                .Where(t => t.IsActive && t.AssigneeUserId.HasValue && userIds.Contains(t.AssigneeUserId.Value) &&
                            t.TaskDueDate >= startUtc && t.TaskDueDate <= endUtc)
                .Select(t => new { AssigneeId = t.AssigneeUserId!.Value, t.TaskStatus, t.TaskDueDate, t.UpdatedAt })
                .ToListAsync();

            var rows = new List<SalesExecutiveReportRowDto>();
            var summary = new SalesExecutiveReportSummaryDto();
            int totalCompletedOnTime = 0;
            int totalDueTasks = 0;

            foreach (var user in users)
            {
                var uid = user.Id;
                var userLeads = leadsInPeriod.Where(l => l.LeadOwnerId == uid).ToList();
                var userLeadIds = userLeads.Select(l => l.Id).ToHashSet();

                int totalLeadsCount = userLeads.Count;
                decimal leadValueSum = userLeads.Sum(l => l.DealAmount ?? 0m);

                // Contacted unique leads
                var callLeadIdsForUser = callLogLeadPairs.Where(c => c.OwnerId == uid).Select(c => c.LeadId);
                var actLeadIdsForUser = activityLeadPairs.Where(a => a.OwnerId == uid).Select(a => a.LeadId);
                var contactedLeadIds = new HashSet<int>(callLeadIdsForUser.Concat(actLeadIdsForUser).Where(id => userLeadIds.Contains(id)));
                int contactedCount = Math.Max(contactedLeadIds.Count, userLeads.Count(l => l.LeadStatusId.HasValue || (l.Notes != null && l.Notes.Trim().Length > 0)));

                // Qualified leads
                int qualifiedCount = userLeads.Count(l => l.LeadStatusId.HasValue && positiveStatusIds.Contains(l.LeadStatusId.Value));

                // Meetings
                int meetingsCount = meetingsInPeriod.Count(id => id == uid);

                // Quotations
                var userQuotes = quotationsInPeriod.Where(q => q.CreatorId == uid).ToList();
                int quotesCount = userQuotes.Count;
                decimal quoteValueSum = userQuotes.Sum(q => q.GrandTotal);

                // Orders Won
                var userWonDeals = dealsWonInPeriod.Where(d => d.OwnerId == uid).ToList();
                int ordersWonCount = userWonDeals.Count;
                decimal orderValueSum = userWonDeals.Sum(d => d.DealAmount ?? 0m);

                // Follow-up compliance
                var userTasks = tasksDueInPeriod.Where(t => t.AssigneeId == uid).ToList();
                int dueCount = userTasks.Count;
                int onTimeCount = userTasks.Count(t => t.TaskStatus == "Completed" && t.UpdatedAt <= t.TaskDueDate.AddHours(24));

                totalDueTasks += dueCount;
                totalCompletedOnTime += onTimeCount;

                // Point in time snapshots
                int openLeadsCount = openLeadsSnapshot.Count(id => id == uid);
                int overdueCount = overdueFollowupsSnapshot.Count(id => id == uid);

                // Safe percentages
                double contactPct = SafePercentage(contactedCount, totalLeadsCount);
                double qualPct = SafePercentage(qualifiedCount, totalLeadsCount);
                double quotePct = SafePercentage(quotesCount, totalLeadsCount);
                double conversionPct = SafePercentage(ordersWonCount, totalLeadsCount);
                double compliancePct = dueCount > 0 ? SafePercentage(onTimeCount, dueCount) : 0.0;

                var row = new SalesExecutiveReportRowDto
                {
                    UserId = uid,
                    ExecutiveName = user.FullName ?? user.Email,
                    UserEmail = user.Email,
                    RoleName = user.Role?.Name ?? "Sales Representative",
                    TotalLeads = totalLeadsCount,
                    Contacted = contactedCount,
                    Qualified = qualifiedCount,
                    Meetings = meetingsCount,
                    Quotations = quotesCount,
                    OrdersWon = ordersWonCount,
                    LeadValue = leadValueSum,
                    QuotationValue = quoteValueSum,
                    OrderValue = orderValueSum,
                    ContactPercentage = contactPct,
                    QualificationPercentage = qualPct,
                    QuotePercentage = quotePct,
                    OrderConversionPercentage = conversionPct,
                    FollowUpCompliancePercentage = compliancePct,
                    OpenLeads = openLeadsCount,
                    OverdueFollowUps = overdueCount
                };

                rows.Add(row);

                // Accumulate summary numerators/denominators
                summary.TotalLeads += totalLeadsCount;
                summary.TotalContacted += contactedCount;
                summary.TotalQualified += qualifiedCount;
                summary.TotalMeetings += meetingsCount;
                summary.TotalQuotations += quotesCount;
                summary.TotalOrdersWon += ordersWonCount;
                summary.TotalLeadValue += leadValueSum;
                summary.TotalQuotationValue += quoteValueSum;
                summary.TotalOrderValue += orderValueSum;
                summary.TotalOpenLeads += openLeadsCount;
                summary.TotalOverdueFollowUps += overdueCount;
            }

            summary.TotalExecutives = rows.Count;
            summary.AverageContactPercentage = SafePercentage(summary.TotalContacted, summary.TotalLeads);
            summary.AverageQualificationPercentage = SafePercentage(summary.TotalQualified, summary.TotalLeads);
            summary.AverageQuotePercentage = SafePercentage(summary.TotalQuotations, summary.TotalLeads);
            summary.AverageOrderConversionPercentage = SafePercentage(summary.TotalOrdersWon, summary.TotalLeads);
            summary.AverageFollowUpCompliancePercentage = totalDueTasks > 0 ? SafePercentage(totalCompletedOnTime, totalDueTasks) : 0.0;

            return Ok(new SalesExecutiveReportResponseDto
            {
                StartDate = localStart,
                EndDate = localEnd,
                Summary = summary,
                Rows = rows
            });
        }

        [HttpGet("monthly-performance-summary")]
        public async Task<IActionResult> GetMonthlyPerformanceSummary(
            [FromQuery] int? year = null,
            [FromQuery] int timeZoneOffsetMinutes = -330)
        {
            var offset = TimeSpan.FromMinutes(-timeZoneOffsetMinutes);
            var nowLocal = DateTime.UtcNow.Add(offset);
            var targetYear = year.HasValue && year.Value > 2000 && year.Value < 2100
                ? year.Value
                : nowLocal.Year;

            var startOfYearLocal = new DateTime(targetYear, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var endOfYearLocal = new DateTime(targetYear + 1, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

            var startOfYearUtc = DateTime.SpecifyKind(startOfYearLocal - offset, DateTimeKind.Utc);
            var endOfYearUtc = DateTime.SpecifyKind(endOfYearLocal - offset, DateTimeKind.Utc);

            // 1. Batched Leads Query
            var rawLeads = await _context.Leads.AsNoTracking()
                .Include(l => l.LeadStatus)
                .Where(l => l.IsActive && l.CreatedAt >= startOfYearUtc && l.CreatedAt < endOfYearUtc)
                .Select(l => new
                {
                    l.Id,
                    l.CreatedAt,
                    DealAmount = l.DealAmount ?? 0m,
                    IsQualified = (l.LeadStatus != null && (l.LeadStatus.IsPositive || l.LeadStatus.IsConversionStatus || l.LeadStatus.Name == "Qualified" || l.LeadStatus.Name == "Interested"))
                })
                .ToListAsync();

            // 2. Batched Quotations Query
            var rawQuotes = await _context.Quotations.AsNoTracking()
                .Where(q => q.Status != "Draft" && q.Status != "Cancelled" &&
                            q.CreatedAt >= startOfYearUtc && q.CreatedAt < endOfYearUtc)
                .Select(q => new
                {
                    q.Id,
                    q.CreatedAt,
                    GrandTotal = q.GrandTotal
                })
                .ToListAsync();

            // 3. Batched Deals Query
            var rawDeals = await _context.Deals.AsNoTracking()
                .Include(d => d.DealStatus)
                .Where(d => d.IsActive && d.CreatedAt >= startOfYearUtc && d.CreatedAt < endOfYearUtc)
                .Select(d => new
                {
                    d.Id,
                    d.CreatedAt,
                    DealAmount = d.DealAmount ?? 0m,
                    Status = d.Status ?? "",
                    IsWon = (d.DealStatus != null && d.DealStatus.IsWon == true) ||
                            (d.Status != null && (d.Status == "Closed Won" || d.Status == "Lead Closed - Won" || d.Status == "Won"))
                })
                .ToListAsync();

            int GetLocalMonthNumber(DateTime? utcDt)
            {
                if (!utcDt.HasValue) return 0;
                var localDt = utcDt.Value.Add(offset);
                return localDt.Month;
            }

            var monthNames = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
            var monthRows = new List<MonthlySummaryRowDto>();

            for (int m = 1; m <= 12; m++)
            {
                var mLeads = rawLeads.Where(l => GetLocalMonthNumber(l.CreatedAt) == m).ToList();
                var mQuotes = rawQuotes.Where(q => GetLocalMonthNumber(q.CreatedAt) == m).ToList();
                var mDeals = rawDeals.Where(d => GetLocalMonthNumber(d.CreatedAt) == m).ToList();

                var totalLeads = mLeads.Count;
                var qualifiedLeads = mLeads.Count(l => l.IsQualified);
                var quotationsCount = mQuotes.Count;
                var ordersWonCount = mDeals.Count(d => d.IsWon);

                var leadValue = mLeads.Sum(l => l.DealAmount);
                var quotationValue = mQuotes.Sum(q => q.GrandTotal);
                var orderValue = mDeals.Where(d => d.IsWon).Sum(d => d.DealAmount);

                monthRows.Add(new MonthlySummaryRowDto
                {
                    MonthLabel = $"{monthNames[m - 1]}-{targetYear}",
                    MonthNumber = m,
                    TotalLeads = totalLeads,
                    QualifiedLeads = qualifiedLeads,
                    QuotationsCount = quotationsCount,
                    OrdersWonCount = ordersWonCount,
                    LeadValue = leadValue,
                    QuotationValue = quotationValue,
                    OrderValue = orderValue,
                    LeadToQuotePercentage = SafePercentage(quotationsCount, totalLeads),
                    QuoteToOrderPercentage = SafePercentage(ordersWonCount, quotationsCount),
                    LeadToOrderPercentage = SafePercentage(ordersWonCount, totalLeads)
                });
            }

            var totals = new MonthlySummaryTotalsDto
            {
                TotalLeads = monthRows.Sum(r => r.TotalLeads),
                QualifiedLeads = monthRows.Sum(r => r.QualifiedLeads),
                QuotationsCount = monthRows.Sum(r => r.QuotationsCount),
                OrdersWonCount = monthRows.Sum(r => r.OrdersWonCount),
                LeadValue = monthRows.Sum(r => r.LeadValue),
                QuotationValue = monthRows.Sum(r => r.QuotationValue),
                OrderValue = monthRows.Sum(r => r.OrderValue)
            };

            totals.LeadToQuotePercentage = SafePercentage(totals.QuotationsCount, totals.TotalLeads);
            totals.QuoteToOrderPercentage = SafePercentage(totals.OrdersWonCount, totals.QuotationsCount);
            totals.LeadToOrderPercentage = SafePercentage(totals.OrdersWonCount, totals.TotalLeads);

            return Ok(new MonthlyPerformanceReportResponse
            {
                SelectedYear = targetYear,
                Months = monthRows,
                Totals = totals
            });
        }

        private static double SafePercentage(double numerator, double denominator)
        {
            if (denominator <= 0) return 0.0;
            var pct = (numerator / denominator) * 100.0;
            return Math.Min(100.0, Math.Round(pct, 1));
        }
    }
}

