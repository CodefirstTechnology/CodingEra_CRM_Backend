using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.DATA;
using CRM.DTO;
using CRM.models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Services
{
    public class DailySalesRecognitionService : IDailySalesRecognitionService
    {
        private readonly TaskDbcontext _context;

        public DailySalesRecognitionService(TaskDbcontext context)
        {
            _context = context;
        }

        public static (decimal dailyActivityScore, decimal businessResultScore, decimal overallScore, string recognition) CalculateScores(
            int newLeads, int customerCalls, int meetings, int followUps,
            int quotations, int leadConversions, int ordersBooked, int paymentFollowUps,
            decimal reportingPct, int teamSupport, decimal leadQualityPct)
        {
            decimal act = Math.Min(100m,
                (Math.Min((decimal)newLeads / 5m, 1m) * 15m) +
                (Math.Min((decimal)customerCalls / 20m, 1m) * 10m) +
                (Math.Min((decimal)meetings / 3m, 1m) * 15m) +
                (Math.Min((decimal)followUps / 10m, 1m) * 15m) +
                (Math.Min((decimal)quotations / 5m, 1m) * 10m) +
                (Math.Min((decimal)leadConversions / 2m, 1m) * 10m) +
                (Math.Min((decimal)paymentFollowUps / 5m, 1m) * 5m) +
                ((reportingPct / 100m) * 5m) +
                (Math.Min((decimal)teamSupport / 1m, 1m) * 5m)
            );

            decimal res = Math.Min(100m,
                (Math.Min((decimal)leadConversions / 2m, 1m) * 35m) +
                (Math.Min((decimal)ordersBooked / 1m, 1m) * 45m) +
                ((leadQualityPct / 100m) * 20m)
            );

            decimal overall = Math.Round((0.70m * act) + (0.30m * res), 1, MidpointRounding.AwayFromZero);

            string badge = overall >= 90.0m ? "🏆 Star Performer" :
                           overall >= 80.0m ? "☀️ Excellent" :
                           overall >= 70.0m ? "👍 Good" : "📝 Improve";

            return (Math.Round(act, 1), Math.Round(res, 1), overall, badge);
        }

        public async Task<DailySalesRecognitionDashboardResponseDto> GetDashboardDataAsync(DateTime? startDate, DateTime? endDate, string? executive)
        {
            var query = _context.DailySalesRecognitionLogs.AsNoTracking().AsQueryable();

            if (startDate.HasValue)
            {
                var sDate = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
                query = query.Where(x => x.Date >= sDate);
            }

            if (endDate.HasValue)
            {
                var eDate = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                query = query.Where(x => x.Date <= eDate);
            }

            if (!string.IsNullOrWhiteSpace(executive))
            {
                var execLower = executive.Trim().ToLower();
                query = query.Where(x => x.ExecutiveName.ToLower() == execLower);
            }

            var logs = await query.OrderByDescending(x => x.Date).ThenBy(x => x.ExecutiveName).ToListAsync();

            var items = logs.Select(x => new DailySalesRecognitionLogDto
            {
                Id = x.Id,
                Date = x.Date,
                ExecutiveUserId = x.ExecutiveUserId,
                ExecutiveName = x.ExecutiveName,
                NewLeads = x.NewLeads,
                CustomerCalls = x.CustomerCalls,
                Meetings = x.Meetings,
                FollowUps = x.FollowUps,
                Quotations = x.Quotations,
                LeadConversions = x.LeadConversions,
                OrdersBooked = x.OrdersBooked,
                PaymentFollowUps = x.PaymentFollowUps,
                ReportingPercentage = x.ReportingPercentage,
                TeamSupport = x.TeamSupport,
                LeadQualityPercentage = x.LeadQualityPercentage,
                DailyActivityScore = x.DailyActivityScore,
                BusinessResultScore = x.BusinessResultScore,
                OverallScore = x.OverallScore,
                Recognition = x.Recognition,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            }).ToList();

            // Aggregations
            var kpi = new DailyKpiSummaryDto
            {
                TotalLeads = items.Sum(x => x.NewLeads),
                TotalMeetings = items.Sum(x => x.Meetings),
                TotalFollowUps = items.Sum(x => x.FollowUps),
                TotalQuotations = items.Sum(x => x.Quotations),
                TotalConversions = items.Sum(x => x.LeadConversions),
                TotalOrders = items.Sum(x => x.OrdersBooked),
                TeamAverageOverallScore = items.Any() ? Math.Round(items.Average(x => x.OverallScore), 1) : 0m
            };

            // Executive Rankings Rollup
            var rankings = items
                .GroupBy(x => x.ExecutiveName, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    decimal avgScore = Math.Round(g.Average(x => x.OverallScore), 1);
                    string badge = avgScore >= 90.0m ? "🏆 Star Performer" :
                                   avgScore >= 80.0m ? "☀️ Excellent" :
                                   avgScore >= 70.0m ? "👍 Good" : "📝 Improve";

                    return new ExecutiveRankingDto
                    {
                        Executive = g.Key,
                        ExecutiveUserId = g.FirstOrDefault()?.ExecutiveUserId,
                        AvgOverallScore = avgScore,
                        TotalLeads = g.Sum(x => x.NewLeads),
                        TotalMeetings = g.Sum(x => x.Meetings),
                        TotalFollowUps = g.Sum(x => x.FollowUps),
                        TotalQuotations = g.Sum(x => x.Quotations),
                        TotalConversions = g.Sum(x => x.LeadConversions),
                        TotalOrders = g.Sum(x => x.OrdersBooked),
                        ReportingPercentage = Math.Round(g.Average(x => x.ReportingPercentage), 1),
                        Recognition = badge
                    };
                })
                .OrderByDescending(r => r.AvgOverallScore)
                .ToList();

            var chartSeries = rankings
                .Select(r => new LeaderboardChartItemDto
                {
                    Executive = r.Executive,
                    AvgOverallScore = r.AvgOverallScore
                })
                .ToList();

            return new DailySalesRecognitionDashboardResponseDto
            {
                Items = items,
                KpiSummary = kpi,
                Rankings = rankings,
                ChartSeries = chartSeries
            };
        }

        public async Task<DailySalesRecognitionLogDto> UpsertLogAsync(DailySalesRecognitionUpsertDto dto)
        {
            var (act, res, overall, badge) = CalculateScores(
                dto.NewLeads, dto.CustomerCalls, dto.Meetings, dto.FollowUps,
                dto.Quotations, dto.LeadConversions, dto.OrdersBooked, dto.PaymentFollowUps,
                dto.ReportingPercentage, dto.TeamSupport, dto.LeadQualityPercentage
            );

            var dateUtc = DateTime.SpecifyKind(dto.Date.Date, DateTimeKind.Utc);

            DailySalesRecognitionLog? entity = null;
            if (dto.Id > 0)
            {
                entity = await _context.DailySalesRecognitionLogs.FindAsync(dto.Id);
            }

            if (entity == null)
            {
                entity = new DailySalesRecognitionLog
                {
                    Date = dateUtc,
                    ExecutiveUserId = dto.ExecutiveUserId,
                    ExecutiveName = dto.ExecutiveName.Trim(),
                    NewLeads = dto.NewLeads,
                    CustomerCalls = dto.CustomerCalls,
                    Meetings = dto.Meetings,
                    FollowUps = dto.FollowUps,
                    Quotations = dto.Quotations,
                    LeadConversions = dto.LeadConversions,
                    OrdersBooked = dto.OrdersBooked,
                    PaymentFollowUps = dto.PaymentFollowUps,
                    ReportingPercentage = dto.ReportingPercentage,
                    TeamSupport = dto.TeamSupport,
                    LeadQualityPercentage = dto.LeadQualityPercentage,
                    DailyActivityScore = act,
                    BusinessResultScore = res,
                    OverallScore = overall,
                    Recognition = badge,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _context.DailySalesRecognitionLogs.AddAsync(entity);
            }
            else
            {
                entity.Date = dateUtc;
                entity.ExecutiveUserId = dto.ExecutiveUserId;
                entity.ExecutiveName = dto.ExecutiveName.Trim();
                entity.NewLeads = dto.NewLeads;
                entity.CustomerCalls = dto.CustomerCalls;
                entity.Meetings = dto.Meetings;
                entity.FollowUps = dto.FollowUps;
                entity.Quotations = dto.Quotations;
                entity.LeadConversions = dto.LeadConversions;
                entity.OrdersBooked = dto.OrdersBooked;
                entity.PaymentFollowUps = dto.PaymentFollowUps;
                entity.ReportingPercentage = dto.ReportingPercentage;
                entity.TeamSupport = dto.TeamSupport;
                entity.LeadQualityPercentage = dto.LeadQualityPercentage;
                entity.DailyActivityScore = act;
                entity.BusinessResultScore = res;
                entity.OverallScore = overall;
                entity.Recognition = badge;
                entity.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return new DailySalesRecognitionLogDto
            {
                Id = entity.Id,
                Date = entity.Date,
                ExecutiveUserId = entity.ExecutiveUserId,
                ExecutiveName = entity.ExecutiveName,
                NewLeads = entity.NewLeads,
                CustomerCalls = entity.CustomerCalls,
                Meetings = entity.Meetings,
                FollowUps = entity.FollowUps,
                Quotations = entity.Quotations,
                LeadConversions = entity.LeadConversions,
                OrdersBooked = entity.OrdersBooked,
                PaymentFollowUps = entity.PaymentFollowUps,
                ReportingPercentage = entity.ReportingPercentage,
                TeamSupport = entity.TeamSupport,
                LeadQualityPercentage = entity.LeadQualityPercentage,
                DailyActivityScore = entity.DailyActivityScore,
                BusinessResultScore = entity.BusinessResultScore,
                OverallScore = entity.OverallScore,
                Recognition = entity.Recognition,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        public async Task<List<DailySalesRecognitionLogDto>> BulkUpsertLogsAsync(List<DailySalesRecognitionUpsertDto> dtos)
        {
            var results = new List<DailySalesRecognitionLogDto>();
            foreach (var dto in dtos)
            {
                if (string.IsNullOrWhiteSpace(dto.ExecutiveName)) continue;
                results.Add(await UpsertLogAsync(dto));
            }
            return results;
        }

        public async Task<bool> DeleteLogAsync(int id)
        {
            var entity = await _context.DailySalesRecognitionLogs.FindAsync(id);
            if (entity == null) return false;

            _context.DailySalesRecognitionLogs.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<DailySalesRecognitionLogDto> AutofillExecutiveMetricsAsync(DateTime date, string executiveName)
        {
            if (string.IsNullOrWhiteSpace(executiveName))
            {
                return new DailySalesRecognitionLogDto
                {
                    Date = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc),
                    ExecutiveName = string.Empty,
                    ReportingPercentage = 100m,
                    TeamSupport = 1
                };
            }

            var cleanExecName = executiveName.Trim();
            var targetDate = date.Date;
            var dayStartUtc = DateTime.SpecifyKind(targetDate, DateTimeKind.Utc);
            var dayEndUtc = DateTime.SpecifyKind(targetDate.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

            // 1. First find matching user in DB
            var users = await _context.Users.AsNoTracking()
                .Include(u => u.Role)
                .Where(u => u.IsActive && !string.IsNullOrWhiteSpace(u.FullName))
                .ToListAsync();

            User? matchedUser = null;

            // Direct full name match (prefer role in crm_roles containing 'sales')
            matchedUser = users.FirstOrDefault(u => u.Role != null && u.Role.Name.ToLower().Contains("sales") && string.Equals(u.FullName.Trim(), cleanExecName, StringComparison.OrdinalIgnoreCase))
                       ?? users.FirstOrDefault(u => string.Equals(u.FullName.Trim(), cleanExecName, StringComparison.OrdinalIgnoreCase));

            // Fallback: contains match
            if (matchedUser == null)
            {
                matchedUser = users.FirstOrDefault(u => u.Role != null && u.Role.Name.ToLower().Contains("sales") && (
                    cleanExecName.IndexOf(u.FullName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    u.FullName.IndexOf(cleanExecName, StringComparison.OrdinalIgnoreCase) >= 0))
                    ?? users.FirstOrDefault(u =>
                    cleanExecName.IndexOf(u.FullName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    u.FullName.IndexOf(cleanExecName, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            int? userId = matchedUser?.Id;
            var execLower = cleanExecName.ToLower();
            var matchedLower = matchedUser?.FullName?.Trim().ToLower();

            // 2. Check if an existing log exists for this exact date & executive to preserve its ID
            var targetStart = DateTime.SpecifyKind(targetDate, DateTimeKind.Utc);
            var targetEnd = DateTime.SpecifyKind(targetDate.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            var existingLog = await _context.DailySalesRecognitionLogs.AsNoTracking()
                .Where(x => x.Date >= targetStart && x.Date <= targetEnd &&
                    (x.ExecutiveName.ToLower() == execLower ||
                     (userId.HasValue && x.ExecutiveUserId == userId.Value) ||
                     (matchedLower != null && x.ExecutiveName.ToLower() == matchedLower)))
                .FirstOrDefaultAsync();

            int existingLogId = existingLog?.Id ?? 0;
            decimal reportingPct = existingLog?.ReportingPercentage ?? 100m;
            int teamSupport = existingLog?.TeamSupport ?? 1;

            // 3. Aggregate live CRM records for this executive on this target date
            int newLeads = 0;
            int customerCalls = 0;
            int meetings = 0;
            int followUps = 0;
            int quotations = 0;
            int leadConversions = 0;
            int ordersBooked = 0;
            int paymentFollowUps = 0;
            decimal leadQualityPct = 0m;

            if (userId.HasValue)
            {
                int uid = userId.Value;

                // Leads
                newLeads = await _context.Leads.AsNoTracking()
                    .CountAsync(l => l.IsActive && (l.LeadOwnerId == uid || l.CreatedBy == uid) &&
                        ((l.LeadDate.HasValue && l.LeadDate.Value >= dayStartUtc && l.LeadDate.Value <= dayEndUtc) ||
                         (l.CreatedAt.HasValue && l.CreatedAt.Value >= dayStartUtc && l.CreatedAt.Value <= dayEndUtc)));

                // Calls
                customerCalls = await _context.CallLogs.AsNoTracking()
                    .CountAsync(c => c.IsActive && (c.CreatedBy == uid || (c.RelatedLeadId.HasValue && _context.Leads.Any(l => l.Id == c.RelatedLeadId.Value && l.LeadOwnerId == uid))) &&
                        ((c.CallStarted >= dayStartUtc && c.CallStarted <= dayEndUtc) ||
                         (c.CreatedAt >= dayStartUtc && c.CreatedAt <= dayEndUtc)));

                // Meetings
                meetings = await _context.Tasks.AsNoTracking()
                    .CountAsync(t => t.IsActive && (t.AssigneeUserId == uid || t.CreatedBy == uid) &&
                        ((t.TaskDueDate >= dayStartUtc && t.TaskDueDate <= dayEndUtc) ||
                         (t.CreatedAt >= dayStartUtc && t.CreatedAt <= dayEndUtc) ||
                         (t.UpdatedAt >= dayStartUtc && t.UpdatedAt <= dayEndUtc && (t.TaskStatus.ToLower() == "done" || t.TaskStatus.ToLower() == "completed"))) &&
                        (t.TaskType.ToLower().Contains("meeting") || t.TaskTitle.ToLower().Contains("meeting") ||
                         t.TaskTitle.ToLower().Contains("demo") || t.TaskTitle.ToLower().Contains("visit") ||
                         t.TaskTitle.ToLower().Contains("discussion") || t.TaskTitle.ToLower().Contains("presentation") ||
                         t.TaskDescription.ToLower().Contains("meeting") || t.TaskDescription.ToLower().Contains("demo") ||
                         t.TaskDescription.ToLower().Contains("visit") || t.TaskDescription.ToLower().Contains("discussion") ||
                         t.TaskDescription.ToLower().Contains("presentation")));

                // Follow-ups
                followUps = await _context.Tasks.AsNoTracking()
                    .CountAsync(t => t.IsActive && (t.AssigneeUserId == uid || t.CreatedBy == uid) &&
                        ((t.TaskDueDate >= dayStartUtc && t.TaskDueDate <= dayEndUtc) ||
                         (t.CreatedAt >= dayStartUtc && t.CreatedAt <= dayEndUtc) ||
                         (t.UpdatedAt >= dayStartUtc && t.UpdatedAt <= dayEndUtc && (t.TaskStatus.ToLower() == "done" || t.TaskStatus.ToLower() == "completed"))));

                // Quotations
                quotations = await _context.Quotations.AsNoTracking()
                    .CountAsync(q => (q.CreatedBy == uid || (q.DealId.HasValue && _context.Deals.Any(d => d.Id == q.DealId.Value && d.DealOwnerId == uid))) &&
                        ((q.QuotationDate >= dayStartUtc && q.QuotationDate <= dayEndUtc) || (q.CreatedAt >= dayStartUtc && q.CreatedAt <= dayEndUtc)) &&
                        q.Status != "Draft" && q.Status != "Cancelled" && q.Status != "Rejected");

                // Lead Conversions (Deals created or moved to stage on this date)
                leadConversions = await _context.Deals.AsNoTracking()
                    .CountAsync(d => d.IsActive && (d.DealOwnerId == uid || d.CreatedBy == uid) &&
                        ((d.CreatedAt >= dayStartUtc && d.CreatedAt <= dayEndUtc) || (d.UpdatedAt >= dayStartUtc && d.UpdatedAt <= dayEndUtc)));

                // Orders Booked (Deals closed/won on this date)
                ordersBooked = await _context.Deals.AsNoTracking()
                    .CountAsync(d => d.IsActive && (d.DealOwnerId == uid || d.CreatedBy == uid) &&
                        ((d.UpdatedAt >= dayStartUtc && d.UpdatedAt <= dayEndUtc) || (d.CreatedAt >= dayStartUtc && d.CreatedAt <= dayEndUtc)) &&
                        (d.Status.ToLower().Contains("won") || d.Status.ToLower().Contains("booked")));

                // Payment Follow-ups
                paymentFollowUps = await _context.Tasks.AsNoTracking()
                    .CountAsync(t => t.IsActive && (t.AssigneeUserId == uid || t.CreatedBy == uid) &&
                        ((t.TaskDueDate >= dayStartUtc && t.TaskDueDate <= dayEndUtc) || (t.CreatedAt >= dayStartUtc && t.CreatedAt <= dayEndUtc)) &&
                        (t.TaskTitle.ToLower().Contains("payment") || t.TaskDescription.ToLower().Contains("payment") || t.TaskType.ToLower().Contains("payment")));

                // Lead Quality %
                if (newLeads > 0)
                {
                    var positiveCount = await _context.Leads.AsNoTracking()
                        .CountAsync(l => l.IsActive && (l.LeadOwnerId == uid || l.CreatedBy == uid) &&
                            ((l.LeadDate.HasValue && l.LeadDate.Value >= dayStartUtc && l.LeadDate.Value <= dayEndUtc) ||
                             (l.CreatedAt.HasValue && l.CreatedAt.Value >= dayStartUtc && l.CreatedAt.Value <= dayEndUtc)) &&
                            l.LeadStatus != null && l.LeadStatus.IsPositive);

                    leadQualityPct = Math.Round(((decimal)positiveCount / (decimal)newLeads) * 100m, 1);
                }
                else
                {
                    leadQualityPct = 0m;
                }
            }

            var (act, res, overall, badge) = CalculateScores(
                newLeads, customerCalls, meetings, followUps, quotations,
                leadConversions, ordersBooked, paymentFollowUps,
                reportingPct, teamSupport, leadQualityPct
            );

            return new DailySalesRecognitionLogDto
            {
                Id = existingLogId,
                Date = DateTime.SpecifyKind(targetDate, DateTimeKind.Utc),
                ExecutiveUserId = userId,
                ExecutiveName = cleanExecName,
                NewLeads = newLeads,
                CustomerCalls = customerCalls,
                Meetings = meetings,
                FollowUps = followUps,
                Quotations = quotations,
                LeadConversions = leadConversions,
                OrdersBooked = ordersBooked,
                PaymentFollowUps = paymentFollowUps,
                ReportingPercentage = reportingPct,
                TeamSupport = teamSupport,
                LeadQualityPercentage = leadQualityPct,
                DailyActivityScore = act,
                BusinessResultScore = res,
                OverallScore = overall,
                Recognition = badge,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public async Task<List<string>> GetActiveExecutivesAsync()
        {
            return await _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .Where(u => u.IsActive && 
                            !string.IsNullOrWhiteSpace(u.FullName) &&
                            u.Role != null && 
                            u.Role.Name.ToLower().Contains("sales"))
                .OrderBy(u => u.FullName)
                .Select(u => u.FullName.Trim())
                .Distinct()
                .ToListAsync();
        }
    }
}
