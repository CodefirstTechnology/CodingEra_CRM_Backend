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

        public async Task<List<string>> GetActiveExecutivesAsync()
        {
            var users = await _context.Users
                .AsNoTracking()
                .Where(u => u.IsActive && !string.IsNullOrWhiteSpace(u.FullName))
                .OrderBy(u => u.FullName)
                .Select(u => u.FullName.Trim())
                .Distinct()
                .ToListAsync();

            if (!users.Any())
            {
                users = new List<string> { "Sharda Ahire", "Neha Verma", "Amit Sharma", "Pooja Patel" };
            }

            return users;
        }
    }
}
